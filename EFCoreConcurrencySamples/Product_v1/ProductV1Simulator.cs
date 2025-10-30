using Microsoft.EntityFrameworkCore;

namespace EFCoreConcurrencySamples.Product_v1;
// ReSharper disable All

// Optimistic Concurrency
// Philosophy: "Conflicts are rare, so don't lock anything. Just check for conflicts when saving."
// https://learn.microsoft.com/en-us/ef/core/saving/concurrency
// EF Core default
/*
How it works:
 - Multiple users can read and modify the same data simultaneously
 - No locks are placed on the data
 - Conflict detection happens only at save time
 - Uses techniques like timestamps, version numbers, or comparing original values

When to use:
 - Web applications with many concurrent users
 - Read-heavy workloads
 - When conflicts are rare
 - When you can handle conflict resolution gracefully
*/
public static class ProductV1Simulator
{
    public static async Task SimulateConcurrencyConflict()
    {
        // Create initial product
        await using var context1 = new ProductV1DbContext();

        // Create the database
        await context1.EnsureCreatedAsync();

        // First record in database
        var product = new ProductV1 { Name = "Test Product", Price = 50m };
        context1.Products.Add(product);
        await context1.SaveChangesAsync();

        // Load the same product in two different contexts (simulating two users)
        await using var context2 = new ProductV1DbContext(); // User 1's context
        await using var context3 = new ProductV1DbContext(); // User 2's context

        var product1 = await context2.Products.FindAsync(product.Id);
        var product2 = await context3.Products.FindAsync(product.Id);

        // User 1 updates the product
        // Database scope (databaseValues)
        product1!.Name = "Product 1";
        product1!.Price = 100m;
        await context2.SaveChangesAsync(); // This succeeds

        // Loaded context before changes so still thinks the product is the original one
        // To avoid:
        // var latestProduct = await context3.Products.FindAsync(product.Id);
        // User 2 tries to update the same product
        // Client scope (proposedValues)
        product2!.Name = "Product 2";
        product2!.Price = 200m;

        try
        {
            await context3.SaveChangesAsync(); // This will throw DbUpdateConcurrencyException
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // DATABASE WINS (databaseValue): Database values (User 1's changes: Price = 100m, Name = Product 1) will be kept
            // Client values (User 2's changes: Price = 200m, Name = Product 2) will be discarded
            // 
            context3.ResolveConcurrency<ProductV1>(ex, ConflictResolutionStrategy.DatabaseWins);

            // CLIENT WINS (proposedValue): Client values (User 2's changes: Price = 200m, Name = Product 2) will override database
            // Database values (User 1's changes: Price = 100m, Name = Product 1) will be discarded
            // context3.ResolveConcurrency<ProductV1>(ex, ConflictResolutionStrategy.ClientWins);

            // CUSTOM RESOLUTION: Custom logic determines which values win per property
            // In this case:
            // Higher price wins (Max of 200m vs 100m = 200m will be kept)
            // Name: Current database's value will keep.
            /*
            context3.ResolveConcurrency<ProductV1>(ex, ConflictResolutionStrategy.Custom,
                (propertyEntry, proposedValue, databaseValue) =>
                {
                    // Keep the database's name
                    // DATABASE WINS (databaseValue) - User 1's changes: Name = "Test Product"
                    if (propertyEntry.Metadata.Name == "Name")
                        return databaseValue;

                    // Keep the higher price
                    // CLIENT WINS (proposedValue) - User 2's changes: Price = 200m
                    if (propertyEntry.Metadata.Name == "Price")
                        return Math.Max((decimal)proposedValue!, (decimal)databaseValue!);

                    // WARNING: Don't mix values and keep your database consistent!

                    return databaseValue;
                });
            */

            await context3.SaveChangesAsync(); // Retry

            // Verify the final result
            var finalProduct = await context3.Products.FindAsync(product.Id);
        }
    }
}

