using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Interfaces
{
    public interface IAuthRepository
    {
        Task AddUserAsync(User user);

        Task<User?> GetByUsernameAsync(string username);

        Task<User?> GetByRefreshTokenAsync(string refreshToken);

        Task UpdateUserAsync(User user);
    }
}