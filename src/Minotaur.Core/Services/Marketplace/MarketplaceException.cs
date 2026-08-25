using System;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Exception thrown when a Marketplace API operation fails
    /// </summary>
    public class MarketplaceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the MarketplaceException class
        /// </summary>
        public MarketplaceException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MarketplaceException class with a message
        /// </summary>
        /// <param name="message">Error message</param>
        public MarketplaceException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MarketplaceException class with a message and inner exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public MarketplaceException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// HTTP status code if the exception was caused by an HTTP error
        /// </summary>
        public System.Net.HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// Error code from the API response
        /// </summary>
        public string ErrorCode { get; set; }
    }
}
