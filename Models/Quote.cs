using System;
using System.Collections.Generic;
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

        // store the user id directly for easier queries
        public string UserId { get; set; } = string.Empty;

        // navigation
        [JsonIgnore]
        public virtual Cart Cart { get; set; }

        // Quote items snapshot
        public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();

        //  Quote Status
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // default
    }
}
