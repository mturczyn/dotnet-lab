using IsolationLevelInSqlWithEF;
using IsolationLevelInSqlWithEF.DataLayer;
using Microsoft.EntityFrameworkCore;
using System.Data;

var exampleType = ExampleType.All;

await SeedDatabaseAsync();

if(exampleType.HasFlag(ExampleType.DirtyRead))
{
    Console.WriteLine("=== DIRTY READ EXAMPLE ===");
    Console.WriteLine();
    await DirtyReadExample.ExecuteAsync();
    Console.WriteLine();
}

if (exampleType.HasFlag(ExampleType.NonRepeatableRead))
{
    Console.WriteLine("=== NON-REPEATABLE READ EXAMPLE ===");
    Console.WriteLine();
    await NonRepeatableReadsExample.ExecuteAsync();
    Console.WriteLine();
}

if (exampleType.HasFlag(ExampleType.PhantomRead))
{
    Console.WriteLine("=== PHANTOM READ EXAMPLE ===");
    Console.WriteLine();
    await PhantomReadsExample.ExecuteAsync();
    Console.WriteLine();
}

if (exampleType.HasFlag(ExampleType.SnapshotIsolation))
{
    Console.WriteLine("=== SNAPSHOT ISOLATION EXAMPLE ===");
    Console.WriteLine();
    await SnapshotIsolationExample.ExecuteAsync();
    Console.WriteLine();
}

async Task SeedDatabaseAsync()
{
    using var ctx = new TestDbContext();

    if (await ctx.TestEntities.AnyAsync())
    {
        return;
    }

    ctx.TestEntities.AddRange(Enumerable.Range(1, 50).Select(i => new TestEntity { Id = Guid.NewGuid(), Name = $"Entity {i}" }));
    await ctx.SaveChangesAsync();
}