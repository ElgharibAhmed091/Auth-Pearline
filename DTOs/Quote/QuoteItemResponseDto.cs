namespace AuthAPI.DTOs.Quote
{
    public class QuoteItemResponseDto
    {
        public int Id { get; set; }
        public string? Barcode { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? ProductImage { get; set; }
        public int CaseSize { get; set; }
        public int CasesPerLayer { get; set; }
        public int CasesPerPallet { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal CasePrice { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsAvailable { get; set; }
        public string? Description { get; set; }
        public string? Ingredients { get; set; }
        public string? Usage { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int Quantity { get; set; }
        public bool IsCase { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class QuoteAdminDetailDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime DateCreated { get; set; }
        public string? UserId { get; set; }
        public List<QuoteItemResponseDto> Items { get; set; } = new();
    }

    public class QuoteAdminListDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public DateTime DateCreated { get; set; }
        public int ItemCount { get; set; }
    }

    public class PagedResponseDto<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
