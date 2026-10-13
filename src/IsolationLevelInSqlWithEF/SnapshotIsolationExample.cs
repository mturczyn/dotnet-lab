using IsolationLevelInSqlWithEF.DataLayer;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace IsolationLevelInSqlWithEF;

public static class SnapshotIsolationExample
{
    public static async Task ExecuteAsync()
    {
        using var testDbContext = new TestDbContext();
        var entityId = await testDbContext.TestEntities.Select(e => e.Id).FirstAsync();
        Console.WriteLine("Demonstrating Snapshot Isolation:");
        var readSnapshotData = ReadSnapshotDataAsync(entityId);
        await Task.Delay(1000);
        var updateWithCommit = UpdateEntityWithCommitAsync(entityId);
        var insertWithCommit = InsertEntityWithCommitAsync();

        await Task.WhenAll(readSnapshotData, updateWithCommit, insertWithCommit);
    }

    private static async Task ReadSnapshotDataAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();
        using var transactionToRead = await testDbContext.Database.BeginTransactionAsync(IsolationLevel.Snapshot);
        var entityNameToReadFirstTime = await testDbContext.TestEntities.Where(e => e.Id == entityId).Select(e => e.Name).FirstAsync();
        Console.WriteLine($"Entity Name (First Read): {entityNameToReadFirstTime}");

        var entityCountFirstTime = await testDbContext.TestEntities.CountAsync();
        Console.WriteLine($"Entity Count (First Read): {entityCountFirstTime}");

        await Task.Delay(5000);
        
        var entityNameToReadSecondTime = await testDbContext.TestEntities.Where(e => e.Id == entityId).Select(e => e.Name).FirstAsync();
        Console.WriteLine($"Entity Name (Second Read): {entityNameToReadSecondTime}");

        var entityCountSecondTime = await testDbContext.TestEntities.CountAsync();
        Console.WriteLine($"Entity Count (Second Read): {entityCountSecondTime}");

        await transactionToRead.CommitAsync();
    }

    private static async Task InsertEntityWithCommitAsync()
    {
        using var testDbContext = new TestDbContext();
        using var transactionToCommit = await testDbContext.Database.BeginTransactionAsync();
        var newEntity = new TestEntity { Id = Guid.NewGuid(), Name = "New Entity (Phantom Read - snapshot example)" };
        testDbContext.TestEntities.Add(newEntity);
        await testDbContext.SaveChangesAsync();
        await transactionToCommit.CommitAsync();
        Console.WriteLine("Inserted new entity and committed transaction.");
    }

    private static async Task UpdateEntityWithCommitAsync(Guid entityId)
    {
        using var testDbContext = new TestDbContext();
        using var transactionToCommit = await testDbContext.Database.BeginTransactionAsync();
        var entityToUpdate = await testDbContext.TestEntities.FirstAsync(e => e.Id == entityId);
        var now = DateTime.Now;
        var newName = $"Updated at {now:yyyy:MM:dd HH:mm:ss} Ticks: {now.Ticks}";
        entityToUpdate.Name = newName;
        await testDbContext.SaveChangesAsync();
        await transactionToCommit.CommitAsync();
        Console.WriteLine($"Entity updated (name to \"{newName}\") and committed.");
    }
}