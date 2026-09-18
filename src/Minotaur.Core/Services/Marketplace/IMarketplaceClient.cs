using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Interface for interacting with the Minotaur Grammar Marketplace API.
    /// This client provides methods for searching, downloading, publishing, and managing grammars.
    /// </summary>
    public interface IMarketplaceClient
    {
        /// <summary>
        /// Base URL of the Marketplace API
        /// </summary>
        string BaseUrl { get; set; }

        /// <summary>
        /// API key for authentication
        /// </summary>
        string? ApiKey { get; set; }

        /// <summary>
        /// Timeout for API requests in milliseconds
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// Search for grammars in the marketplace
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="filter">Optional filter criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Search results with matching grammars</returns>
        Task<GrammarSearchResult> SearchGrammarsAsync(
            string? query = null,
            GrammarFilter? filter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed information about a specific grammar
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="version">Optional version (defaults to latest)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Grammar information</returns>
        Task<GrammarInfo?> GetGrammarAsync(
            string vendor,
            string name,
            string? version = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Download a grammar package
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="version">Optional version (defaults to latest)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream containing the grammar package (.tar.gz)</returns>
        Task<Stream> DownloadGrammarPackageAsync(
            string vendor,
            string name,
            string? version = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publish a grammar to the marketplace
        /// </summary>
        /// <param name="grammarPackage">Grammar package stream</param>
        /// <param name="metadata">Grammar metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Publish result with grammar ID and status</returns>
        Task<PublishResult> PublishGrammarAsync(
            Stream grammarPackage,
            GrammarMetadata metadata,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing grammar
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="grammarPackage">Updated grammar package stream</param>
        /// <param name="metadata">Updated metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Update result</returns>
        Task<PublishResult> UpdateGrammarAsync(
            string vendor,
            string name,
            Stream grammarPackage,
            GrammarMetadata metadata,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a grammar from the marketplace
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteGrammarAsync(
            string vendor,
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get list of grammars published by a user
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of grammars published by the user</returns>
        Task<IEnumerable<GrammarInfo>> GetUserGrammarsAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get list of grammars purchased by the current user
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of purchased grammars</returns>
        Task<IEnumerable<PurchasedGrammar>> GetPurchasedGrammarsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Purchase a grammar (for paid grammars)
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="version">Optional version</param>
        /// <param name="paymentMethodId">Payment method ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Purchase result with transaction details</returns>
        Task<PurchaseResult> PurchaseGrammarAsync(
            string vendor,
            string name,
            string? version = null,
            string? paymentMethodId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get reviews for a grammar
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of reviews</returns>
        Task<GrammarReviews> GetGrammarReviewsAsync(
            string vendor,
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a review for a grammar
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="rating">Rating (1-5)</param>
        /// <param name="comment">Optional comment</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Review result</returns>
        Task<ReviewResult> AddGrammarReviewAsync(
            string vendor,
            string name,
            int rating,
            string? comment = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the current user has access to a grammar
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="version">Optional version</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user has access</returns>
        Task<bool> CheckAccessAsync(
            string vendor,
            string name,
            string? version = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Filter criteria for grammar search
    /// </summary>
    public class GrammarFilter
    {
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string PricingModel { get; set; } = string.Empty;
        public string SortBy { get; set; } = "created_at";
        public string SortOrder { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;
    }

    /// <summary>
    /// Search result containing matching grammars
    /// </summary>
    public class GrammarSearchResult
    {
        public IEnumerable<GrammarInfo> Grammars { get; set; } = null!;
        public PaginationInfo Pagination { get; set; } = null!;
    }

    /// <summary>
    /// Pagination information
    /// </summary>
    public class PaginationInfo
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int Pages { get; set; }
    }

    /// <summary>
    /// Grammar metadata for publishing
    /// </summary>
    public class GrammarMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string MinotaurVersion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string MainFile { get; set; } = string.Empty;
        public Dictionary<string, string> Dependencies { get; set; } = new();
        public string Documentation { get; set; } = string.Empty;
        public string PricingModel { get; set; } = "free";
        public decimal Price { get; set; } = 0;
    }

    /// <summary>
    /// Result of a publish operation
    /// </summary>
    public class PublishResult
    {
        public bool Success { get; set; }
        public string GrammarId { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of a purchase operation
    /// </summary>
    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string GrammarId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// Information about a purchased grammar
    /// </summary>
    public class PurchasedGrammar
    {
        public string GrammarId { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime PurchasedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string AccessType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of adding a review
    /// </summary>
    public class ReviewResult
    {
        public bool Success { get; set; }
        public string ReviewId { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// Reviews for a grammar
    /// </summary>
    public class GrammarReviews
    {
        public IEnumerable<GrammarReview> Reviews { get; set; } = null!;
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public PaginationInfo Pagination { get; set; } = null!;
    }

    /// <summary>
    /// Individual grammar review
    /// </summary>
    public class GrammarReview
    {
        public string Id { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
        public GrammarReviewUser User { get; set; } = null!;
    }

    /// <summary>
    /// User information for a review
    /// </summary>
    public class GrammarReviewUser
    {
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
    }
}
