using System.Net;
using api.Controllers;
using api.Extensions;
using api.Interfaces;
using api.Options;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace api.Tests;

public sealed class ArchitectureConsistencyTests
{
    [Theory]
    [InlineData(typeof(SprintsController), typeof(ISprintService))]
    [InlineData(typeof(DailyUpdatesController), typeof(IDailyUpdateService))]
    public void DataHeavyControllersDependOnFocusedServices(
        Type controllerType,
        Type expectedServiceType)
    {
        var constructor = Assert.Single(controllerType.GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(expectedServiceType, parameter.ParameterType);
    }

    [Fact]
    public void NotificationContractContainsOnlyUsedReadAndMutationOperations()
    {
        var methodNames = typeof(INotificationService)
            .GetMethods()
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            ["GetForUserAsync", "MarkAllAsReadAsync", "MarkAsReadAsync"],
            methodNames);
    }

    [Fact]
    public void ForwardedHeadersTrustOnlyConfiguredProxyWhenEnabled()
    {
        var configuration = Configuration(
            ("ForwardedHeaders:Enabled", "true"),
            ("ForwardedHeaders:KnownProxies:0", "172.28.0.10"),
            ("AuditLogging:RetentionDays", "45"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationServices(configuration);

        using var provider = services.BuildServiceProvider();
        var forwarded = provider
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;
        var audit = provider
            .GetRequiredService<IOptions<AuditLoggingOptions>>()
            .Value;

        Assert.Equal(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            forwarded.ForwardedHeaders);
        Assert.Contains(IPAddress.Parse("172.28.0.10"), forwarded.KnownProxies);
        Assert.Equal(1, forwarded.ForwardLimit);
        Assert.True(forwarded.RequireHeaderSymmetry);
        Assert.Equal(45, audit.RetentionDays);
    }

    private static IConfiguration Configuration(
        params (string Key, string Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=test;Database=test;User Id=test;Password=test;TrustServerCertificate=True",
            ["JWT:Issuer"] = "AgileWorkspace.Tests",
            ["JWT:Audience"] = "AgileWorkspace.Tests.Client",
            ["JWT:SigningKey"] = "test-signing-key-that-is-at-least-32-bytes-long"
        };

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
