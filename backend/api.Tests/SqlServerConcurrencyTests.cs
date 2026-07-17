using api.Data;
using api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace api.Tests;

public sealed class SqlServerConcurrencyTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Project_RowVersion_Rejects_Stale_Update_On_SqlServer()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                "TEST_SQLSERVER_CONNECTION_STRING must be set for SQL Server integration tests.");
        }

        var databaseName = $"AgileWorkspaceConcurrency_{Guid.NewGuid():N}";
        var connectionBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = databaseName,
            TrustServerCertificate = true,
        };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;

        await using var setupContext = new ApplicationDbContext(options);

        try
        {
            await setupContext.Database.EnsureCreatedAsync();

            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString("N"),
                UserName = "sql-concurrency-user",
                NormalizedUserName = "SQL-CONCURRENCY-USER",
                Email = "sql-concurrency@example.invalid",
                NormalizedEmail = "SQL-CONCURRENCY@EXAMPLE.INVALID",
                FullName = "SQL Server Concurrency User",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            };
            var project = new Project
            {
                Name = "Concurrency project",
                Description = "SQL Server row-version verification",
                StartDate = DateTime.UtcNow.Date,
                CreatedByUserId = user.Id,
            };

            setupContext.Users.Add(user);
            setupContext.Projects.Add(project);
            await setupContext.SaveChangesAsync();

            await using var firstContext = new ApplicationDbContext(options);
            await using var secondContext = new ApplicationDbContext(options);
            var firstCopy = await firstContext.Projects.SingleAsync(item => item.Id == project.Id);
            var staleCopy = await secondContext.Projects.SingleAsync(item => item.Id == project.Id);

            firstCopy.Name = "First committed update";
            await firstContext.SaveChangesAsync();

            staleCopy.Name = "Stale conflicting update";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => secondContext.SaveChangesAsync());
        }
        finally
        {
            await setupContext.Database.EnsureDeletedAsync();
        }
    }
}
