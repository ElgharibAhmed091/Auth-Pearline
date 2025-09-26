using System.ComponentModel.DataAnnotations;
using AuthAPI.Models;

namespace AuthAPI.DTOs.Quote
{
    public class UpdateQuoteStatusDto
    {
        [Required]
        public QuoteStatus Status { get; set; }
    }
}
