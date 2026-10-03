using System.Text.Json;
using FluentValidation;

namespace SariSariPOS.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            await HandleAsync(ctx, ex);
        }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, code) = ex switch
        {
            ValidationException => (400, "VALIDATION_ERROR"),
            ArgumentException => (400, "BAD_REQUEST"),
            UnauthorizedAccessException => (401, "UNAUTHORIZED"),
            KeyNotFoundException => (404, "NOT_FOUND"),
            InvalidOperationException => (409, "CONFLICT"),
            _ => (500, "INTERNAL_ERROR"),
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning(ex, "Handled exception: {Message}", ex.Message);

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";

        object body = ex switch
        {
            ValidationException ve => new
            {
                error = "Validation failed",
                code,
                details = ve.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            },
            _ => new
            {
                error = status >= 500 ? "An unexpected error occurred" : ex.Message,
                code,
                details = (object?)null,
            },
        };

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}