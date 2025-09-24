using System.Text.Json.Serialization;

namespace AuthAPI.DTOs.Quote
{
    public class UpdateQuoteStatusDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
