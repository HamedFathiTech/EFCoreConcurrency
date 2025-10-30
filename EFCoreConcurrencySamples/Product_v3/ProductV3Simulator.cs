using Microsoft.EntityFrameworkCore;

namespace EFCoreConcurrencySamples.Product_v3;
// ReSharper disable All
public static class ProductV3Simulator
{
    // Pessimistic Concurrency
    // Philosophy: "Conflicts might happen, so lock the data to prevent them entirely."
    // https://learn.microsoft.com/en-us/ef/core/saving/concurrency
    /*
    How it works:
     - When someone reads data for modification, it gets locked
     - Other users must wait until the lock is released
     - No conflicts occur because only one user can modify at a time
     - Uses database-level locks (row locks, table locks, etc.)

    When to use?:
     - Financial transactions where conflicts must never happen
     - Critical data that can't afford conflicts
     - When conflict resolution is too complex
     - Batch processing systems
    */
    public static async Task SimulateConcurrencyConflict()
    {
        await using var context1 = new ProductV3DbContext();
        // Create the database
        await context1.EnsureCreatedAsync();
        // First record in database
        var product = new ProductV3 { Name = "Test Product", Price = 50m };
        context1.Products.Add(product);
        await context1.SaveChangesAsync();

        // Run both users concurrently
        var user1Task = SimulateUser1(product.Id);
        var user2Task = SimulateUser2(product.Id);

        // Wait for both to complete
        await Task.WhenAll(user1Task, user2Task);

        // Check final result
        await using var contextFinal = new ProductV3DbContext();
        var finalProduct = await contextFinal.Products.FindAsync(product.Id);
        Console.WriteLine($"Final product: {finalProduct?.Name}, Price: {finalProduct?.Price}");
    }

    private static async Task SimulateUser1(int productId)
    {
        await using var context = new ProductV3DbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 1 attempting to acquire lock...");

            var product = await context.Products
                .FromSqlRaw("SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", productId)
                .FirstOrDefaultAsync();

            // SQL Server: SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}
            // PostgreSQL: SELECT * FROM Products WHERE Id = $1 FOR UPDATE;
            // MySQL: SELECT * FROM Products WHERE Id = ? FOR UPDATE;
            // Oracle: SELECT * FROM Products WHERE Id = :1 FOR UPDATE;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 1 acquired lock on product");

            // Simulate some processing time
            await Task.Delay(2000);

            product!.Name = "Product 1";
            product.Price = 100m;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 1 completed update");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 1 failed: {ex.Message}");
            throw;
        }
    }

    private static async Task SimulateUser2(int productId)
    {
        // Add a small delay so User 1 starts first
        await Task.Delay(100);

        await using var context = new ProductV3DbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 2 attempting to acquire lock...");

            var product = await context.Products
                .FromSqlRaw("SELECT * FROM Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", productId)
                .FirstOrDefaultAsync();

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 2 acquired lock on product");

            product!.Name = "Product 2";
            product.Price = 200m;
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 2 completed update");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User 2 failed: {ex.Message}");
            throw;
        }
    }
}