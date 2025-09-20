public class CartItemDto
{
    public int Id { get; set; }
    public string ProductBarcode { get; set; }
    public string ProductName { get; set; }
    public string ProductImage { get; set; }
    public int Quantity { get; set; }
    public bool IsCase { get; set; }
    public decimal PricePerItem { get; set; }
    public decimal Subtotal { get; set; }

    public int CaseSize { get; set; }        
    public int CasesPerLayer { get; set; }
    public int CasesPerPallet { get; set; }
    public int LeadTimeDays { get; set; }
}
