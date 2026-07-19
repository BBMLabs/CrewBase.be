using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.BuildingBlocks.Observability.ErrorHandling;

/// <summary>
/// Single place every unhandled exception funnels through, producing the Problem Details shape
/// mandated by spec section 18. Never returns a stack trace to the client - the exception is
/// still logged with full detail server-side.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    ICorrelationIdAccessor correlationIdAccessor)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, title, errors) = Map(exception);

        logger.LogError(
            exception,
            "İşlenmeyen hata: {Code} {Path}",
            code, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Type = $"https://errors.rowingclub.dev/{code}",
            Title = title,
            Status = statusCode,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
        };

        problemDetails.Extensions["traceId"] = correlationIdAccessor.CorrelationId;
        problemDetails.Extensions["code"] = code;
        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Code, string Title, IDictionary<string, string[]>? Errors) Map(
        Exception exception) => exception switch
    {
        ValidationException validationException => (
            StatusCodes.Status400BadRequest,
            "validation_error",
            "Validation failed",
            validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

        NotFoundException notFoundException => (
            StatusCodes.Status404NotFound, notFoundException.ErrorCode, "Not found", null),

        AuthenticationFailedException => (
            StatusCodes.Status401Unauthorized, "unauthorized", "Unauthorized", null),

        ConcurrencyException => (
            StatusCodes.Status409Conflict, "concurrency_conflict", "Concurrency conflict", null),

        DomainException domainException => (
            StatusCodes.Status409Conflict, domainException.ErrorCode, "Conflict", null),

        UnauthorizedAccessException => (
            StatusCodes.Status403Forbidden, "forbidden", "Forbidden", null),

        _ => (StatusCodes.Status500InternalServerError, "unexpected_error", "An unexpected error occurred", null),
    };
}
