using AuthAPI.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using testapi.Models;

namespace AuthAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteItem> QuoteItems { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product primary key is Barcode
            modelBuilder.Entity<Product>()
                .HasKey(p => p.Barcode);

            // Cart -> CartItems one-to-many
            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem -> Product (FK to Product.Barcode)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany() // no navigation from Product to cartitems
                .HasForeignKey(ci => ci.ProductBarcode)
                .OnDelete(DeleteBehavior.Restrict);

            // Quote -> Cart (optional)
            modelBuilder.Entity<Quote>()
                .HasOne(q => q.Cart)
                .WithMany()
                .HasForeignKey(q => q.CartId)
                .OnDelete(DeleteBehavior.Restrict);
            // Quote -> QuoteItems one-to-many
            modelBuilder.Entity<Quote>()
                .HasMany(q => q.Items)
                .WithOne(i => i.Quote)
                .HasForeignKey(i => i.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
