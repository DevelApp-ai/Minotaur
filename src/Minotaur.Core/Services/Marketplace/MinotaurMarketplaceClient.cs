using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// HTTP client implementation for the Minotaur Grammar Marketplace API.
    /// This client provides methods for searching, downloading, publishing, and managing grammars.
    /// </summary>
    public class MinotaurMarketplaceClient : IMarketplaceClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private string _apiKey;
        private int _timeout = 30000; // 30 seconds default timeout

        /// <summary>
        /// Initializes a new instance of the MinotaurMarketplaceClient
        /// </summary>
        /// <param name="baseUrl">Base URL of the Marketplace API</param>
        /// <param name="apiKey">API key for authentication (optional)</param>
        public MinotaurMarketplaceClient(string baseUrl = null, string apiKey = null)
        {
            _httpClient = new HttpClient();
            BaseUrl = baseUrl ?? "https://marketplace.minotaur.dev/api";
            ApiKey = apiKey;
            
            // Set default headers
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "MinotaurMarketplaceClient/1.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd(
                "application/json");
        }

        /// <summary>
        /// Base URL of the Marketplace API
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// API key for authentication
        /// </summary>
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                _apiKey = value;
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", _apiKey);
                }
                else
                {
                    if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("Authorization");
                    }
                }
            }
        }

        /// <summary>
        /// Timeout for API requests in milliseconds
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set => _timeout = value;
        }

        /// <summary>
        /// Dispose the HTTP client
        /// </summary>
        public void Dispose()
        {
            _httpClient.Dispose();
        }

        /// <summary>
        /// Search for grammars in the marketplace
        /// </summary>
        public async Task<GrammarSearchResult> SearchGrammarsAsync(
            string query = null,
            GrammarFilter filter = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = BuildUrl("/grammars", query, filter);
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);
                
                var response = await _httpClient.GetAsync(url, cts.Token);
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                var result = JsonSerializer.Deserialize<MarketplaceApiResponse<GrammarSearchResult>>(json, options);
                
                if (result?.Success == true)
                {
                    return result.Data;
                }
                
                throw new MarketplaceException(result?.Error ?? "Failed to search grammars");
            }
            catch (HttpRequestException ex)
            {
                throw new MarketplaceException($"HTTP error: {ex.StatusCode} - {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new MarketplaceException("Request timeout");
            }
        }

        /// <summary>
        /// Get detailed information about a specific grammar
        /// </summary>
        public async Task<GrammarInfo> GetGrammarAsync(
            string vendor,
            string name,
            string version = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = version != null 
                    ? $"/grammars/{Uri.EscapeDataString(vendor)}/{Uri.EscapeDataString(name)}?version={Uri.EscapeDataString(version)}"
                    : $"/grammars/{Uri.EscapeDataString(vendor)}/{Uri.EscapeDataString(name)}";
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);
                
                var response = await _httpClient.GetAsync(BuildUrl(url), cts.Token);
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                var result = JsonSerializer.Deserialize<MarketplaceApiResponse<GrammarInfoResponse>>(json, options);
                
                if (result?.Success == true && result.Data?.Grammar != null)
                {
                    return result.Data.Grammar;
                }
                
                throw new MarketplaceException(result?.Error ?? "Grammar not found");
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                throw new MarketplaceException($"HTTP error: {ex.StatusCode} - {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new MarketplaceException("Request timeout");
            }
        }

        /// <summary>
        /// Download a grammar package
        /// </summary>
        public async Task<Stream> DownloadGrammarPackageAsync(
            string vendor,
            string name,
            string version = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = version != null
                    ? $"/grammars/{Uri.EscapeDataString(vendor)}/{Uri.EscapeDataString(name)}/download?version={Uri.EscapeDataString(version)}"
                    : $"/grammars/{Uri.EscapeDataString(vendor)}/{Uri.EscapeDataString(name)}/download";
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);
                
                var response = await _httpClient.GetAsync(BuildUrl(url), cts.Token);
                
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadAsStreamAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new MarketplaceException($"HTTP error: {ex.StatusCode} - {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new MarketplaceException("Request timeout");
            }
        }

        /// <summary>
        /// Publish a grammar to the marketplace
        /// </summary>
        public async Task<PublishResult> PublishGrammarAsync(
            Stream grammarPackage,
            GrammarMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);
                
                using var formData = new MultipartFormDataContent();
                
                // Add package file
                var packageContent = new StreamContent(grammarPackage);
                packageContent.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
                formData.Add(packageContent, "package", "grammar.tar.gz");
                
                // Add metadata as JSON
                var metadataJson = JsonSerializer.Serialize(metadata);
                formData.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");
                
                var response = await _httpClient.PostAsync(
                    BuildUrl("/grammars/publish"),
                    formData,
                    cts.Token);
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                var result = JsonSerializer.Deserialize<MarketplaceApiResponse<PublishResult>>(json, options);
                
                if (result?.Success == true)
                {
                    return result.Data;
                }
                
                return new PublishResult
                {
                    Success = false,
                    Error = result?.Error ?? "Failed to publish grammar"
                };
            }
            catch (HttpRequestException ex)
            {
                return new PublishResult
                {
                    Success = false,
                    Error = $