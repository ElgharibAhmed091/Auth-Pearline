namespace AuthAPI.DTOs
{
    public class QuoteSummaryDto
    {
        public decimal QuoteSubtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal QuoteTotal { get; set; }
    }
}
