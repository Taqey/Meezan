using System.Net;
using System.Text.Json;
using FluentValidation;

namespace Meezan.WebApi.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        object response;

        switch (exception)
        {
            case ValidationException validationEx:
                code = HttpStatusCode.BadRequest;
                response = new
                {
                    status = (int)code,
                    title = "Validation Error",
                    errors = validationEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                };
                break;

            case KeyNotFoundException keyNotFoundEx:
                code = HttpStatusCode.NotFound;
                response = new
                {
                    status = (int)code,
                    title = "Not Found",
                    message = keyNotFoundEx.Message
                };
                break;

            case ArgumentException argEx:
                code = HttpStatusCode.BadRequest;
                response = new
                {
                    status = (int)code,
                    title = "Invalid Request",
                    message = argEx.Message
                };
                break;

            default:
                code = HttpStatusCode.InternalServerError;
                response = new
                {
                    status = (int)code,
                    title = "Server Error",
                    message = exception.Message
                };
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
