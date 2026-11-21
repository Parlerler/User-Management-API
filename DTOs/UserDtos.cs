using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs;

public record CreateUserRequest(
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    string FullName,

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [StringLength(200)]
    string Email,

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(80, MinimumLength = 2)]
    string Department,

    [StringLength(50)]
    string Role = "User"
);

public record UpdateUserRequest(
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    string FullName,

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [StringLength(200)]
    string Email,

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(80, MinimumLength = 2)]
    string Department,

    [StringLength(50)]
    string Role = "User",

    bool IsActive = true
);

public record UserResponse(
    int Id,
    string FullName,
    string Email,
    string Department,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record PagedUsersResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<UserResponse> Items
);