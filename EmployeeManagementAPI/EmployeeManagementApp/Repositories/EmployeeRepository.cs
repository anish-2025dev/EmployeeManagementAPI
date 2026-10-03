using EmployeeManagementApp.Data;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Interfaces;
using EmployeeManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementApp.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(
            AppDbContext context,
            ILogger<EmployeeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(List<Employee> Employees, int TotalRecords)>
            GetAllEmployeesAsync(EmployeeQueryParametersDTO parameters)
        {
            _logger.LogInformation(
                "Fetching employees page {Page} with page size {PageSize}",
                parameters.Page,
                parameters.PageSize);

            var employeeQuery = _context.Employees.AsNoTracking();

            // Filtering

            if (!string.IsNullOrWhiteSpace(parameters.Department))
            {
                employeeQuery = employeeQuery.Where(
                    e => e.Department == parameters.Department);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Designation))
            {
                employeeQuery = employeeQuery.Where(
                    e => e.Designation == parameters.Designation);
            }

            if (parameters.IsActive.HasValue)
            {
                employeeQuery = employeeQuery.Where(
                    e => e.IsActive == parameters.IsActive.Value);
            }

            // Search

            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                var search = parameters.Search.Trim();

                employeeQuery = employeeQuery.Where(
                    e =>
                        e.Name.Contains(search) ||
                        e.Email.Contains(search) ||
                        e.Department.Contains(search) ||
                        e.Designation.Contains(search));
            }

            // Count after filters/search, before pagination

            var totalRecords = await employeeQuery.CountAsync();

            // Sorting

            if (!string.IsNullOrWhiteSpace(parameters.SortBy))
            {
                switch (parameters.SortBy.ToLower())
                {
                    case "name":
                        employeeQuery = parameters.Descending
                            ? employeeQuery.OrderByDescending(e => e.Name)
                            : employeeQuery.OrderBy(e => e.Name);
                        break;

                    case "salary":
                        employeeQuery = parameters.Descending
                            ? employeeQuery.OrderByDescending(e => e.Salary)
                            : employeeQuery.OrderBy(e => e.Salary);
                        break;

                    case "joiningdate":
                        employeeQuery = parameters.Descending
                            ? employeeQuery.OrderByDescending(e => e.JoiningDate)
                            : employeeQuery.OrderBy(e => e.JoiningDate);
                        break;

                    default:
                        employeeQuery = employeeQuery.OrderBy(e => e.Id);
                        break;
                }
            }
            else
            {
                employeeQuery = employeeQuery.OrderBy(e => e.Id);
            }

            // Pagination

            var employees = await employeeQuery
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            return (employees, totalRecords);
        }

        public Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation(
                "Fetching employee with ID {Id} from the database.",
                id);

            return _context.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddEmployeeAsync(Employee employee)
        {
            _logger.LogInformation(
                "Adding a new employee to the database.");

            _context.Employees.Add(employee);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            _logger.LogInformation(
                "Updating employee with ID {Id} in the database.",
                employee.Id);

            _context.Employees.Update(employee);

            await _context.SaveChangesAsync();
        }


        public async Task DeleteEmployeeAsync(Employee employee)
        {
            _logger.LogInformation(
                "Deleting employee with ID {Id} from the database.",
                employee.Id);

            _context.Employees.Remove(employee);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            _logger.LogInformation(
                "Checking if employee email {Email} already exists.",
                email);

            return await _context.Employees
                .AnyAsync(e => e.Email == email);
        }


    }
}


// a professional query pipeline follows
//IQueryable
//↓
//Where(Department)
//↓
//Where(Search)
//↓
//CountAsync()
//↓
//OrderBy()
//↓
//Skip()
//↓
//Take()
//↓
//ToListAsync()