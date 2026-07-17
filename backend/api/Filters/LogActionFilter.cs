using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using api.Extensions;
using api.Interfaces;
using api.Models.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace api.Filters;

public sealed class LogActionFilter(
    IApiLogRepository repository,
    IConfiguration configuration,
    ILogger<LogActionFilter> logger) : IAsyncActionFilter
{
    private static readonly string[] SensitiveFragments =
    [
        "password",
        "token",
        "authorization",
        "securitystamp",
        "secret",
        "connectionstring",
        "apikey",
        "accesskey"
    ];

    private const int MaxPayloadLength = 4096;
    private const long MaxRequestBodyLengthToLog = 64 * 1024;
    private const int MaxCollectionItemsToLog = 100;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!ShouldAudit(request.Path))
        {
            await next();
            return;
        }

        var sensitiveEndpoint = IsSensitiveEndpoint(request.Path);
        var log = new ApiLog
        {
            AppUserId = context.HttpContext.User.Identity?.IsAuthenticated == true
                ? context.HttpContext.User.GetUserId()
                : null,
            AppUserName = context.HttpContext.User.Identity?.IsAuthenticated == true
                ? context.HttpContext.User.GetUsername()
                : "guest",
            Uri = request.Path,
            HttpMethod = request.Method,
            RequestData = sensitiveEndpoint
                ? "[sensitive authentication payload omitted]"
                : CaptureRequestPayloads()
                    ? GetSafeRequestData(context)
                    : "[request payload capture disabled]",
            Timestamp = DateTime.UtcNow
        };

        var executed = await next();
        log.ResponseData = CaptureResponsePayloads() && !sensitiveEndpoint
            ? Sanitize(ExtractResponse(executed.Result))
            : "[response payload capture disabled]";

        try
        {
            await repository.CreateAsync(log, context.HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "API action logging failed for {Method} {Path}.",
                request.Method,
                request.Path);
        }
    }

    private bool ShouldAudit(PathString path)
    {
        if (!configuration.GetValue("AuditLogging:Enabled", true))
        {
            return false;
        }

        var excludedPrefixes = configuration
            .GetSection("AuditLogging:ExcludedPathPrefixes")
            .Get<string[]>() ?? ["/health", "/swagger", "/assets", "/favicon"];

        var pathValue = path.Value ?? string.Empty;
        return !excludedPrefixes.Any(prefix =>
            !string.IsNullOrWhiteSpace(prefix) &&
            pathValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private bool CaptureRequestPayloads() =>
        configuration.GetValue("AuditLogging:CaptureRequestPayloads", true);

    private bool CaptureResponsePayloads() =>
        configuration.GetValue("AuditLogging:CaptureResponsePayloads", false);

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;
        return pathValue.StartsWith("/api/account/login", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/api/account/register", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/api/profile/me/password", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetSafeRequestData(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (request.HasFormContentType || ContainsFileOrBinary(context.ActionArguments))
        {
            return "[form, uploaded file, or binary payload omitted]";
        }

        if (request.ContentLength is > MaxRequestBodyLengthToLog)
        {
            return "[request payload omitted because it exceeds the logging limit]";
        }

        return Sanitize(context.ActionArguments);
    }

    private static object? ExtractResponse(IActionResult? result) => result switch
    {
        FileResult => "[file response omitted]",
        ObjectResult objectResult => objectResult.Value,
        JsonResult jsonResult => jsonResult.Value,
        StatusCodeResult statusCodeResult => new { statusCodeResult.StatusCode },
        _ => null
    };

    private static string? Sanitize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (ContainsFileOrBinary(value))
        {
            return "[file or binary payload omitted]";
        }

        try
        {
            var node = JsonSerializer.SerializeToNode(value, new JsonSerializerOptions { MaxDepth = 8 });
            Redact(node);
            var text = node?.ToJsonString() ?? string.Empty;
            return text.Length <= MaxPayloadLength
                ? text
                : text[..MaxPayloadLength] + "...[truncated]";
        }
        catch
        {
            return "[payload omitted]";
        }
    }

    private static bool ContainsFileOrBinary(object? value, int depth = 0)
    {
        if (value is null || depth > 4)
        {
            return false;
        }

        if (value is IFormFile or IFormFileCollection or Stream or byte[])
        {
            return true;
        }

        if (value is string)
        {
            return false;
        }

        if (value is IDictionary dictionary)
        {
            var inspected = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (++inspected > MaxCollectionItemsToLog || ContainsFileOrBinary(entry.Value, depth + 1))
                {
                    return true;
                }
            }

            return false;
        }

        if (value is ICollection collection && collection.Count > MaxCollectionItemsToLog)
        {
            return true;
        }

        if (value is IEnumerable enumerable)
        {
            var inspected = 0;
            foreach (var item in enumerable)
            {
                if (++inspected > MaxCollectionItemsToLog || ContainsFileOrBinary(item, depth + 1))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var pair in obj.ToList())
            {
                if (SensitiveFragments.Any(fragment =>
                    pair.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
                {
                    obj[pair.Key] = "[redacted]";
                }
                else
                {
                    Redact(pair.Value);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                Redact(item);
            }
        }
    }
}
