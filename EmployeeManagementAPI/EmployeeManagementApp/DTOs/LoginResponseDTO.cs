namespace EmployeeManagementApp.DTOs
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }
}
