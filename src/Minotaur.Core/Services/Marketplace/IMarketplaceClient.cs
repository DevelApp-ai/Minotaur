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
        string ApiKey { get; set; }

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
            string query = null,
            GrammarFilter filter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed information about a specific grammar
        /// </summary>
        /// <param name="vendor">Vendor name</param>
        /// <param name="name">Grammar name</param>
        /// <param name="version">Optional version (defaults to latest)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Grammar information</returns>
        Task<GrammarInfo> GetGrammarAsync(
            string vendor,
            string name,
            string version = null,
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
            string version = null,
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
            string version = null,
            string paymentMethodId = null,
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
            string comment = null,
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
            string version = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Filter criteria for grammar search
    /// </summary>
    public class GrammarFilter
    {
        public string[] Tags { get; set; }
        public string PricingModel { get; set; }
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
        public IEnumerable<GrammarInfo> Grammars { get; set; }
        public PaginationInfo Pagination { get; set; }
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
        public string Name { get; set; }
        public string Vendor { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string MinotaurVersion { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string[] Tags { get; set; }
        public string MainFile { get; set; }
        public Dictionary<string, string> Dependencies { get; set; }
        public string Documentation { get; set; }
        public string PricingModel { get; set; } = "free";
        public decimal Price { get; set; } = 0;
    }

    /// <summary>
    /// Result of a publish operation
    /// </summary>
    public class PublishResult
    {
        public bool Success { get; set; }
        public string GrammarId { get; set; }
        public string Vendor { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Result of a purchase operation
    /// </summary>
    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string GrammarId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Information about a purchased grammar
    /// </summary>
    public class PurchasedGrammar
    {
        public string GrammarId { get; set; }
        public string Vendor { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime PurchasedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string AccessType { get; set; }
    }

    /// <summary>
    /// Result of adding a review
    /// </summary>
    public class ReviewResult
    {
        public bool Success { get; set; }
        public string ReviewId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Reviews for a grammar
    /// </summary>
    public class GrammarReviews
    {
        public IEnumerable<GrammarReview> Reviews { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public PaginationInfo Pagination { get; set; }
    }

    /// <summary>
    /// Individual grammar review
    /// </summary>
    public class GrammarReview
    {
        public string Id { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
        public GrammarReviewUser User { get; set; }
    }

    /// <summary>
    /// User information for a review
    /// </summary>
    public class GrammarReviewUser
    {
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsVerified { get; set; }
    }
}
