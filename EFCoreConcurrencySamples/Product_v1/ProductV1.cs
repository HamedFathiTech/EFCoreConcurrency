using System.ComponentModel.DataAnnotations;

namespace EFCoreConcurrencySamples.Product_v1;
// ReSharper disable All

public class ProductV1
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!; // SQL Server row version for concurrency
}