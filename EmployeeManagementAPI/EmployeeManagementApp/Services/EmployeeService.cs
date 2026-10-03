using AutoMapper;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Interfaces;
using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeerepository;
        private readonly ILogger<EmployeeService> _logger;
        private readonly IMapper _mapper;

        public EmployeeService(
            IEmployeeRepository employeeRepository,
            ILogger<EmployeeService> logger,
            IMapper mapper)
        {
            _employeerepository = employeeRepository;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<PagedResult<ResponseEmployeeDTO>> GetAllEmployeesAsync(
            EmployeeQueryParametersDTO pagination)
        {
            _logger.LogInformation(
                "Fetching employees page {Page} with page size {PageSize}",
                pagination.Page,
                pagination.PageSize);

            var result =
                await _employeerepository
                    .GetAllEmployeesAsync(pagination);

            var employeeDtos =
                _mapper.Map<List<ResponseEmployeeDTO>>(
                    result.Employees);

            return new PagedResult<ResponseEmployeeDTO>
            {
                Items = employeeDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalRecords = result.TotalRecords,
                TotalPages = (int)Math.Ceiling(
                    result.TotalRecords /
                    (double)pagination.PageSize)
            };
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation(
                "Fetching employee with ID: {Id} from the repository asynchronously.",
                id);

            return await _employeerepository.GetEmployeeByIdAsync(id);
        }

        public async Task AddEmployeeAsync(Employee employee)
        {
            _logger.LogInformation(
                "Adding a new employee to the repository.");

            var emailExists =
                await _employeerepository
                    .EmailExistsAsync(employee.Email);

            if (emailExists)
            {
                throw new InvalidOperationException(
                    $"Employee with email '{employee.Email}' already exists.");
            }

            await _employeerepository
                .AddEmployeeAsync(employee);
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            _logger.LogInformation(
                "Updating employee with ID: {Id} in the repository.",
                employee.Id);

            var existingEmployee =
                await _employeerepository.GetEmployeeByIdAsync(employee.Id);

            if (existingEmployee == null)
            {
                return;
            }

            existingEmployee.Name = employee.Name;
            existingEmployee.Email = employee.Email;
            existingEmployee.Department = employee.Department;
            existingEmployee.Designation = employee.Designation;
            existingEmployee.Salary = employee.Salary;
            existingEmployee.JoiningDate = employee.JoiningDate;
            existingEmployee.IsActive = employee.IsActive;

            await _employeerepository.UpdateEmployeeAsync(existingEmployee);
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            _logger.LogInformation(
                "Deleting employee with ID: {Id} from the repository.",
                id);

            var employee =
                await _employeerepository.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return;
            }

            await _employeerepository.DeleteEmployeeAsync(employee);
        }
    }
}