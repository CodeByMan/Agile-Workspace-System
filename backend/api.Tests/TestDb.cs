using api.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Tests;

internal static class TestDb
{
    public static ApplicationDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"agile-tests-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
