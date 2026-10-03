using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Interfaces
{
    public interface IEmployeeService
    {
        Task<PagedResult<ResponseEmployeeDTO>>GetAllEmployeesAsync(EmployeeQueryParametersDTO pagination);
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task AddEmployeeAsync(Employee employee);
        Task UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int id);
    }
}