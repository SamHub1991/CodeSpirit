using System.ComponentModel.DataAnnotations;

namespace CodeSpirit.Modules.UserManagement.Models;

public record CreateUserRequest
{
    [Required]
    [MinLength(2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
