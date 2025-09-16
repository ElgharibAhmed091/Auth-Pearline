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
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Get cart contents
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return Ok(new CartDto { Id = 0, Items = new List<CartItemDto>(), Total = 0 });

            var response = new CartDto
            {
                Id = cart.Id,
                Items = cart.Items.Select(i => new CartItemDto
                {
                    Id = i.Id,
                    ProductBarcode = i.ProductBarcode,
                    ProductName = i.Product.ProductName,
                    ProductImage = i.Product.ProductImage,
                    Quantity = i.Quantity,
                    IsCase = i.IsCase,
                    PricePerItem = i.IsCase ? i.Product.CasePrice : i.Product.UnitPrice,
                    Subtotal = i.IsCase
                        ? i.Quantity * i.Product.CasePrice
                        : i.Quantity * i.Product.UnitPrice
                }).ToList(),
                Total = cart.Items.Sum(i => i.IsCase
                    ? i.Quantity * i.Product.CasePrice
                    : i.Quantity * i.Product.UnitPrice)
            };

            return Ok(response);
        }

        // POST: Add product to cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(string barcode, int quantity = 1, bool isCase = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
            if (product == null) return NotFound("Product not found");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductBarcode == product.Barcode && i.IsCase == isCase);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductBarcode = product.Barcode,
                    Quantity = quantity,
                    IsCase = isCase
                });
            }

            await _context.SaveChangesAsync();
            return await GetCart();
        }

        // DELETE: Remove product from cart
        [HttpDelete("remove/{barcode}")]
        public async Task<IActionResult> RemoveFromCart(string barcode, bool isCase = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.ProductBarcode == barcode && i.IsCase == isCase);
            if (item == null) return NotFound("Item not found");

            cart.Items.Remove(item);
            await _context.SaveChangesAsync();

            return await GetCart();
        }

        // DELETE: Clear the whole cart
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return NotFound("Cart not found");

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart cleared successfully" });
        }
    }
}
