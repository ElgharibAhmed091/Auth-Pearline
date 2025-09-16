namespace AuthAPI.DTOs
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public string ProductBarcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsCase { get; set; }
        public decimal PricePerItem { get; set; }
        public decimal Subtotal { get; set; }
    }
}
