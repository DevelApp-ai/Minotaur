using System;
using System.Collections.Generic;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Complete information about a grammar in the marketplace
    /// </summary>
    public class GrammarInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Vendor { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string MinotaurVersion { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string MainFile { get; set; }
        public Dictionary<string, string> Dependencies { get; set; } = new Dictionary<string, string>();
        public string Documentation { get; set; }
        public string IconUrl { get; set; }
        public string Repository { get; set; }
        public string PricingModel { get; set; } = "free";
        public decimal Price { get; set; } = 0;
        public int DownloadCount { get; set; }
        public double RatingAverage { get; set; }
        public int RatingCount { get; set; }
        public GrammarAuthor Author { get; set; }
        public bool HasAccess { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PackageUrl { get; set; }
        public long PackageSize { get; set; }
        public string PackageHash { get; set; }
        public bool IsPublished { get; set; } = true;
        public bool IsDeprecated { get; set; } = false;
    }

    /// <summary>
    /// Author information for a grammar
    /// </summary>
    public class GrammarAuthor
    {
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
    }
}
