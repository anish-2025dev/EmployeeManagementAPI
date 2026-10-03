using System.ComponentModel.DataAnnotations;
public class EmployeeQueryParametersDTO
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Department { get; set; }

    public string? Designation { get; set; }

    public bool? IsActive { get; set; }

    public string? SortBy { get; set; }

    public bool Descending { get; set; } = false;

    public string? Search { get; set; }
}