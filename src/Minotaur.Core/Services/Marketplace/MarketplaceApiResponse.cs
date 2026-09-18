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
        public T Data { get; set; } = default!;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("pagination")]
        public PaginationInfo Pagination { get; set; } = null!;
    }

    /// <summary>
    /// Response wrapper for grammar info
    /// </summary>
    public class GrammarInfoResponse
    {
        [JsonPropertyName("grammar")]
        public GrammarInfo Grammar { get; set; } = null!;
    }
}
