using AuthAPI.Data;
using AuthAPI.DTOs.Quote;
using AuthAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Controllers.Admin
{
    [Route("api/admin/quotes")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminQuoteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminQuoteController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns all quotes (no paging). Use carefully for small datasets or exports.
        /// GET: api/admin/quotes/all
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllQuotesNoPaging()
        {
            var quotes = await _context.Quotes
                .Include(q => q.Items)
                .OrderByDescending(q => q.DateCreated)
                .ToListAsync();

            var dtos = quotes.Select(q => new QuoteAdminDetailDto
            {
                Id = q.Id,
                Email = q.Email ?? string.Empty,
                Comments = string.IsNullOrWhiteSpace(q.Comments) ? null : q.Comments,
                TotalPrice = q.TotalPrice,
                DateCreated = q.DateCreated,
                UserId = string.IsNullOrWhiteSpace(q.UserId) ? null : q.UserId,
                Items = q.Items.Select(i => new QuoteItemResponseDto
                {
                    Id = i.Id,
                    Barcode = i.Barcode,
                    ProductName = i.ProductName,
                    Brand = i.Brand,
                    ProductImage = i.ProductImage,
                    CaseSize = i.CaseSize,
                    CasesPerLayer = i.CasesPerLayer,
                    CasesPerPallet = i.CasesPerPallet,
                    LeadTimeDays = i.LeadTimeDays,
                    CasePrice = i.CasePrice,
                    UnitPrice = i.UnitPrice,
                    IsAvailable = i.IsAvailable,
                    Description = i.Description,
                    Ingredients = i.Ingredients,
                    Usage = i.Usage,
                    CategoryId = i.CategoryId,
                    CategoryName = i.CategoryName,
                    Quantity = i.Quantity,
                    IsCase = i.IsCase,
                    Subtotal = i.Subtotal
                }).ToList()
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Get paged list of quotes (admin).
        /// Query params: page, pageSize, from, to, email (optional)
        /// GET: api/admin/quotes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllQuotes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? email = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 500) pageSize = 25;

            var query = _context.Quotes
                .Include(q => q.Items)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(q => q.DateCreated >= from.Value);

            if (to.HasValue)
                query = query.Where(q => q.DateCreated <= to.Value);

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(q => q.Email != null && q.Email.Contains(email));

            var total = await query.CountAsync();

            var quotes = await query
                .OrderByDescending(q => q.DateCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = quotes.Select(q => new QuoteAdminListDto
            {
                Id = q.Id,
                Email = q.Email ?? string.Empty,
                TotalPrice = q.TotalPrice,
                DateCreated = q.DateCreated,
                ItemCount = q.Items?.Count ?? 0
            }).ToList();

            var response = new PagedResponseDto<QuoteAdminListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                Items = items
            };

            return Ok(response);
        }

        /// <summary>
        /// Get admin-level details for a specific quote id.
        /// GET: api/admin/quotes/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuoteById(int id)
        {
            var quote = await _context.Quotes
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
                return NotFound(new { message = "Quote not found" });

            var dto = new QuoteAdminDetailDto
            {
                Id = quote.Id,
                Email = quote.Email ?? string.Empty,
                Comments = string.IsNullOrWhiteSpace(quote.Comments) ? null : quote.Comments,
                TotalPrice = quote.TotalPrice,
                DateCreated = quote.DateCreated,
                UserId = string.IsNullOrWhiteSpace(quote.UserId) ? null : quote.UserId,
                Items = quote.Items.Select(i => new QuoteItemResponseDto
                {
                    Id = i.Id,
                    Barcode = i.Barcode,
                    ProductName = i.ProductName,
                    Brand = i.Brand,
                    ProductImage = i.ProductImage,
                    CaseSize = i.CaseSize,
                    CasesPerLayer = i.CasesPerLayer,
                    CasesPerPallet = i.CasesPerPallet,
                    LeadTimeDays = i.LeadTimeDays,
                    CasePrice = i.CasePrice,
                    UnitPrice = i.UnitPrice,
                    IsAvailable = i.IsAvailable,
                    Description = i.Description,
                    Ingredients = i.Ingredients,
                    Usage = i.Usage,
                    CategoryId = i.CategoryId,
                    CategoryName = i.CategoryName,
                    Quantity = i.Quantity,
                    IsCase = i.IsCase,
                    Subtotal = i.Subtotal
                }).ToList()
            };

            return Ok(dto);
        }

        /// <summary>
        /// Get all quotes for a specific userId (admin).
        /// GET: api/admin/quotes/user/{userId}
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetQuotesByUser(string userId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest(new { message = "userId is required" });

            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 500) pageSize = 50;

            var query = _context.Quotes
                .Include(q => q.Items)
                .Where(q => q.UserId == userId);

            var total = await query.CountAsync();

            var quotes = await query
                .OrderByDescending(q => q.DateCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!quotes.Any())
                return NotFound(new { message = "No quotes found for this user" });

            var items = quotes.Select(q => new QuoteAdminListDto
            {
                Id = q.Id,
                Email = q.Email ?? string.Empty,
                TotalPrice = q.TotalPrice,
                DateCreated = q.DateCreated,
                ItemCount = q.Items?.Count ?? 0
            }).ToList();

            var response = new PagedResponseDto<QuoteAdminListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                Items = items
            };

            return Ok(response);
        }

        /// <summary>
        /// DELETE: api/admin/quotes/{id}
        /// Deletes a quote (and its items) by Id
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuote(int id)
        {
            var quote = await _context.Quotes
                .Include(q => q.Items)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null)
                return NotFound(new { message = "Quote not found" });

            if (quote.Items != null && quote.Items.Any())
                _context.QuoteItems.RemoveRange(quote.Items);

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Quote deleted successfully" });
        }

        /// <summary>
        /// DELETE: api/admin/quotes/all
        /// Deletes ALL quotes (⚠️ use carefully!)
        /// </summary>
        [HttpDelete("all")]
        public async Task<IActionResult> DeleteAllQuotes()
        {
            var allQuotes = await _context.Quotes.Include(q => q.Items).ToListAsync();

            if (!allQuotes.Any())
                return NotFound(new { message = "No quotes found" });

            var allItems = allQuotes.SelectMany(q => q.Items).ToList();
            if (allItems.Any())
                _context.QuoteItems.RemoveRange(allItems);

            _context.Quotes.RemoveRange(allQuotes);
            await _context.SaveChangesAsync();

            return Ok(new { message = "All quotes deleted successfully" });
        }

    }
}
