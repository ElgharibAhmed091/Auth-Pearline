using System;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Models
{
    public class OtpCode
    {
        [Key]
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        public DateTime ExpirationTimeUtc { get; set; }

        public bool IsUsed { get; set; } = false;
    }
}
