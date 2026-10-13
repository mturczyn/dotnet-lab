using IsolationLevelInSqlWithEF.DataLayer;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace IsolationLevelInSqlWithEF;

public static class PhantomReadsExample
{
    public static async Task ExecuteAsync()
    {
        using var testDbContext = new TestDbContext();
        Console.WriteLine("Demonstrating Phantom Reads:");

        // Reads data twice, second time after delay and after another transaction inserts a new entity and commits
        var readPhantomData = ReadPhantomDataAsync();
        await Task.Delay(1000);
        var insertWithCommit = InsertEntityWithCommitAsync();
        await Task.WhenAll(readPhantomData, insertWithCommit);

        Console.WriteLine();
        Console.WriteLine("Phantom reads prevented:");

        var readNonPhantomData = ReadNonPhantomDataAsync();
        await Task.Delay(1000);
        insertWithCommit = InsertEntityWithCommitAsync();
        await Task.WhenAll(readNonPhantomData, insertWithCommit);
    }

    private static async Task InsertEntityWithCommitAsync()
    {
        using var testDbContext = new TestDbContext();
        using var transactionToCommit = await testDbContext.Database.BeginTransactionAsync();
        var newEntity = new TestEntity { Id = Guid.NewGuid(), Name = "New Entity (Phantom Read)" };
        testDbContext.TestEntities.Add(newEntity);
        await testDbContext.SaveChangesAsync();
        await transactionToCommit.CommitAsync();
        Console.WriteLine("Inserted new entity and committed transaction.");
    }

    private static async Task ReadNonPhantomDataAsync()
    {
        using var testDbContext = new TestDbContext();
        using var transactionToRead = await testDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entityCountFirstTime = await testDbContext.TestEntities.CountAsync();
        Console.WriteLine($"Entity Count (First Read): {entityCountFirstTime}");
        await Task.Delay(5000);
        var entityCountSecondTime = await testDbContext.TestEntities.CountAsync();
        Console.WriteLine($"Entity Count (Second Read): {entityCountSecondTime}");
        await transactionToRead.CommitAsync();
    }

    private static async Task ReadPhantomDataAsync()
    {
        using var testDbContext = new TestDbContext();
        // repeatable read isolation level allows phantom reads
        using var transactionToRead = await testDbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var entityCountFirstTime = await testDbContext.TestEntities.CountAsync();
        Console.WriteLine($"Entity Count (First Read): {entityCountFirstTime}");
        await Task.Delay(5000);
        var entityCountSecondTime = await testDbContext.TestEntities.CountAsync();
        Console.WriteLine($"Entity Count (Second Read): {entityCountSecondTime}");
        await transactionToRead.CommitAsync();
    }
}