using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthAPI.Models
{
    public class Quote
    {
        [Key]
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Comments { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public int CartId { get; set; }

        [JsonIgnore]
        public virtual Cart Cart { get; set; }
    }
}
