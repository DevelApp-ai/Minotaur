using System;
using System.Collections.Generic;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Complete information about a grammar in the marketplace
    /// </summary>
    public class GrammarInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string MinotaurVersion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string MainFile { get; set; } = string.Empty;
        public Dictionary<string, string> Dependencies { get; set; } = new Dictionary<string, string>();
        public string Documentation { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public string PricingModel { get; set; } = "free";
        public decimal Price { get; set; } = 0;
        public int DownloadCount { get; set; }
        public double RatingAverage { get; set; }
        public int RatingCount { get; set; }
        public GrammarAuthor Author { get; set; } = null!;
        public bool HasAccess { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PackageUrl { get; set; } = string.Empty;
        public long PackageSize { get; set; }
        public string PackageHash { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
        public bool IsDeprecated { get; set; } = false;
    }

    /// <summary>
    /// Author information for a grammar
    /// </summary>
    public class GrammarAuthor
    {
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
    }
}
