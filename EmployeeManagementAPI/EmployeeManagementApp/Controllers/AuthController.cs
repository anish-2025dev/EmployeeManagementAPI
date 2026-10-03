using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ResponseUserDTO>> Register(
            RegisterUserDTO registerUserDTO)
        {
            _logger.LogInformation(
                "Registering user {UserName}",
                registerUserDTO.UserName);

            var response =
                await _authService.RegisterUserAsync(
                    registerUserDTO);

            return CreatedAtAction(
                nameof(Register),
                new { id = response.Id },
                response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<ResponseUserDTO>> Login(LoginUserDTO loginUserDTO)
        {
            _logger.LogInformation(
                "Login attempt for user {UserName}",
                loginUserDTO.UserName);

            var response =
                await _authService.LoginUserAsync(
                    loginUserDTO);

            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponseDTO>> RefreshToken(
            RefreshTokenRequestDTO refreshTokenRequestDTO)
        {
            var response =
                await _authService.RefreshTokenAsync(
                    refreshTokenRequestDTO);

            return Ok(response);
        }
    }
}