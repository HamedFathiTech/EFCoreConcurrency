using Microsoft.EntityFrameworkCore;

namespace EFCoreConcurrencySamples.Product_v2;
// ReSharper disable All
public class ProductV2DbContext : DbContext
{
    private readonly SemaphoreSlim _versioningSemaphore = new SemaphoreSlim(1, 1);
    private readonly Lock _versioningLock = new Lock();

    public DbSet<ProductV2> Products { get; set; }

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
        modelBuilder.Entity<ProductV2>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Version)
                .IsConcurrencyToken();
        });
    }

    public override int SaveChanges()
    {
        ApplyVersioning();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        await ApplyVersioningAsync();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        await ApplyVersioningAsync();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task ApplyVersioningAsync()
    {
        await _versioningSemaphore.WaitAsync();
        try
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified)
                {
                    var versionProperty = entry.Property("Version");
                    var originalVersion = (long)(entry.OriginalValues["Version"] ?? throw new InvalidOperationException());
                    var currentVersion = (long)(versionProperty.CurrentValue ?? throw new InvalidOperationException());

                    // Check if user manually changed the version
                    if (currentVersion != originalVersion && currentVersion != originalVersion + 1)
                    {
                        throw new InvalidOperationException(
                            $"Version property cannot be manually modified. " +
                            $"Expected: {originalVersion}, Got: {currentVersion}");
                    }

                    // Set correct version
                    versionProperty.CurrentValue = originalVersion + 1;
                }
            }
        }
        finally
        {
            _versioningSemaphore.Release();
        }
    }

    private void ApplyVersioning()
    {
        lock (_versioningLock)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified)
                {
                    var versionProperty = entry.Property("Version");
                    var originalVersion = (long)(entry.OriginalValues["Version"] ?? throw new InvalidOperationException());
                    var currentVersion = (long)(versionProperty.CurrentValue ?? throw new InvalidOperationException());

                    // Check if user manually changed the version
                    if (currentVersion != originalVersion && currentVersion != originalVersion + 1)
                    {
                        throw new InvalidOperationException(
                            $"Version property cannot be manually modified. " +
                            $"Expected: {originalVersion}, Got: {currentVersion}");
                    }

                    // Set correct version
                    versionProperty.CurrentValue = originalVersion + 1;
                }
            }
        }
    }

    public async Task EnsureCreatedAsync()
    {
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
    }
}