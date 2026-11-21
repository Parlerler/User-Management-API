using System.ComponentModel.DataAnnotations;
using UserManagementAPI.DTOs;
using UserManagementAPI.Middleware;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IUserService, InMemoryUserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Task 3, Step 5: Exact middleware order required
// 1. Error-handling middleware first
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Authentication middleware next
app.UseMiddleware<TokenAuthenticationMiddleware>();

// 3. Logging middleware last
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var usersGroup = app.MapGroup("/api/users")
    .WithTags("Users");

// GET /api/users?page=1&pageSize=20&department=IT&search=david
usersGroup.MapGet("/", async (
    IUserService userService,
    CancellationToken ct,
    int page = 1,
    int pageSize = 20,
    string? department = null,
    string? search = null) =>
{
    try
    {
        var result = await userService.GetUsersAsync(page, pageSize, department, search, ct);
        return Results.Ok(result);
    }
    catch (Exception)
    {
        throw; // Handled centrally by ExceptionHandlingMiddleware
    }
});

// GET /api/users/{id}
usersGroup.MapGet("/{id:int}", async (int id, IUserService userService, CancellationToken ct) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { error = "User ID must be a positive integer." });
    }

    try
    {
        var user = await userService.GetUserByIdAsync(id, ct);
        return user is null
            ? Results.NotFound(new { error = $"User with ID {id} was not found." })
            : Results.Ok(user);
    }
    catch (Exception)
    {
        throw;
    }
});

// POST /api/users
usersGroup.MapPost("/", async (CreateUserRequest request, IUserService userService, CancellationToken ct) =>
{
    var validationErrors = ValidateObject(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    try
    {
        var created = await userService.CreateUserAsync(request, ct);
        return Results.Created($"/api/users/{created.Id}", created);
    }
    catch (Exception)
    {
        throw;
    }
});

// PUT /api/users/{id}
usersGroup.MapPut("/{id:int}", async (int id, UpdateUserRequest request, IUserService userService, CancellationToken ct) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { error = "User ID must be a positive integer." });
    }

    var validationErrors = ValidateObject(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    try
    {
        var updated = await userService.UpdateUserAsync(id, request, ct);
        return updated is null
            ? Results.NotFound(new { error = $"User with ID {id} was not found." })
            : Results.Ok(updated);
    }
    catch (Exception)
    {
        throw;
    }
});

// DELETE /api/users/{id}
usersGroup.MapDelete("/{id:int}", async (int id, IUserService userService, CancellationToken ct) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { error = "User ID must be a positive integer." });
    }

    try
    {
        var deleted = await userService.DeleteUserAsync(id, ct);
        return deleted
            ? Results.NoContent()
            : Results.NotFound(new { error = $"User with ID {id} was not found." });
    }
    catch (Exception)
    {
        throw;
    }
});

// Test endpoint to verify unhandled exception -> 500 JSON error via ExceptionHandlingMiddleware
usersGroup.MapGet("/simulate-error", () =>
{
    throw new InvalidOperationException("Simulated internal failure for middleware verification.");
});

app.Run();

static Dictionary<string, string[]> ValidateObject(object instance)
{
    var context = new ValidationContext(instance);
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(instance, context, results, validateAllProperties: true);

    return results
        .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "general")
        .ToDictionary(
            g => g.Key,
            g => g.Select(r => r.ErrorMessage ?? "Invalid value.").ToArray()
        );
}