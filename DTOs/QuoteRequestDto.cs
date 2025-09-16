namespace AuthAPI.DTOs
{
    public class QuoteRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        // optionally add other fields from the form (e.g. CompanyName, Phone, etc.)
    }
}
