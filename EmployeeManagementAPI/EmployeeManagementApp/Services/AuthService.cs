using AutoMapper;
using EmployeeManagementApp.Data;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Interfaces;
using EmployeeManagementApp.Models;
using EmployeeManagementApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IJwtService _jwtService;

        private readonly AppDbContext _context;
        private const int MaxFailedAttempts = 5;

        public AuthService(
    IAuthRepository authRepository,
    IMapper mapper,
    IPasswordHasher<User> passwordHasher,
    IJwtService jwtService,
    AppDbContext context)
        {
            _authRepository = authRepository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _context = context;
        }

        public async Task<ResponseUserDTO> RegisterUserAsync(
            RegisterUserDTO registerUserDTO)
        {
            var existingUser =
                await _authRepository.GetByUsernameAsync(
                    registerUserDTO.UserName);

            if (existingUser != null)
            {
                throw new InvalidOperationException(
                    "Username already exists.");
            }

            var user = _mapper.Map<User>(registerUserDTO);

            user.Role = "Employee";
            user.PasswordHash = _passwordHasher.HashPassword(user, registerUserDTO.Password);

            await _authRepository.AddUserAsync(user);

            return _mapper.Map<ResponseUserDTO>(user);
        }

        public async Task<LoginResponseDTO> LoginUserAsync(LoginUserDTO loginUserDTO)
        {
            var user = await _authRepository.GetByUsernameAsync(
                loginUserDTO.UserName);

            if (user == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid username or password.");
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "Account is locked. Try again later.");
            }

            var result =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    loginUserDTO.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;

                await LogAuditAsync(
                    user.UserName,
                    "LOGIN_FAILED",
                    "Invalid credentials");

                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd =
                        DateTime.UtcNow.AddMinutes(15);

                    await LogAuditAsync(
                        user.UserName,
                        "ACCOUNT_LOCKED",
                        "Too many failed login attempts");
                }

                await _authRepository.UpdateUserAsync(user);

                throw new UnauthorizedAccessException(
                    "Invalid username or password.");
            }

            var accessToken = _jwtService.GenerateToken(user);

            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;

            user.RefreshTokenExpiryTime =
                DateTime.UtcNow.AddDays(7);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await _authRepository.UpdateUserAsync(user);
            await LogAuditAsync(user.UserName,
                "LOGIN_SUCCESS",
                "User logged in successfully");

            return new LoginResponseDTO
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                UserName = user.UserName,
                Role = user.Role
            };
        }


        public async Task<LoginResponseDTO> RefreshTokenAsync(
            RefreshTokenRequestDTO refreshTokenRequestDTO)
        {
            if (string.IsNullOrWhiteSpace(
                refreshTokenRequestDTO.RefreshToken))
            {
                throw new UnauthorizedAccessException(
                    "Refresh token is required.");
            }

            var user =
                await _authRepository.GetByRefreshTokenAsync(
                    refreshTokenRequestDTO.RefreshToken);

            if (user == null)
            {
                throw new UnauthorizedAccessException(
                    "Invalid refresh token.");
            }

            if (!user.RefreshTokenExpiryTime.HasValue ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;

                await _authRepository.UpdateUserAsync(user);

                throw new UnauthorizedAccessException(
                    "Refresh token has expired. Please log in again.");
            }

            var newAccessToken =
                _jwtService.GenerateToken(user);

            var newRefreshToken =
                _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime =
                DateTime.UtcNow.AddDays(7);

            await _authRepository.UpdateUserAsync(user);

            await LogAuditAsync(user.UserName,
                "REFRESH_TOKEN_USED",
                "Access token refreshed");

            return new LoginResponseDTO
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                UserName = user.UserName,
                Role = user.Role
            };
        }

        private async Task LogAuditAsync(
            string userName,
            string action,
            string details)
        {
            await _context.AuditLogs.AddAsync(
                new AuditLog
                {
                    UserName = userName,
                    Action = action,
                    Details = details,
                    TimeStampUtc = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();
        }

    }
}