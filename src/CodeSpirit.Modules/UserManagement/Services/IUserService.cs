using CodeSpirit.Modules.UserManagement.Models;

namespace CodeSpirit.Modules.UserManagement.Services;

public interface IUserService
{
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task<User?> GetUserByIdAsync(Guid id);
    Task<List<User>> GetAllUsersAsync();
}
