/*
 * This file is part of Minotaur.
 * 
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Linq;
using System.Net.Http.Headers;

namespace Minotaur.Tests.Grammar;

/// <summary>
/// Thrown when a grammar file cannot be retrieved from the external
/// Minotaur-Grammars repository (network unavailable, repository private
/// without a token, or file missing).
/// </summary>
public class ExternalGrammarUnavailableException : Exception
{
    public ExternalGrammarUnavailableException(string path, string reason)
        : base($"Grammar '{path}' could not be fetched from the Minotaur-Grammars repository: {reason}")
    {
    }
}

/// <summary>
/// Test helper that retrieves grammar definitions from the external
/// Minotaur-Grammars repository (https://github.com/DevelApp-ai/Minotaur-Grammars),
/// which is the single source of truth for grammar files since they were
/// externalized out of this repository.
///
/// Configuration (all optional, via environment variables):
///  - MINOTAUR_GRAMMARS_RAW_BASE: base URL for raw file contents.
///    Defaults to the main branch of DevelApp-ai/Minotaur-Grammars on GitHub.
///  - MINOTAUR_GRAMMARS_TOKEN (or GITHUB_TOKEN): bearer token used when the
///    repository is private.
///
/// Results are cached per relative path, so repeated lookups by tests do not
/// hammer the remote repository.
/// </summary>
public sealed class ExternalGrammarRepository : IDisposable
{
    public const string DefaultRawBaseUrl =
        "https://raw.githubusercontent.com/DevelApp-ai/Minotaur-Grammars/main/";

    private static readonly Lazy<ExternalGrammarRepository> DefaultInstance = new(() => new ExternalGrammarRepository());

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly Dictionary<string, string> _cache = new();
    private readonly object _lock = new();

    public static ExternalGrammarRepository Default => DefaultInstance.Value;

    public ExternalGrammarRepository()
    {
        _baseUrl = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_RAW_BASE")
            ?? DefaultRawBaseUrl;
        if (!_baseUrl.EndsWith("/"))
            _baseUrl += "/";

        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        var token = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_TOKEN")
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Fetches the content of a grammar file by its repository-relative path,
    /// e.g. "CSharp10.grammar" or "PostalCodes/US_Postal_Code.grammar".
    /// Throws <see cref="ExternalGrammarUnavailableException"/> when the file
    /// cannot be retrieved, so tests can degrade to "inconclusive" instead of
    /// failing in offline environments.
    /// </summary>
    public async Task<string> GetGrammarAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path must not be empty.", nameof(relativePath));

        lock (_lock)
        {
            if (_cache.TryGetValue(relativePath, out var cached))
                return cached;
        }

        string content;
        try
        {
            // Escape each path segment separately (slashes are separators,
            // not data). Uri.EscapeDataString per segment; Uri.EscapeUriString
            // is obsolete (SYSLIB0013).
            var escapedPath = string.Join("/",
                relativePath.Split('/').Select(Uri.EscapeDataString));
            using var response = await _httpClient.GetAsync(_baseUrl + escapedPath);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalGrammarUnavailableException(
                    relativePath, $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})");
            }

            content = await response.Content.ReadAsStringAsync();
        }
        catch (ExternalGrammarUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ExternalGrammarUnavailableException(relativePath, ex.Message);
        }

        lock (_lock)
        {
            _cache[relativePath] = content;
        }

        return content;
    }

    public void Dispose() => _httpClient.Dispose();
}
