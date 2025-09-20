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
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var quotes = await _context.Quotes
                .Include(q => q.Items)
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.DateCreated)
                .ToListAsync();

            if (!quotes.Any())
                return NotFound(new { message = "No quotes found for this user" });

            var response = quotes.Select(q => new
            {
                q.Id,
                q.Email,
                q.Comments,
                q.TotalPrice,
                q.DateCreated,
                Items = q.Items.Select(i => new
                {
                    i.Id,
                    i.Barcode,
                    i.ProductName,
                    i.Brand,
                    i.ProductImage,
                    i.CaseSize,
                    i.CasesPerLayer,
                    i.CasesPerPallet,
                    i.LeadTimeDays,
                    i.CasePrice,
                    i.UnitPrice,
                    i.IsAvailable,
                    i.Description,
                    i.Ingredients,
                    i.Usage,
                    CategoryId = i.CategoryId,
                    CategoryName = i.CategoryName,
                    i.Quantity,
                    i.IsCase,
                    i.Subtotal
                })
            });

            return Ok(response);
        }

        // GET: api/quote/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuoteById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var quote = await _context.Quotes
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId);

            if (quote == null)
                return NotFound(new { message = "Quote not found" });

            var response = new
            {
                quote.Id,
                quote.Email,
                quote.Comments,
                quote.TotalPrice,
                quote.DateCreated,
                Items = quote.Items.Select(i => new
                {
                    i.Id,
                    i.Barcode,
                    i.ProductName,
                    i.Brand,
                    i.ProductImage,
                    i.CaseSize,
                    i.CasesPerLayer,
                    i.CasesPerPallet,
                    i.LeadTimeDays,
                    i.CasePrice,
                    i.UnitPrice,
                    i.IsAvailable,
                    i.Description,
                    i.Ingredients,
                    i.Usage,
                    CategoryId = i.CategoryId,
                    CategoryName = i.CategoryName,
                    i.Quantity,
                    i.IsCase,
                    i.Subtotal
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

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.Items.Any())
                return BadRequest("Cart is empty.");

            decimal subtotal = 0m;
            var quote = new Quote
            {
                Email = string.IsNullOrWhiteSpace(request.Email) ? userEmail ?? string.Empty : request.Email,
                Comments = request.Comments,
                CartId = cart.Id,
                UserId = userId
            };

            // create QuoteItems snapshot
            foreach (var ci in cart.Items)
            {
                var p = ci.Product;
                var price = ci.IsCase ? (p?.CasePrice ?? 0m) : (p?.UnitPrice ?? 0m);
                var lineSubtotal = price * ci.Quantity;
                subtotal += lineSubtotal;

                var qi = new QuoteItem
                {
                    Barcode = p?.Barcode ?? ci.ProductBarcode,
                    ProductName = p?.ProductName ?? string.Empty,
                    Brand = p?.Brand ?? string.Empty,
                    ProductImage = p?.ProductImage ?? string.Empty,
                    CaseSize = p?.CaseSize ?? 1,
                    CasesPerLayer = p?.CasesPerLayer ?? 0,
                    CasesPerPallet = p?.CasesPerPallet ?? 0,
                    LeadTimeDays = p?.LeadTimeDays ?? 0,
                    CasePrice = p?.CasePrice ?? 0m,
                    UnitPrice = p?.UnitPrice ?? 0m,
                    IsAvailable = p?.IsAvailable ?? false,
                    Description = p?.Description ?? string.Empty,
                    Ingredients = p?.Ingredients ?? string.Empty,
                    Usage = p?.Usage ?? string.Empty,
                    CategoryId = p?.CategoryId ?? 0,
                    CategoryName = p?.Category?.Name ?? string.Empty,

                    Quantity = ci.Quantity,
                    IsCase = ci.IsCase,
                    Subtotal = lineSubtotal
                };

                quote.Items.Add(qi);
            }

            decimal tax = Math.Round(subtotal * TAX_RATE, 2);
            quote.TotalPrice = Math.Round(subtotal + tax, 2);

            _context.Quotes.Add(quote);

            // optional: clear cart after submission
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
