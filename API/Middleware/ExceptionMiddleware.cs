using System.Net;
using API.Errors;
using System.Text.Json;
namespace API.Middleware;

public class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{message}", ex.Message);

            if (context.Response.HasStarted)
            {
                logger.LogWarning("The response has already started, the error handler will not be executed.");
                throw;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = env.IsDevelopment()
                ? new ApiException(context.Response.StatusCode, ex.Message, ex.StackTrace)
                : new ApiException(context.Response.StatusCode, "An unexpected error occurred.", null);

            var json = JsonSerializer.Serialize(response, _jsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}
