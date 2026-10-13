using IsolationLevelInSqlWithEF.DataLayer;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Azure.Core;

namespace IsolationLevelInSqlWithEF;

public static class NonRepeatableReadsExample
{
    public static async Task ExecuteAsync()
    {
        using var testDbContext = new TestDbContext();
        var entityId = await testDbContext.TestEntities.Select(e => e.Id).FirstAsync();

        Console.WriteLine("Demonstrating Non-Repeatable Reads:");

        // Reads data twice, second read is after another transaction updates the entity.
        var readNonRepeatableData = ReadNonRepeatableDataAsync(entityId);
        await Task.Delay(1000);
        var updateWithCommit = UpdateEntityWithCommitAsync(entityId);
        await Task.WhenAll(readNonRepeatableData, updateWithCommit);

        Console.WriteLine();
        Console.WriteLine("\nDemonstrating Repeatable Reads:");

        var readRepeatableData = ReadRepeatableDataAsync(entityId);
        await Task.Delay(1000);
        updateWithCommit = UpdateEntityWithCommitAsync(entityId);
        await Task.WhenAll(readRepeatableData, updateWithCommit);
    }

    private static async Task UpdateEntityWithCommitAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();
        using var transactionToCommit = await testDbContext.Database.BeginTransactionAsync();
        var entityToUpdate = await testDbContext.TestEntities.FirstAsync(e => e.Id == entityId);
        var now = DateTime.Now;
        entityToUpdate.Name = $"Updated at {now:yyyy:MM:dd HH:mm:ss} Ticks: {now.Ticks}";
        await testDbContext.SaveChangesAsync();
        await transactionToCommit.CommitAsync();
        Console.WriteLine("Entity updated and committed.");
    }

    private static async Task ReadNonRepeatableDataAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();

        // default level is IsolationLevel.ReadCommitted, unless database setting is different
        using var transactionToRead = await testDbContext.Database.BeginTransactionAsync();

        var entityNameToReadFirstTime = await testDbContext.TestEntities.Where(e => e.Id == entityId).Select(e => e.Name).FirstAsync();
        Console.WriteLine($"Entity Name (First Read): {entityNameToReadFirstTime}");
        await Task.Delay(5000);
        var entityNameToReadSecondTime = await testDbContext.TestEntities.Where(e => e.Id == entityId).Select(e => e.Name).FirstAsync();
        Console.WriteLine($"Entity Name (Second Read): {entityNameToReadSecondTime}");
        await transactionToRead.CommitAsync();
    }

    private static async Task ReadRepeatableDataAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();

        using var transactionToRead = await testDbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var entityNameToReadFirstTime = await testDbContext.TestEntities.Where(e => e.Id == entityId).Select(e => e.Name).FirstAsync();
        Console.WriteLine($"Entity Name (First Read): {entityNameToReadFirstTime}");
        await Task.Delay(10000);
        var entityNameToReadSecondTime = await testDbContext.TestEntities.Where(e => e.Id == entityId).Select(e => e.Name).FirstAsync();
        Console.WriteLine($"Entity Name (Second Read): {entityNameToReadSecondTime}");
        await transactionToRead.CommitAsync();
    }
}
