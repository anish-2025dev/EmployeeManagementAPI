using EmployeeManagementApp.Data;
using EmployeeManagementApp.Interfaces;
using EmployeeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementApp.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetByUsernameAsync(
            string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(
                    user => user.UserName == username);
        }

        public async Task<User?> GetByRefreshTokenAsync(
            string refreshToken)
        {
            return await _context.Users
                .FirstOrDefaultAsync(
                    user => user.RefreshToken == refreshToken);
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}