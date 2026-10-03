using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<(List<Employee> Employees, int TotalRecords)> GetAllEmployeesAsync(EmployeeQueryParametersDTO pagination);
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(Employee employee);

        Task<bool> EmailExistsAsync(string email);
    }
}
