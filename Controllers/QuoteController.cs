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


        // GET: api/quote/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyQuotes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var quotes = await _context.Quotes
                .Include(q => q.Cart)
                .ThenInclude(c => c.Items)
                .ThenInclude(i => i.Product)
                .Where(q => q.Cart.UserId == userId)
                .ToListAsync();

            if (!quotes.Any())
                return NotFound(new { message = "No quotes found for this user" });

            var response = quotes.Select(q => new
            {
                q.Id,
                q.Email,
                q.Comments,
                q.TotalPrice,
                Items = q.Cart.Items.Select(i => new
                {
                    i.Id,
                    ProductName = i.Product?.ProductName,
                    i.Quantity,
                    PricePerUnit = i.Product?.UnitPrice ?? 0m,
                    PricePerCase = i.Product?.CasePrice ?? 0m,
                    Total = (i.Product?.CasePrice ?? i.Product?.UnitPrice ?? 0m) * i.Quantity
                })
            });

            return Ok(response);
        }
        // GET: api/quote/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuoteById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var quote = await _context.Quotes
                .Include(q => q.Cart)
                .ThenInclude(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.Cart.UserId == userId);

            if (quote == null)
                return NotFound(new { message = "Quote not found" });

            var response = new
            {
                quote.Id,
                quote.Email,
                quote.Comments,
                quote.TotalPrice,
                Items = quote.Cart.Items.Select(i => new
                {
                    i.Id,
                    ProductName = i.Product?.ProductName,
                    i.Quantity,
                    PricePerUnit = i.Product?.UnitPrice ?? 0m,
                    PricePerCase = i.Product?.CasePrice ?? 0m,
                    Total = (i.Product?.CasePrice ?? i.Product?.UnitPrice ?? 0m) * i.Quantity
                })
            };

            return Ok(response);
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
