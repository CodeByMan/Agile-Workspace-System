using System.Text.Json;
using api.Dtos.Common;
using api.Exceptions;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Models.Log;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace api.Middlewares;

public sealed class ErrorLoggingMiddleware(
    RequestDelegate next,
    ILogger<ErrorLoggingMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context,
        IErrorLogRepository errorLogRepository,
        UserManager<AppUser> userManager)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (statusCode, errorType, clientMessage) = Map(exception);
            logger.LogError(
                exception,
                "{ErrorType} for {Method} {Path}",
                errorType,
                context.Request.Method,
                context.Request.Path);

            await TryPersistErrorAsync(
                context,
                errorLogRepository,
                userManager,
                exception,
                statusCode,
                errorType);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(ApiResponse<object>.Fail(clientMessage)),
                context.RequestAborted);
        }
    }

    private async Task TryPersistErrorAsync(
        HttpContext context,
        IErrorLogRepository errorLogRepository,
        UserManager<AppUser> userManager,
        Exception exception,
        int statusCode,
        string errorType)
    {
        try
        {
            var username = context.User.Identity?.IsAuthenticated == true
                ? context.User.GetUsername()
                : "guest";
            var user = username == "guest"
                ? null
                : await userManager.FindByNameAsync(username);

            await errorLogRepository.CreateAsync(
                new ErrorLog
                {
                    AppUserId = user?.Id,
                    AppUserName = user?.UserName ?? username,
                    Uri = context.Request.Path,
                    HttpMethod = context.Request.Method,
                    ErrorCode = statusCode,
                    ErrorType = errorType,
                    ErrorMessage = StoredErrorMessage(exception),
                    Timestamp = DateTime.UtcNow
                },
                context.RequestAborted);
        }
        catch (Exception loggingException)
        {
            logger.LogError(
                loggingException,
                "Failed to persist an error log for the original {ErrorType} exception.",
                errorType);
        }
    }

    private static (int StatusCode, string ErrorType, string Message) Map(
        Exception exception)
    {
        if (exception is ApiException apiException)
        {
            return (
                apiException.StatusCode,
                apiException.ErrorType,
                apiException.Message);
        }

        if (exception is DbUpdateConcurrencyException)
        {
            return (
                StatusCodes.Status409Conflict,
                "ConcurrencyConflict",
                "The record was changed by another request. Reload it and try again.");
        }

        if (exception is DbUpdateException
            {
                InnerException: SqlException { Number: 2601 or 2627 }
            })
        {
            return (
                StatusCodes.Status409Conflict,
                "UniqueConstraintConflict",
                "A record with the same unique values already exists.");
        }

        if (exception is UnauthorizedAccessException)
        {
            return (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required.");
        }

        return (
            StatusCodes.Status500InternalServerError,
            "UnhandledException",
            "An unexpected error occurred while processing the request.");
    }

    private static string StoredErrorMessage(Exception exception)
    {
        var message = exception is ApiException apiException
            ? apiException.Message
            : exception.GetType().Name;

        return message.Length <= 2000 ? message : message[..2000];
    }
}
