namespace api.Exceptions;

public abstract class ApiException(
    int statusCode,
    string errorType,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorType { get; } = errorType;
}

public sealed class BadRequestException(string message)
    : ApiException(StatusCodes.Status400BadRequest, "BadRequest", message);

public sealed class UnauthorizedException(
    string message = "Authentication is required.")
    : ApiException(StatusCodes.Status401Unauthorized, "Unauthorized", message);

public sealed class ForbiddenException(
    string message = "You are not authorized to perform this action.")
    : ApiException(StatusCodes.Status403Forbidden, "Forbidden", message);

public sealed class NotFoundException(string message)
    : ApiException(StatusCodes.Status404NotFound, "NotFound", message);

public sealed class ConflictException(string message)
    : ApiException(StatusCodes.Status409Conflict, "Conflict", message);

public sealed class ExternalServiceException(
    string message,
    int statusCode = StatusCodes.Status502BadGateway)
    : ApiException(statusCode, "ExternalServiceError", message);
