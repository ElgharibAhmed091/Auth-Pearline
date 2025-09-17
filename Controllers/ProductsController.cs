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

        // DELETE all products by categoryId
        [HttpDelete("category/{categoryId}")]
        public async Task<IActionResult> DeleteProductsByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            if (!products.Any())
                return NotFound(new { message = "No products found for this category." });

            _context.Products.RemoveRange(products);
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
        [HttpDelete("{ca}")]
        public async Task<IActionResult> DeleteProduct1(string barcode)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // POST product مع صورة من الجهاز
        [HttpPost("create-with-image")]
        public async Task<IActionResult> CreateProductWithImage([FromForm] ProductWithImageDto dto)
        {
            string? imagePath = null;

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
                var filePath = Path.Combine("wwwroot/images", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(stream);
                }

                imagePath = "/images/" + fileName;
            }

            var product = new Product
            {
                Barcode = dto.Barcode,
                ProductName = dto.ProductName,
                Brand = dto.Brand,
                CategoryId = dto.CategoryId,
                UnitPrice = dto.UnitPrice,
                Description = dto.Description,
                ProductImage = imagePath,
                Ingredients = dto.Ingredients ?? "",
                Usage = dto.Usage ?? "", // أضف هذا السطر
                                         // تهيئة الحقول الإجبارية الأخرى إذا لزم الأمر
                CaseSize = 0, // قيمة افتراضية
                CasesPerLayer = 0, // قيمة افتراضية
                CasesPerPallet = 0, // قيمة افتراضية
                LeadTimeDays = 0, // قيمة افتراضية
                CasePrice = 0, // قيمة افتراضية
                IsAvailable = false // قيمة افتراضية
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }
        public class ProductWithImageDto
        {
            public string Barcode { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Brand { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public decimal UnitPrice { get; set; }
            public string? Description { get; set; }
            public string? Ingredients { get; set; }   // ✅ جديد
            public string Usage { get; set; } = string.Empty;
            public IFormFile? Image { get; set; }
        }



    }
}