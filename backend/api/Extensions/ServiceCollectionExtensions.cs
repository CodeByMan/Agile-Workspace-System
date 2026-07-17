using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using api.Constants;
using api.Data;
using api.Dtos.Common;
using api.Filters;
using api.Health;
using api.Interfaces;
using api.Models;
using api.Options;
using api.Repositories;
using api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ConfigureForwardedHeaders(services, configuration);
        services.Configure<AuditLoggingOptions>(
            configuration.GetSection(AuditLoggingOptions.SectionName));

        services
            .AddControllers(options => options.Filters.AddService<LogActionFilter>())
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
                options.JsonSerializerOptions.ReferenceHandler =
                    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors
                            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)
                            .ToArray());

                return new BadRequestObjectResult(
                    ApiResponse<object>.Fail("Validation failed.", errors));
            };
        });

        services.AddEndpointsApiExplorer();
        services.AddMemoryCache();
        services.AddSignalR();
        services
            .AddHealthChecks()
            .AddCheck<SqlReadinessHealthCheck>("sqlserver", tags: ["ready"]);

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<IDailyUpdateService, DailyUpdateService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ResourceAuthorizationService>();
        services.AddScoped<LogActionFilter>();
        services.AddScoped<IApiLogRepository, ApiLogRepository>();
        services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
        services.AddHostedService<AuditLogRetentionService>();
        services.AddHttpClient<GitHubIssueImportService>(client =>
            client.Timeout = TimeSpan.FromSeconds(30));

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. Configure it with user secrets or environment variables.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 10;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        ConfigureAuthentication(services, configuration);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
            options.AddPolicy(
                "DeliveryLeadership",
                policy => policy.RequireRole(AppRoles.DeliveryLeadership));
            options.AddPolicy("TeamMember", policy => policy.RequireRole(AppRoles.All));
        });

        ConfigureCors(services, configuration);
        ConfigureSwagger(services);
        return services;
    }

    private static void ConfigureForwardedHeaders(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var enabled = configuration.GetValue("ForwardedHeaders:Enabled", false);
        var configuredProxies = configuration
            .GetSection("ForwardedHeaders:KnownProxies")
            .Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = enabled
                ? ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                : ForwardedHeaders.None;
            options.ForwardLimit = 1;
            options.RequireHeaderSymmetry = true;

            foreach (var configuredProxy in configuredProxies)
            {
                if (!IPAddress.TryParse(configuredProxy, out var proxyAddress))
                {
                    throw new InvalidOperationException(
                        $"ForwardedHeaders:KnownProxies contains an invalid IP address: '{configuredProxy}'.");
                }

                options.KnownProxies.Add(proxyAddress);
            }
        });
    }

    private static void ConfigureAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var signingKey = configuration["JWT:SigningKey"];
        var issuer = configuration["JWT:Issuer"];
        var audience = configuration["JWT:Audience"];

        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key is missing or shorter than 32 bytes. Configure JWT:SigningKey securely.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("JWT issuer is missing. Configure JWT:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT audience is missing. Configure JWT:Audience.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrWhiteSpace(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs/tasks"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<AppUser>>();
                        var userId = context.Principal?
                            .FindFirstValue(ClaimTypes.NameIdentifier);
                        var tokenStamp = context.Principal?
                            .FindFirstValue("security_stamp");
                        var user = string.IsNullOrWhiteSpace(userId)
                            ? null
                            : await userManager.FindByIdAsync(userId);

                        if (user is null ||
                            !user.IsActive ||
                            string.IsNullOrWhiteSpace(tokenStamp) ||
                            !string.Equals(
                                tokenStamp,
                                user.SecurityStamp,
                                StringComparison.Ordinal))
                        {
                            context.Fail(
                                "The authentication token is stale or the account is inactive.");
                        }
                    },
                    OnChallenge = async context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiResponse<object>.Fail(
                                "Authentication is required or the token is no longer valid.")));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiResponse<object>.Fail(
                                "You are not authorized to perform this action.")));
                    }
                };
            });
    }

    private static void ConfigureCors(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            var origins = (configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ??
                ["http://localhost:4200", "https://localhost:4200"])
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (origins.Length == 0 || origins.Any(origin => origin == "*"))
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins must contain explicit trusted origins and cannot use a wildcard with credentials.");
            }

            options.AddPolicy("Frontend", policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Agile Workspace API",
                Version = "v1",
                Description =
                    "Secure API for project planning, sprint execution, work-item tracking, daily updates, notifications, and reporting."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter a bearer token.",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            });
        });
    }
}
