using EmployeeManagementApp.Interfaces;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace EmployeeManagementApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeService _employeeService;

        private readonly IMapper _mapper;

        public EmployeeController(
        IEmployeeService employeeService,
        ILogger<EmployeeController> logger,
        IMapper mapper)
        {
            _employeeService = employeeService;
            _logger = logger;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllEmployees([FromQuery] EmployeeQueryParametersDTO pagination)
        {
            _logger.LogInformation(
                "Fetching employees page {Page}",
                pagination.Page);

            var result =
                await _employeeService
                    .GetAllEmployeesAsync(pagination);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            _logger.LogInformation(
                "Fetching employee with ID: {Id}",
                id);

            var employee =
                await _employeeService.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                _logger.LogWarning(
                    "Employee with ID: {Id} not found",
                    id);

                return NotFound();
            }

            var responseEmployeeDto =
                _mapper.Map<ResponseEmployeeDTO>(employee);

            return Ok(responseEmployeeDto);
        }


        [HttpPost]
        public async Task<IActionResult> AddEmployee(
        CreateEmployeeDTO employeeDto)
        {
            _logger.LogInformation(
                "Adding a new employee");

            var employee =
                _mapper.Map<Employee>(employeeDto);

            await _employeeService.AddEmployeeAsync(employee);

            return Created();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.LogInformation(
                "Deleting employee with ID: {Id}",
                id);

            await _employeeService.DeleteEmployeeAsync(id);

            return NoContent();
        }


        [HttpPut]
        public async Task<IActionResult> UpdateEmployee(
        UpdateEmployeeDTO employeeDto)
        {
            _logger.LogInformation(
                "Updating employee with ID: {EmployeeId}",
                employeeDto.Id);

            var employee = _mapper.Map<Employee>(employeeDto);
            await _employeeService.UpdateEmployeeAsync(employee);
            return Ok();
        }

    }
}