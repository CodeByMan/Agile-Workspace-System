using System.Data;
using api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace api.Tests;

public sealed class AgileWorkspaceFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_connection.State != ConnectionState.Open) _connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=test;Database=test;User Id=test;Password=test;TrustServerCertificate=True",
                ["JWT:SigningKey"] = "tests-only-signing-key-with-at-least-32-bytes",
                ["JWT:Issuer"] = "AgileWorkspace.Tests",
                ["JWT:Audience"] = "AgileWorkspace.Tests",
                ["JWT:LifetimeMinutes"] = "60",
                ["Https:Redirect"] = "false",
                ["Https:UseHsts"] = "false",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Seed:EnableDemoData"] = "false"
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
