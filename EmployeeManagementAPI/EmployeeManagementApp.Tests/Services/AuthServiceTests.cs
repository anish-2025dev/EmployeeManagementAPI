using AutoMapper;
using EmployeeManagementApp.Data;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Interfaces;
using EmployeeManagementApp.Models;
using EmployeeManagementApp.Services;
using EmployeeManagementApp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace EmployeeManagementApp.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly Mock<IAuthRepository> _authRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
        private readonly Mock<IJwtService> _jwtServiceMock;

        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _authRepositoryMock =
                new Mock<IAuthRepository>();

            _mapperMock =
                new Mock<IMapper>();

            _passwordHasherMock =
                new Mock<IPasswordHasher<User>>();

            _jwtServiceMock =
                new Mock<IJwtService>();

            var options =
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(
                        $"AuthServiceTests_{Guid.NewGuid()}")
                    .Options;

            _context = new AppDbContext(options);

            _authService = new AuthService(
                _authRepositoryMock.Object,
                _mapperMock.Object,
                _passwordHasherMock.Object,
                _jwtServiceMock.Object,
                _context);
        }

        [Fact]
        public async Task RegisterUserAsync_WhenUsernameAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var registerUserDto = new RegisterUserDTO
            {
                UserName = "anish",
                Password = "Password@123"
            };

            var existingUser = new User
            {
                Id = 1,
                UserName = "anish",
                PasswordHash = "existing-password-hash",
                Role = "Employee"
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(
                        registerUserDto.UserName))
                .ReturnsAsync(existingUser);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _authService.RegisterUserAsync(
                        registerUserDto));

            // Assert
            Assert.Equal(
                "Username already exists.",
                exception.Message);

            _authRepositoryMock.Verify(
                repository =>
                    repository.GetByUsernameAsync("anish"),
                Times.Once);

            _authRepositoryMock.Verify(
                repository =>
                    repository.AddUserAsync(
                        It.IsAny<User>()),
                Times.Never);
        }

        public void Dispose()
        {
            _context.Dispose();
        }


        [Fact]
        public async Task RegisterUserAsync_WhenInputIsValid_CreatesEmployeeUser()
        {
            // Arrange
            var registerUserDto = new RegisterUserDTO
            {
                UserName = "anish",
                Password = "Password@123"
            };

            var mappedUser = new User
            {
                UserName = "anish"
            };

            var responseDto = new ResponseUserDTO
            {
                UserName = "anish"
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(registerUserDto.UserName))
                .ReturnsAsync((User?)null);

            _mapperMock
                .Setup(mapper =>
                    mapper.Map<User>(registerUserDto))
                .Returns(mappedUser);

            _passwordHasherMock
                .Setup(hasher =>
                    hasher.HashPassword(
                        mappedUser,
                        registerUserDto.Password))
                .Returns("hashed-password");

            _mapperMock
                .Setup(mapper =>
                    mapper.Map<ResponseUserDTO>(mappedUser))
                .Returns(responseDto);

            // Act
            var result =
                await _authService.RegisterUserAsync(registerUserDto);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(
                "anish",
                result.UserName);

            Assert.Equal(
                "Employee",
                mappedUser.Role);

            Assert.Equal(
                "hashed-password",
                mappedUser.PasswordHash);

            _authRepositoryMock.Verify(
                repository =>
                    repository.AddUserAsync(
                        It.IsAny<User>()),
                Times.Once);
        }


        [Fact]
        public async Task LoginUserAsync_WhenUserDoesNotExist_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new LoginUserDTO
            {
                UserName = "anish",
                Password = "Password@123"
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(loginDto.UserName))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _authService.LoginUserAsync(loginDto));

            // Assert
            Assert.Equal(
                "Invalid username or password.",
                exception.Message);

            _authRepositoryMock.Verify(
                repository =>
                    repository.GetByUsernameAsync("anish"),
                Times.Once);
        }

        [Fact]
        public async Task LoginUserAsync_WhenAccountIsLocked_ThrowsInvalidOperationException()
        {
            // Arrange
            var loginDto = new LoginUserDTO
            {
                UserName = "anish",
                Password = "Password@123"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "hash",
                LockoutEnd = DateTime.UtcNow.AddMinutes(10)
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(loginDto.UserName))
                .ReturnsAsync(user);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _authService.LoginUserAsync(loginDto));

            // Assert
            Assert.Equal(
                "Account is locked. Try again later.",
                exception.Message);
        }

        [Fact]
        public async Task LoginUserAsync_WhenPasswordIsInvalid_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new LoginUserDTO
            {
                UserName = "anish",
                Password = "WrongPassword"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                FailedLoginAttempts = 0
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(loginDto.UserName))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(hasher =>
                    hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        loginDto.Password))
                .Returns(PasswordVerificationResult.Failed);

            // Act
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _authService.LoginUserAsync(loginDto));

            // Assert
            Assert.Equal(1, user.FailedLoginAttempts);

            _authRepositoryMock.Verify(
                repository =>
                    repository.UpdateUserAsync(user),
                Times.Once);
        }

        [Fact]
        public async Task LoginUserAsync_OnFifthFailedAttempt_LocksAccount()
        {
            // Arrange
            var loginDto = new LoginUserDTO
            {
                UserName = "anish",
                Password = "WrongPassword"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                Role = "Employee",
                FailedLoginAttempts = 4,
                LockoutEnd = null
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(loginDto.UserName))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(hasher =>
                    hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        loginDto.Password))
                .Returns(PasswordVerificationResult.Failed);

            var beforeLoginAttempt = DateTime.UtcNow;

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _authService.LoginUserAsync(loginDto));

            var afterLoginAttempt = DateTime.UtcNow;

            // Assert
            Assert.Equal(
                "Invalid username or password.",
                exception.Message);

            Assert.Equal(5, user.FailedLoginAttempts);

            Assert.True(user.LockoutEnd.HasValue);

            Assert.InRange(
                user.LockoutEnd!.Value,
                beforeLoginAttempt.AddMinutes(15),
                afterLoginAttempt.AddMinutes(15));

            _authRepositoryMock.Verify(
                repository =>
                    repository.UpdateUserAsync(user),
                Times.Once);

            var auditLogs =
                await _context.AuditLogs
                    .Where(log => log.UserName == "anish")
                    .ToListAsync();

            Assert.Equal(2, auditLogs.Count);

            Assert.Contains(
                auditLogs,
                log => log.Action == "LOGIN_FAILED");

            Assert.Contains(
                auditLogs,
                log => log.Action == "ACCOUNT_LOCKED");
        }

        [Fact]
        public async Task LoginUserAsync_WhenCredentialsAreValid_ReturnsTokens()
        {
            // Arrange
            var loginDto = new LoginUserDTO
            {
                UserName = "anish",
                Password = "Password@123"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                Role = "Employee",
                FailedLoginAttempts = 0
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(loginDto.UserName))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(hasher =>
                    hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        loginDto.Password))
                .Returns(PasswordVerificationResult.Success);

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateToken(user))
                .Returns("access-token");

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateRefreshToken())
                .Returns("refresh-token");

            // Act
            var result =
                await _authService.LoginUserAsync(loginDto);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(
                "access-token",
                result.Token);

            Assert.Equal(
                "refresh-token",
                result.RefreshToken);

            Assert.Equal(
                "anish",
                result.UserName);

            Assert.Equal(
                "Employee",
                result.Role);

            Assert.Equal(
                "refresh-token",
                user.RefreshToken);

            Assert.True(
                user.RefreshTokenExpiryTime.HasValue);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateToken(user),
                Times.Once);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateRefreshToken(),
                Times.Once);

            _authRepositoryMock.Verify(
                repository =>
                    repository.UpdateUserAsync(user),
                Times.Once);
        }

        [Fact]
        public async Task LoginUserAsync_WhenCredentialsAreValid_ResetsLockoutFields()
        {
            // Arrange
            var loginDto = new LoginUserDTO
            {
                UserName = "anish",
                Password = "Password@123"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                Role = "Employee",
                FailedLoginAttempts = 3,
                LockoutEnd = DateTime.UtcNow.AddMinutes(-1)
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(loginDto.UserName))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(hasher =>
                    hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        loginDto.Password))
                .Returns(PasswordVerificationResult.Success);

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateToken(user))
                .Returns("access-token");

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateRefreshToken())
                .Returns("refresh-token");

            // Act
            var result =
                await _authService.LoginUserAsync(loginDto);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(
                0,
                user.FailedLoginAttempts);

            Assert.Null(user.LockoutEnd);

            _authRepositoryMock.Verify(
                repository =>
                    repository.UpdateUserAsync(
                        It.Is<User>(updatedUser =>
                            updatedUser.FailedLoginAttempts == 0 &&
                            updatedUser.LockoutEnd == null)),
                Times.Once);
        }

        [Fact]
        public async Task LoginUserAsync_WhenCredentialsAreValid_CreatesLoginSuccessAuditLog()
        {
            // Arrange
            var loginDto = new LoginUserDTO
            {
                UserName = "anish",
                Password = "Password@123"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                Role = "Employee"
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByUsernameAsync(loginDto.UserName))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(hasher =>
                    hasher.VerifyHashedPassword(
                        user,
                        user.PasswordHash,
                        loginDto.Password))
                .Returns(PasswordVerificationResult.Success);

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateToken(user))
                .Returns("access-token");

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateRefreshToken())
                .Returns("refresh-token");

            // Act
            await _authService.LoginUserAsync(loginDto);

            // Assert
            var auditLog =
                await _context.AuditLogs.SingleAsync();

            Assert.Equal(
                "anish",
                auditLog.UserName);

            Assert.Equal(
                "LOGIN_SUCCESS",
                auditLog.Action);

            Assert.Equal(
                "User logged in successfully",
                auditLog.Details);
        }

        #region Refresh Token Tests

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshTokenIsEmpty_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var requestDto = new RefreshTokenRequestDTO
            {
                RefreshToken = string.Empty
            };

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _authService.RefreshTokenAsync(requestDto));

            // Assert
            Assert.Equal(
                "Refresh token is required.",
                exception.Message);

            _authRepositoryMock.Verify(
                repository =>
                    repository.GetByRefreshTokenAsync(
                        It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshTokenIsInvalid_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var requestDto = new RefreshTokenRequestDTO
            {
                RefreshToken = "invalid-refresh-token"
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByRefreshTokenAsync(
                        requestDto.RefreshToken))
                .ReturnsAsync((User?)null);

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _authService.RefreshTokenAsync(requestDto));

            // Assert
            Assert.Equal(
                "Invalid refresh token.",
                exception.Message);

            _authRepositoryMock.Verify(
                repository =>
                    repository.GetByRefreshTokenAsync(
                        "invalid-refresh-token"),
                Times.Once);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateToken(
                        It.IsAny<User>()),
                Times.Never);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateRefreshToken(),
                Times.Never);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshTokenIsExpired_ClearsTokenAndThrowsUnauthorizedAccessException()
        {
            // Arrange
            var requestDto = new RefreshTokenRequestDTO
            {
                RefreshToken = "expired-refresh-token"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                Role = "Employee",
                RefreshToken = "expired-refresh-token",
                RefreshTokenExpiryTime =
                    DateTime.UtcNow.AddMinutes(-5)
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByRefreshTokenAsync(
                        requestDto.RefreshToken))
                .ReturnsAsync(user);

            // Act
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => _authService.RefreshTokenAsync(requestDto));

            // Assert
            Assert.Equal(
                "Refresh token has expired. Please log in again.",
                exception.Message);

            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiryTime);

            _authRepositoryMock.Verify(
                repository =>
                    repository.UpdateUserAsync(
                        It.Is<User>(updatedUser =>
                            updatedUser.RefreshToken == null &&
                            updatedUser.RefreshTokenExpiryTime == null)),
                Times.Once);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateToken(
                        It.IsAny<User>()),
                Times.Never);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateRefreshToken(),
                Times.Never);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshTokenIsValid_ReturnsNewAccessAndRefreshTokens()
        {
            // Arrange
            var requestDto = new RefreshTokenRequestDTO
            {
                RefreshToken = "current-refresh-token"
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                Role = "Employee",
                RefreshToken = "current-refresh-token",
                RefreshTokenExpiryTime =
                    DateTime.UtcNow.AddDays(2)
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByRefreshTokenAsync(
                        requestDto.RefreshToken))
                .ReturnsAsync(user);

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateToken(user))
                .Returns("new-access-token");

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateRefreshToken())
                .Returns("new-refresh-token");

            // Act
            var result =
                await _authService.RefreshTokenAsync(requestDto);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(
                "new-access-token",
                result.Token);

            Assert.Equal(
                "new-refresh-token",
                result.RefreshToken);

            Assert.Equal(
                "anish",
                result.UserName);

            Assert.Equal(
                "Employee",
                result.Role);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateToken(user),
                Times.Once);

            _jwtServiceMock.Verify(
                service =>
                    service.GenerateRefreshToken(),
                Times.Once);

            _authRepositoryMock.Verify(
                repository =>
                    repository.UpdateUserAsync(user),
                Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_WhenRefreshTokenIsValid_RotatesStoredTokenAndCreatesAuditLog()
        {
            // Arrange
            const string oldRefreshToken =
                "old-refresh-token";

            const string newRefreshToken =
                "rotated-refresh-token";

            var requestDto = new RefreshTokenRequestDTO
            {
                RefreshToken = oldRefreshToken
            };

            var user = new User
            {
                UserName = "anish",
                PasswordHash = "stored-hash",
                Role = "Employee",
                RefreshToken = oldRefreshToken,
                RefreshTokenExpiryTime =
                    DateTime.UtcNow.AddDays(2)
            };

            _authRepositoryMock
                .Setup(repository =>
                    repository.GetByRefreshTokenAsync(
                        oldRefreshToken))
                .ReturnsAsync(user);

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateToken(user))
                .Returns("new-access-token");

            _jwtServiceMock
                .Setup(service =>
                    service.GenerateRefreshToken())
                .Returns(newRefreshToken);

            var beforeRefresh = DateTime.UtcNow;

            // Act
            await _authService.RefreshTokenAsync(requestDto);

            var afterRefresh = DateTime.UtcNow;

            // Assert
            Assert.NotEqual(
                oldRefreshToken,
                user.RefreshToken);

            Assert.Equal(
                newRefreshToken,
                user.RefreshToken);

            Assert.True(
                user.RefreshTokenExpiryTime.HasValue);

            Assert.InRange(
                user.RefreshTokenExpiryTime!.Value,
                beforeRefresh.AddDays(7),
                afterRefresh.AddDays(7));

            _authRepositoryMock.Verify(
                repository =>
                    repository.UpdateUserAsync(
                        It.Is<User>(updatedUser =>
                            updatedUser.RefreshToken ==
                            newRefreshToken)),
                Times.Once);

            var auditLog =
                await _context.AuditLogs.SingleAsync();

            Assert.Equal(
                "anish",
                auditLog.UserName);

            Assert.Equal(
                "REFRESH_TOKEN_USED",
                auditLog.Action);

            Assert.Equal(
                "Access token refreshed",
                auditLog.Details);
        }

        #endregion
    }
}