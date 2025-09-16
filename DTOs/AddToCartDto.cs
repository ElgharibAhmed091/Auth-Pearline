namespace AuthAPI.DTOs
{
    public class AddToCartDto
    {
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }
}
