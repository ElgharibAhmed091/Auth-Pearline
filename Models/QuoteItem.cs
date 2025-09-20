using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthAPI.Models
{
    public class QuoteItem
    {
        [Key]
        public int Id { get; set; }

        public int QuoteId { get; set; }
        [JsonIgnore]
        public Quote Quote { get; set; }

        // snapshot product fields
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;

        public int CaseSize { get; set; } = 1;
        public int CasesPerLayer { get; set; } = 0;
        public int CasesPerPallet { get; set; } = 0;
        public int LeadTimeDays { get; set; } = 0;

        public decimal CasePrice { get; set; } = 0m;
        public decimal UnitPrice { get; set; } = 0m;
        public bool IsAvailable { get; set; } = false;
        public string Description { get; set; } = string.Empty;
        public string Ingredients { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;

        public int CategoryId { get; set; } = 0;
        public string CategoryName { get; set; } = string.Empty;

        // quote-specific
        public int Quantity { get; set; } = 0;
        public bool IsCase { get; set; } = false;
        public decimal Subtotal { get; set; } = 0m;
    }
}
