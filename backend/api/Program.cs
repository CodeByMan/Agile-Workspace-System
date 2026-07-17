using api.Data;
using api.Extensions;
using api.Health;
using api.Hubs;
using api.Middlewares;
using api.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsEnvironment("Testing"))
            await context.Database.EnsureCreatedAsync();
        else if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
            await context.Database.MigrateAsync();

        if (builder.Configuration.GetValue<bool>("Seed:EnableDemoData"))
        {
            if (!app.Environment.IsDevelopment())
                throw new InvalidOperationException("Demo data may only be enabled in the Development environment.");

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            await DemoDataSeeder.SeedAsync(context, userManager, builder.Configuration);
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Application startup failed during required database initialization.");
        throw;
    }
}

if (builder.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    app.UseForwardedHeaders();
}
app.UseMiddleware<ErrorLoggingMiddleware>();

if (!app.Environment.IsDevelopment() && builder.Configuration.GetValue("Https:UseHsts", true))
    app.UseHsts();
if (builder.Configuration.GetValue("Https:Redirect", true))
    app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TaskCollaborationHub>("/hubs/tasks");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

public partial class Program { }
