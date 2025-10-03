/*
 * This file is part of Minotaur.
 * 
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
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

using System.Text.RegularExpressions;

namespace Minotaur.Projects.Grammar;

/// <summary>
/// Represents a grammar version with support for semantic versioning and language-specific versioning schemes.
/// </summary>
public partial class GrammarVersion : IComparable<GrammarVersion>, IEquatable<GrammarVersion>
{
    private static readonly Regex VersionRegex = CreateVersionRegex();

    /// <summary>
    /// Gets the major version number.
    /// </summary>
    public int Major { get; init; }

    /// <summary>
    /// Gets the minor version number.
    /// </summary>
    public int Minor { get; init; }

    /// <summary>
    /// Gets the patch version number.
    /// </summary>
    public int Patch { get; init; }

    /// <summary>
    /// Gets the pre-release identifier (e.g., "alpha", "beta", "rc1").
    /// </summary>
    public string? PreRelease { get; init; }

    /// <summary>
    /// Gets the build metadata.
    /// </summary>
    public string? BuildMetadata { get; init; }

    /// <summary>
    /// Gets the original version string.
    /// </summary>
    public string OriginalString { get; init; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the GrammarVersion class.
    /// </summary>
    /// <param name="major">The major version number.</param>
    /// <param name="minor">The minor version number.</param>
    /// <param name="patch">The patch version number.</param>
    /// <param name="preRelease">Optional pre-release identifier.</param>
    /// <param name="buildMetadata">Optional build metadata.</param>
    public GrammarVersion(int major, int minor = 0, int patch = 0, string? preRelease = null, string? buildMetadata = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        BuildMetadata = buildMetadata;
        OriginalString = ToString();
    }

    /// <summary>
    /// Parses a version string into a GrammarVersion instance.
    /// Supports semantic versioning (e.g., "1.2.3-alpha+build") and language-specific formats (e.g., "C#10", "ES2022").
    /// </summary>
    /// <param name="versionString">The version string to parse.</param>
    /// <returns>A GrammarVersion instance representing the parsed version.</returns>
    /// <exception cref="ArgumentException">Thrown when the version string format is invalid.</exception>
    public static GrammarVersion Parse(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            throw new ArgumentException("Version string cannot be null or empty.", nameof(versionString));

        // Handle language-specific version formats
        var languageVersion = TryParseLanguageSpecificVersion(versionString);
        if (languageVersion != null)
            return languageVersion;

        // Handle semantic versioning
        var match = VersionRegex.Match(versionString);
        if (!match.Success)
            throw new ArgumentException($"Invalid version format: {versionString}", nameof(versionString));

        var major = int.Parse(match.Groups["major"].Value);
        var minor = match.Groups["minor"].Success ? int.Parse(match.Groups["minor"].Value) : 0;
        var patch = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;
        var preRelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null;
        var buildMetadata = match.Groups["buildmetadata"].Success ? match.Groups["buildmetadata"].Value : null;

        return new GrammarVersion(major, minor, patch, preRelease, buildMetadata)
        {
            OriginalString = versionString
        };
    }

    /// <summary>
    /// Tries to parse a version string into a GrammarVersion instance.
    /// </summary>
    /// <param name="versionString">The version string to parse.</param>
    /// <param name="version">When this method returns, contains the parsed version if successful, or null if parsing failed.</param>
    /// <returns>True if parsing was successful, false otherwise.</returns>
    public static bool TryParse(string? versionString, out GrammarVersion? version)
    {
        version = null;
        
        if (string.IsNullOrWhiteSpace(versionString))
            return false;

        try
        {
            version = Parse(versionString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static GrammarVersion? TryParseLanguageSpecificVersion(string versionString)
    {
        // C# versions (e.g., "C#10", "CSharp10")
        var csharpMatch = Regex.Match(versionString, @"^C#?(?:Sharp)?(\d+)(?:\.(\d+))?$", RegexOptions.IgnoreCase);
        if (csharpMatch.Success)
        {
            var major = int.Parse(csharpMatch.Groups[1].Value);
            var minor = csharpMatch.Groups[2].Success ? int.Parse(csharpMatch.Groups[2].Value) : 0;
            return new GrammarVersion(major, minor) { OriginalString = versionString };
        }

        // JavaScript/ECMAScript versions (e.g., "ES2022", "ES6", "JavaScript2022")
        var jsMatch = Regex.Match(versionString, @"^(?:ES|ECMAScript|JavaScript)(\d+)$", RegexOptions.IgnoreCase);
        if (jsMatch.Success)
        {
            var year = int.Parse(jsMatch.Groups[1].Value);
            // Convert year to version number (e.g., 2022 -> 13, 2015 -> 6)
            var version = year >= 2015 ? year - 2009 : year;
            return new GrammarVersion(version) { OriginalString = versionString };
        }

        // Python versions (e.g., "Python311", "Python3.11")
        var pythonMatch = Regex.Match(versionString, @"^Python(\d)(\d{1,2})$", RegexOptions.IgnoreCase);
        if (pythonMatch.Success)
        {
            var major = int.Parse(pythonMatch.Groups[1].Value);
            var minor = int.Parse(pythonMatch.Groups[2].Value);
            return new GrammarVersion(major, minor) { OriginalString = versionString };
        }

        // Java versions (e.g., "Java17", "Java8")
        var javaMatch = Regex.Match(versionString, @"^Java(\d+)$", RegexOptions.IgnoreCase);
        if (javaMatch.Success)
        {
            var major = int.Parse(javaMatch.Groups[1].Value);
            return new GrammarVersion(major) { OriginalString = versionString };
        }

        // C/C++ standards (e.g., "C17", "Cpp20")
        var cMatch = Regex.Match(versionString, @"^C(?:pp)?(\d+)$", RegexOptions.IgnoreCase);
        if (cMatch.Success)
        {
            var standard = int.Parse(cMatch.Groups[1].Value);
            return new GrammarVersion(standard) { OriginalString = versionString };
        }

        // Rust editions (e.g., "Rust2021")
        var rustMatch = Regex.Match(versionString, @"^Rust(\d{4})$", RegexOptions.IgnoreCase);
        if (rustMatch.Success)
        {
            var edition = int.Parse(rustMatch.Groups[1].Value);
            return new GrammarVersion(edition) { OriginalString = versionString };
        }

        return null;
    }

    /// <summary>
    /// Compares this version with another version.
    /// </summary>
    /// <param name="other">The version to compare with.</param>
    /// <returns>A value indicating the relative order of the versions.</returns>
    public int CompareTo(GrammarVersion? other)
    {
        if (other is null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;

        // Pre-release versions are lower than release versions
        if (PreRelease is null && other.PreRelease is not null) return 1;
        if (PreRelease is not null && other.PreRelease is null) return -1;
        if (PreRelease is not null && other.PreRelease is not null)
            return string.Compare(PreRelease, other.PreRelease, StringComparison.OrdinalIgnoreCase);

        return 0;
    }

    /// <summary>
    /// Determines whether this version is equal to another version.
    /// </summary>
    /// <param name="other">The version to compare with.</param>
    /// <returns>True if the versions are equal, false otherwise.</returns>
    public bool Equals(GrammarVersion? other)
    {
        return CompareTo(other) == 0;
    }

    /// <summary>
    /// Determines whether this version is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return obj is GrammarVersion other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code for this version.
    /// </summary>
    /// <returns>A hash code for this version.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, PreRelease);
    }

    /// <summary>
    /// Gets the string representation of this version.
    /// </summary>
    /// <returns>A string representation of this version.</returns>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        
        if (!string.IsNullOrEmpty(PreRelease))
            version += $"-{PreRelease}";
            
        if (!string.IsNullOrEmpty(BuildMetadata))
            version += $"+{BuildMetadata}";
            
        return version;
    }

    [GeneratedRegex(@"^(?<major>\d+)(\.(?<minor>\d+))?(\.(?<patch>\d+))?(-(?<prerelease>[0-9A-Za-z\-\.]+))?(\+(?<buildmetadata>[0-9A-Za-z\-\.]+))?$")]
    private static partial Regex CreateVersionRegex();

    public static bool operator ==(GrammarVersion? left, GrammarVersion? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(GrammarVersion? left, GrammarVersion? right)
    {
        return !(left == right);
    }

    public static bool operator <(GrammarVersion? left, GrammarVersion? right)
    {
        return left?.CompareTo(right) < 0;
    }

    public static bool operator >(GrammarVersion? left, GrammarVersion? right)
    {
        return left?.CompareTo(right) > 0;
    }

    public static bool operator <=(GrammarVersion? left, GrammarVersion? right)
    {
        return left?.CompareTo(right) <= 0;
    }

    public static bool operator >=(GrammarVersion? left, GrammarVersion? right)
    {
        return left?.CompareTo(right) >= 0;
    }
}