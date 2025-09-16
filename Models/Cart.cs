using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AuthAPI.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        // navigation to cart items
        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
