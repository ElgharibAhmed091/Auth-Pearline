using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace testapi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        [JsonIgnore] // يمنع الحلقة المفرغة عند تحويل JSON
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
    public class Product
    {
        [Key]
        public string Barcode { get; set; }  // الـ Id

        public string ProductName { get; set; }
        public string Brand { get; set; }
        public string ProductImage { get; set; }
        public int CaseSize { get; set; }
        public int CasesPerLayer { get; set; }
        public int CasesPerPallet { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal CasePrice { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsAvailable { get; set; }
        public string Description { get; set; }
        public string Ingredients { get; set; }
        public string Usage { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
    public class ProductJsonDto
    {
        public string barcode { get; set; }
        public string productName { get; set; }
        public string brand { get; set; }
        public string productImage { get; set; }
        public int caseSize { get; set; }
        public int casesPerLayer { get; set; }
        public int casesPerPallet { get; set; }
        public int leadTimeDays { get; set; }
        public decimal casePrice { get; set; }
        public decimal unitPrice { get; set; }
        public bool isAvailable { get; set; }
        public string description { get; set; }
        public string ingredients { get; set; }
        public string usage { get; set; }
        public string category { get; set; }
    }

}
