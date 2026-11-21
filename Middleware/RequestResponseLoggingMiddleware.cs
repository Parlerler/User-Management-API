using System.Diagnostics;

namespace UserManagementAPI.Middleware;

public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        logger.LogInformation("Incoming Request: {Method} {Path}", method, path);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            logger.LogInformation(
                "Outgoing Response: {Method} {Path} -> {StatusCode} ({ElapsedMs} ms)",
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}