using System.ComponentModel.DataAnnotations;

public class CreateEmployeeDTO
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Designation { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Salary { get; set; }

    [Required]
    public DateTime JoiningDate { get; set; }

    public bool IsActive { get; set; } = true;
}