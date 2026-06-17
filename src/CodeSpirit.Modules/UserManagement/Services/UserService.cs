using CodeSpirit.Core.Attributes;
using CodeSpirit.Modules.UserManagement.Models;

namespace CodeSpirit.Modules.UserManagement.Services;

[Service(Lifetime = ServiceLifetime.Singleton)]
public class UserService : IUserService
{
    private readonly List<User> _users = new();
    private readonly object _lock = new();

    public Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };
        lock (_lock)
        {
            _users.Add(user);
        }
        return Task.FromResult(user);
    }

    public Task<User?> GetUserByIdAsync(Guid id)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_users.ToList());
        }
    }
}
