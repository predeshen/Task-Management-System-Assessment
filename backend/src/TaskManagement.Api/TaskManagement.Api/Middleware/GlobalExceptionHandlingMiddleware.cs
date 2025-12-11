using System.Net;
using System.Text.Json;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions
/// and returns consistent error responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid request parameters";
                response.Errors.Add(new ValidationError { Field = "Request", Message = exception.Message });
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                response.Errors.Add(new ValidationError { Field = "Authorization", Message = exception.Message });
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Resource not found";
                response.Errors.Add(new ValidationError { Field = "Resource", Message = exception.Message });
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid operation";
                response.Errors.Add(new ValidationError { Field = "Operation", Message = exception.Message });
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An internal server error occurred";
                response.Errors.Add(new ValidationError { Field = "Server", Message = "Please contact support if the problem persists" });
                break;
        }

        response.TraceId = context.TraceIdentifier;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

