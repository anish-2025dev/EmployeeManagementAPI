using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);

        string GenerateRefreshToken();
    }
}
