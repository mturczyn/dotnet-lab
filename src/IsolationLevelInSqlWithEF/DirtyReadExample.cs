using Microsoft.EntityFrameworkCore;
using IsolationLevelInSqlWithEF.DataLayer;
using System.Data;

namespace IsolationLevelInSqlWithEF;

public static class DirtyReadExample
{
    public static async Task ExecuteAsync()
    {
        using var testDbContext = new TestDbContext();

        var entityId = await testDbContext.TestEntities.Select(e => e.Id).FirstAsync();

        var updateWithRollback = UpdateEntityWithRollbackAsync(entityId);
        await Task.Delay(1000);
        var readDirtyData = ReadDirtyDataAsync(entityId);
        await Task.WhenAll(updateWithRollback, readDirtyData);

        var entityAtTheEnd = await testDbContext.TestEntities.FirstAsync(e => e.Id == entityId);
        
        Console.WriteLine($"Entity Name (final value, after dirty read example): {entityAtTheEnd.Name}");
        Console.WriteLine();

        Console.WriteLine("Dirty reads prevented (Read Committed Isolation Level):");

        updateWithRollback = UpdateEntityWithRollbackAsync(entityId);
        await Task.Delay(1000);
        var readCleanData = ReadCleanDataAsync(entityId);
        await Task.WhenAll(updateWithRollback, readCleanData);
        Console.WriteLine($"Entity Name (final value, after clean read example): {entityAtTheEnd.Name}");
    }

    static async Task ReadCleanDataAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();
        using var transactionToRead = await testDbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var entityToRead = await testDbContext.TestEntities.FirstAsync(e => e.Id == entityId);
        Console.WriteLine($"Entity Name (Clean Read): {entityToRead.Name}");
        await transactionToRead.CommitAsync();
    }

    static async Task ReadDirtyDataAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();
        using var transactionToRead = await testDbContext.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
        var entityToRead = await testDbContext.TestEntities.FirstAsync(e => e.Id == entityId);
        Console.WriteLine($"Entity Name (Dirty Read): {entityToRead.Name}");
        await transactionToRead.CommitAsync();
    }

    static async Task UpdateEntityWithRollbackAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();

        using var transactionToRollback = await testDbContext.Database.BeginTransactionAsync();
        
        var entityToUpdate = await testDbContext.TestEntities.FirstAsync(e => e.Id == entityId);

        entityToUpdate.Name = "Updated Name, which will be rolled back - DIRTY READ";

        await testDbContext.SaveChangesAsync();

        await Task.Delay(5000);

        await transactionToRollback.RollbackAsync();

    }
}
