using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using testapi.Models;

namespace AuthAPI.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; }

        public string ProductBarcode { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }

        public bool IsCase { get; set; }
    }
}
