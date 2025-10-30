using EFCoreConcurrencySamples.Product_v1;
using EFCoreConcurrencySamples.Product_v2;
using EFCoreConcurrencySamples.Product_v3;

namespace EFCoreConcurrencySamples;
// ReSharper disable All

internal class Program
{
    /*
    Database Concurrency
      Multiple users/processes accessing database simultaneously
    
    Problems to avoid:
      - Lost updates (changes overwritten)
      - Dirty reads (reading uncommitted data)
      - Data inconsistency

    EF Core Solutions

      1. Optimistic Concurrency:
        - Add [Timestamp] or [ConcurrencyCheck] property to entities
        - EF Core checks for changes before saving
        - Throws DbUpdateConcurrencyException if conflict detected

      2. Pessimistic Concurrency:
        - Lock data during transactions
        - Use raw SQL with locking hints

    Best Practice: 
      Use optimistic concurrency with proper exception handling for most scenarios.
    */
    static async Task Main(string[] args)
    {
        await ProductV1Simulator.SimulateConcurrencyConflict();
        await ProductV2Simulator.SimulateConcurrencyConflict();
        await ProductV3Simulator.SimulateConcurrencyConflict();
    }
}

