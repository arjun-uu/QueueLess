using Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.Middlewares;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        var (statusCode, title) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation Failed"),

            BadRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad Request"),

            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource Not Found"),

            ConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict"),

            UnauthorizedException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized"),

            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Forbidden"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception is QueuelessException
                ? exception.Message
                : "An unexpected error occurred.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";


        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}