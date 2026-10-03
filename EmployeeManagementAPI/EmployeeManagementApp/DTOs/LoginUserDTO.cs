using System.ComponentModel.DataAnnotations;

namespace EmployeeManagementApp.DTOs
{
    public class LoginUserDTO
    {
        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}