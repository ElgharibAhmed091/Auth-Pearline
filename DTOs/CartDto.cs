using System.Collections.Generic;

namespace AuthAPI.DTOs
{
    public class CartDto
    {
        public int Id { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public decimal Total { get; set; }
    }
}


