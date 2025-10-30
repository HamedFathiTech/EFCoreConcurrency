using Microsoft.EntityFrameworkCore;

namespace EFCoreConcurrencySamples.Product_v1;
// ReSharper disable All
public class ProductV1DbContext : DbContext
{
    public DbSet<ProductV1> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder
                .UseSqlServer(
                    "Server=127.0.0.1,4433;Database=EFCoreConcurrencyTest;User Id=sa;Password=P@ssW0rd;TrustServerCertificate=true;MultipleActiveResultSets=true");
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductV1>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.RowVersion)
                .IsRowVersion();
        });
    }

    public async Task EnsureCreatedAsync()
    {
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
    }
}