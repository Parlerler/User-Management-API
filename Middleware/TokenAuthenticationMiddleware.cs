using System.Text.Json;

namespace UserManagementAPI.Middleware;

public class TokenAuthenticationMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<TokenAuthenticationMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] PublicPaths =
    [
        "/health",
        "/openapi"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var token = ExtractToken(context.Request);
        var validTokens = configuration.GetSection("ApiAuthentication:ValidTokens").Get<string[]>() ?? [];

        if (string.IsNullOrWhiteSpace(token) || !validTokens.Contains(token, StringComparer.Ordinal))
        {
            logger.LogWarning("Unauthorized access attempt to {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized. Valid token is required." }, JsonOptions));
            return;
        }

        await next(context);
    }

    private static string? ExtractToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var value = authHeader.ToString();
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return value["Bearer ".Length..].Trim();
            }
        }

        if (request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
        {
            var key = apiKeyHeader.ToString().Trim();
            if (!string.IsNullOrEmpty(key))
            {
                return key;
            }
        }

        return null;
    }
}