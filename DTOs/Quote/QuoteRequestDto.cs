namespace AuthAPI.DTOs.Quote
{
    public class QuoteRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    public class QuoteResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime DateCreated { get; set; }
        public IEnumerable<object> Items { get; set; } = new List<object>();
    }
}
