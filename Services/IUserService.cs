using UserManagementAPI.DTOs;

namespace UserManagementAPI.Services;

public interface IUserService
{
    Task<PagedUsersResponse> GetUsersAsync(int page = 1, int pageSize = 20, string? department = null, string? search = null, CancellationToken cancellationToken = default);
    Task<UserResponse?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse?> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}