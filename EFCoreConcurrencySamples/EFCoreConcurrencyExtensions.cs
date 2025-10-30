using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace EFCoreConcurrencySamples;
// ReSharper disable All

// https://learn.microsoft.com/en-us/ef/core/saving/concurrency

/// <summary>
/// Defines strategies for resolving concurrency conflicts.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// Client values always win over database values.
    /// Client = Proposed values (the values the current user is trying to save)
    /// Database = Current values stored in the database (potentially modified by another user)
    /// Result: The current user's changes will override any database changes
    /// </summary>
    ClientWins,

    /// <summary>
    /// Database values always win over client values.
    /// Client = Proposed values (the values the current user is trying to save)
    /// Database = Current values stored in the database (potentially modified by another user)
    /// Result: The database's current values will be kept, discarding the current user's changes
    /// </summary>
    DatabaseWins,

    /// <summary>
    /// Use custom resolution logic provided by the caller.
    /// Client = Proposed values (the values the current user is trying to save)
    /// Database = Current values stored in the database (potentially modified by another user)
    /// Result: Custom function determines which values to keep on a property-by-property basis
    /// </summary>
    Custom
}

public static class EFCoreConcurrencyExtensions
{
    /// <summary>
    /// Handles concurrency exceptions by resolving conflicts based on the provided strategy.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="ex">The DbUpdateConcurrencyException to handle.</param>
    /// <param name="strategy">The conflict resolution strategy to use.</param>
    /// <param name="resolveConflict">The function to resolve property conflicts when using Custom strategy. 
    /// Parameters: 
    /// - PropertyEntry propertyEntry: The property being resolved
    /// - object? proposedValue: The CLIENT value (what the current user wants to save)
    /// - object? databaseValue: The DATABASE value (what's currently stored, possibly changed by another user)
    /// Returns: object? resolvedValue (the final value to save)
    /// </param>
    /// <exception cref="NotSupportedException">Thrown when the entity type is not supported.</exception>
    /// <exception cref="ArgumentException">Thrown when Custom strategy is specified without providing resolveConflict function.</exception>
    public static void ResolveConcurrency<TEntity>(
        this DbContext context,
        DbUpdateConcurrencyException ex,
        ConflictResolutionStrategy strategy,
        Func<PropertyEntry, object?, object?, object?>? resolveConflict = null)
        where TEntity : class
    {
        if (strategy == ConflictResolutionStrategy.Custom && resolveConflict == null)
        {
            throw new ArgumentException(
                "Custom conflict resolution function must be provided when using Custom strategy.",
                nameof(resolveConflict));
        }

        foreach (var entry in ex.Entries)
        {
            if (entry.Entity is TEntity)
            {
                var proposedValues = entry.CurrentValues;
                var databaseValues = entry.GetDatabaseValues();

                if (databaseValues == null)
                {
                    throw new InvalidOperationException(
                        "The entity no longer exists in the database.");
                }

                foreach (var property in proposedValues.Properties)
                {
                    var propertyEntry = entry.Property(property.Name);
                    var proposedValue = proposedValues[property];
                    var databaseValue = databaseValues[property];

                    proposedValues[property] = strategy switch
                    {
                        ConflictResolutionStrategy.ClientWins => proposedValue,
                        ConflictResolutionStrategy.DatabaseWins => databaseValue,
                        ConflictResolutionStrategy.Custom => resolveConflict!(propertyEntry, proposedValue, databaseValue),
                        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Invalid conflict resolution strategy.")
                    };
                }

                entry.OriginalValues.SetValues(databaseValues);
            }
            else
            {
                throw new NotSupportedException(
                    $"Don't know how to handle concurrency conflicts for {entry.Metadata.Name}");
            }
        }
    }

    /// <summary>
    /// Handles concurrency exceptions by resolving conflicts based on the provided strategy.
    /// Overload that maintains backward compatibility with your original method signature.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="ex">The DbUpdateConcurrencyException to handle.</param>
    /// <param name="resolveConflict">The function to resolve property conflicts. 
    /// Parameters: 
    /// - PropertyEntry propertyEntry: The property being resolved
    /// - object? proposedValue: The CLIENT value (what the current user wants to save)
    /// - object? databaseValue: The DATABASE value (what's currently stored, possibly changed by another user)
    /// Returns: object? resolvedValue (the final value to save)
    /// </param>
    /// <exception cref="NotSupportedException">Thrown when the entity type is not supported.</exception>
    public static void ResolveConcurrency<TEntity>(
        this DbContext context,
        DbUpdateConcurrencyException ex,
        Func<PropertyEntry, object?, object?, object?> resolveConflict)
        where TEntity : class
    {
        ResolveConcurrency<TEntity>(context, ex, ConflictResolutionStrategy.Custom, resolveConflict);
    }
}