using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests;

public sealed class ModelIntegrityTests : IClassFixture<AgileWorkspaceFactory>
{
    private readonly AgileWorkspaceFactory _factory;
    public ModelIntegrityTests(AgileWorkspaceFactory factory) => _factory = factory;

    [Fact]
    public void DailyUpdate_HasRequiredUniqueProjectUserDateIndex()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entity = context.Model.FindEntityType(typeof(DailyUpdate))!;
        var index = entity.GetIndexes().Single(x => x.Properties.Select(p => p.Name)
            .SequenceEqual([nameof(DailyUpdate.ProjectId), nameof(DailyUpdate.UserId), nameof(DailyUpdate.UpdateDate)]));
        Assert.True(index.IsUnique);
    }

    [Theory]
    [InlineData(typeof(Project))]
    [InlineData(typeof(Sprint))]
    [InlineData(typeof(TaskItem))]
    public void MutableAggregate_HasConcurrencyToken(Type entityType)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var property = context.Model.FindEntityType(entityType)!.FindProperty("RowVersion");
        Assert.NotNull(property);
        Assert.True(property!.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
    }
}
