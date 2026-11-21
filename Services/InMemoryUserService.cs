using System.Collections.Concurrent;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class InMemoryUserService : IUserService
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _currentId;

    public InMemoryUserService()
    {
        SeedInitialUsers();
    }

    private void SeedInitialUsers()
    {
        var seed = new[]
        {
            new User { FullName = "Sarah Jenkins", Email = "sarah.jenkins@techhive.io", Department = "HR", Role = "HR Manager" },
            new User { FullName = "David Chen", Email = "david.chen@techhive.io", Department = "IT", Role = "Systems Admin" },
            new User { FullName = "Amira Hassan", Email = "amira.hassan@techhive.io", Department = "Engineering", Role = "Backend Engineer" }
        };

        foreach (var user in seed)
        {
            user.Id = Interlocked.Increment(ref _currentId);
            _users[user.Id] = user;
        }
    }

    public Task<PagedUsersResponse> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        string? department = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Clamp pagination parameters to prevent unbounded scans or invalid offsets
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IEnumerable<User> query = _users.Values;

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(u => u.Department.Equals(department.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Materialize filtered list once for accurate count and single-pass projection
        var filtered = query.OrderBy(u => u.Id).ToList();
        var totalCount = filtered.Count;

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToResponse)
            .ToList();

        return Task.FromResult(new PagedUsersResponse(page, pageSize, totalCount, items));
    }

    public Task<UserResponse?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (id <= 0)
        {
            return Task.FromResult<UserResponse?>(null);
        }

        _users.TryGetValue(id, out var user);
        return Task.FromResult(user is null ? null : MapToResponse(user));
    }

    public Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (_users.Values.Any(u => u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateEmailException($"A user with email '{normalizedEmail}' already exists.");
        }

        var newId = Interlocked.Increment(ref _currentId);
        var user = new User
        {
            Id = newId,
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Department = request.Department.Trim(),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _users[newId] = user;
        return Task.FromResult(MapToResponse(user));
    }

    public Task<UserResponse?> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (id <= 0 || !_users.TryGetValue(id, out var existing))
        {
            return Task.FromResult<UserResponse?>(null);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailConflict = _users.Values.Any(u =>
            u.Id != id && u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

        if (emailConflict)
        {
            throw new DuplicateEmailException($"A user with email '{normalizedEmail}' already exists.");
        }

        existing.FullName = request.FullName.Trim();
        existing.Email = normalizedEmail;
        existing.Department = request.Department.Trim();
        existing.Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim();
        existing.IsActive = request.IsActive;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        return Task.FromResult<UserResponse?>(MapToResponse(existing));
    }

    public Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (id <= 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_users.TryRemove(id, out _));
    }

    private static UserResponse MapToResponse(User user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.Department,
            user.Role,
            user.IsActive,
            user.CreatedAtUtc,
            user.UpdatedAtUtc
        );
}

public sealed class DuplicateEmailException(string message) : Exception(message);