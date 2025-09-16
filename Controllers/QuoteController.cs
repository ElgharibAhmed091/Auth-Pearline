using AuthAPI.Data;
using AuthAPI.DTOs;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class QuoteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const decimal TAX_RATE = 0.00m; // change if needed

        public QuoteController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/quote/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuote([FromBody] QuoteRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Cart is empty.");

            // calculate total (use CasePrice or UnitPrice depending on your business rule)
            decimal subtotal = cart.Items.Sum(i =>
            {
                var price = i.Product?.CasePrice ?? i.Product?.UnitPrice ?? 0m;
                return price * i.Quantity;
            });

            decimal tax = Math.Round(subtotal * TAX_RATE, 2);
            decimal total = Math.Round(subtotal + tax, 2);

            var quote = new Quote
            {
                Email = string.IsNullOrWhiteSpace(request.Email) ? userEmail ?? string.Empty : request.Email,
                Comments = request.Comments,
                TotalPrice = total,
                CartId = cart.Id
            };

            _context.Quotes.Add(quote);

            // Optionally: clear the cart after submission
            // _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Quote submitted successfully",
                quoteId = quote.Id,
                total = quote.TotalPrice
            });
        }
    }
}
