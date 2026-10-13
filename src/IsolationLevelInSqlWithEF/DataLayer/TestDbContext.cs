using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IsolationLevelInSqlWithEF.DataLayer;

public class TestDbContext : DbContext
{
    public const string ConnectionString = "Server=localhost;Database=TestDb;User Id=sa;Password=myStrong(!)Password;TrustServerCertificate=true";

    public DbSet<TestEntity> TestEntities { get; set; }

    public TestDbContext() { }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConnectionString);
    }
}

public class TestEntity
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; }
}