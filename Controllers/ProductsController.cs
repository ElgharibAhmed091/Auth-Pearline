using AuthAPI.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using testapi.Models;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET all products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category) // جلب بيانات الفئة
                .ToListAsync();

            return Ok(products);
        }

        // GET products by category
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .ToListAsync();

            return Ok(products);
        }

        // GET product by barcode
        [HttpGet("{barcode}")]
        public async Task<ActionResult<Product>> GetProduct(string barcode)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            if (product == null) return NotFound();

            return Ok(product);
        }

        // POST product
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (product.Category != null)
            {
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == product.CategoryId);
                if (existingCategory != null)
                    product.Category = existingCategory;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { barcode = product.Barcode }, product);
        }

        // PUT product
        [HttpPut("{barcode}")]
        public async Task<IActionResult> UpdateProduct(string barcode, Product updatedProduct)
        {
            if (barcode != updatedProduct.Barcode) return BadRequest();

            _context.Entry(updatedProduct).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE product
        [HttpDelete("{barcode}")]
        public async Task<IActionResult> DeleteProduct(string barcode)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Bulk add from JSON
        [HttpPost("bulk-from-json-local")]
        public async Task<IActionResult> BulkAddProductsFromLocalJson([FromBody] List<ProductJsonDto> productsJson)
        {
            var categoriesDict = await _context.Categories.ToDictionaryAsync(c => c.Name);

            foreach (var productJson in productsJson)
            {
                // التعامل مع الفئة
                if (!categoriesDict.TryGetValue(productJson.category, out var category))
                {
                    category = new Category { Name = productJson.category };
                    categoriesDict[productJson.category] = category;
                    _context.Categories.Add(category);
                }

                string ext = Path.GetExtension(productJson.productImage);
                string localImagePath = Path.Combine("wwwroot/images", productJson.barcode + ext);

                var product = new Product
                {
                    Barcode = productJson.barcode,
                    ProductName = productJson.productName,
                    Brand = productJson.brand,
                    CaseSize = productJson.caseSize,
                    CasesPerLayer = productJson.casesPerLayer,
                    CasesPerPallet = productJson.casesPerPallet,
                    LeadTimeDays = productJson.leadTimeDays,
                    CasePrice = productJson.casePrice,
                    UnitPrice = productJson.unitPrice,
                    IsAvailable = productJson.isAvailable,
                    Description = productJson.description,
                    Ingredients = productJson.ingredients,
                    Usage = productJson.usage,
                    Category = category,
                    ProductImage = "/images/" + productJson.barcode + ext
                };

                _context.Products.Add(product);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "All products added successfully!" });
        }
    }
}