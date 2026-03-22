using FluentValidation;
using LottyAB.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, title, errors) = exception switch
        {
            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                new Dictionary<string, object> { ["message"] = notFoundEx.Message }
            ),
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                new Dictionary<string, object> { ["errors"] = validationEx.Errors }
            ),
            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                "Conflict",
                new Dictionary<string, object> { ["message"] = conflictEx.Message }
            ),
            UnauthorizedException unauthorizedEx => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                new Dictionary<string, object> { ["message"] = unauthorizedEx.Message }
            ),
            UnprocessableEntityException unprocessableEntityEx => (
                StatusCodes.Status422UnprocessableEntity,
                "Unprocessable Entity",
                new Dictionary<string, object> { ["message"] = unprocessableEntityEx.Message }
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                new Dictionary<string, object> { ["message"] = "An unexpected error occurred." }
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        foreach (var kvp in errors)
            problemDetails.Extensions[kvp.Key] = kvp.Value;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}