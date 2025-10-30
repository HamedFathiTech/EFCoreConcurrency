using System.ComponentModel.DataAnnotations;

namespace EFCoreConcurrencySamples.Product_v2;
// ReSharper disable All
public class ProductV2
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    [ConcurrencyCheck]
    public long Version { get; set; } // Non-SQL Server concurrency check
    // public Guid Version { get; set; } // Alternative: use Guid for concurrency check
}