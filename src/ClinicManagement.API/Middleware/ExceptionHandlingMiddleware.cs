using ClinicManagement.Domain.Exceptions;
using System.Text.Json;

namespace ClinicManagement.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var correlationId = context.TraceIdentifier;

        var (statusCode, error) = ex switch
        {
            DomainException => (StatusCodes.Status400BadRequest, ex.Message),
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new { success = false, data = (object?)null, error, correlationId };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
