using EmployeeManagementApp.DTOs;

namespace EmployeeManagementApp.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ResponseUserDTO> RegisterUserAsync(
            RegisterUserDTO registerUserDTO);

        Task<LoginResponseDTO> LoginUserAsync(
            LoginUserDTO loginUserDTO);

        Task<LoginResponseDTO> RefreshTokenAsync(
            RefreshTokenRequestDTO refreshTokenRequestDTO);

    }
}

