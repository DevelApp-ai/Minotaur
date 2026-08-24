using System.Text.Json.Serialization;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Standard API response format from the Marketplace
    /// </summary>
    /// <typeparam name="T">Type of the data in the response</typeparam>
    public class MarketplaceApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfo Pagination { get; set; }
    }

    /// <summary>
    /// Response wrapper for grammar info
    /// </summary>
    public class GrammarInfoResponse
    {
        [JsonPropertyName("grammar")]
        public GrammarInfo Grammar { get; set; }
    }
}
