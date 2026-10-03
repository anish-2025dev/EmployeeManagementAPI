using AutoMapper;
using System.Linq;
using EmployeeManagementApp.DTOs;
using EmployeeManagementApp.Interfaces;
using EmployeeManagementApp.Models;
using EmployeeManagementApp.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeManagementApp.Tests.Services
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
        private readonly Mock<ILogger<EmployeeService>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;

        private readonly EmployeeService _employeeService;

        public EmployeeServiceTests()
        {
            _employeeRepositoryMock =
                new Mock<IEmployeeRepository>();

            _loggerMock =
                new Mock<ILogger<EmployeeService>>();

            _mapperMock =
                new Mock<IMapper>();

            _employeeService = new EmployeeService(
                _employeeRepositoryMock.Object,
                _loggerMock.Object,
                _mapperMock.Object);
        }

        #region Get All Employees Tests

        [Fact]
        public async Task GetAllEmployeesAsync_WhenEmployeesExist_ReturnsPagedResult()
        {
            // Arrange
            var queryParameters =
                new EmployeeQueryParametersDTO
                {
                    Page = 2,
                    PageSize = 2
                };

            var employees = new List<Employee>
            {
                new Employee
                {
                    Id = 3,
                    Name = "Employee Three",
                    Email = "employee3@example.com",
                    Department = "Engineering",
                    Designation = "Developer"
                },
                new Employee
                {
                    Id = 4,
                    Name = "Employee Four",
                    Email = "employee4@example.com",
                    Department = "Engineering",
                    Designation = "Developer"
                }
            };

            var employeeDtos =
                new List<ResponseEmployeeDTO>
                {
                    new ResponseEmployeeDTO
                    {
                        Id = 3,
                        Name = "Employee Three",
                        Email = "employee3@example.com"
                    },
                    new ResponseEmployeeDTO
                    {
                        Id = 4,
                        Name = "Employee Four",
                        Email = "employee4@example.com"
                    }
                };

            _employeeRepositoryMock
                .Setup(repository =>
                    repository.GetAllEmployeesAsync(
                        queryParameters))
                .ReturnsAsync(
                    (Employees: employees,
                     TotalRecords: 5));

            _mapperMock
                .Setup(mapper =>
                    mapper.Map<List<ResponseEmployeeDTO>>(
                        employees))
                .Returns(employeeDtos);

            // Act
            var result =
                await _employeeService
                    .GetAllEmployeesAsync(queryParameters);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(
                2,
                result.Items.Count());

            Assert.Equal(
                2,
                result.Page);

            Assert.Equal(
                2,
                result.PageSize);

            Assert.Equal(
                5,
                result.TotalRecords);

            Assert.Equal(
                3,
                result.TotalPages);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.GetAllEmployeesAsync(
                        queryParameters),
                Times.Once);

            _mapperMock.Verify(
                mapper =>
                    mapper.Map<List<ResponseEmployeeDTO>>(
                        employees),
                Times.Once);
        }

        #endregion

        #region Get Employee By ID Tests

        [Fact]
        public async Task GetEmployeeByIdAsync_WhenEmployeeExists_ReturnsEmployee()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                Name = "Anish Gupta",
                Email = "anish@example.com",
                Department = "Engineering",
                Designation = "Software Trainee"
            };

            _employeeRepositoryMock
                .Setup(repository =>
                    repository.GetEmployeeByIdAsync(1))
                .ReturnsAsync(employee);

            // Act
            var result =
                await _employeeService
                    .GetEmployeeByIdAsync(1);

            // Assert
            Assert.NotNull(result);

            Assert.Equal(
                1,
                result.Id);

            Assert.Equal(
                "Anish Gupta",
                result.Name);

            Assert.Equal(
                "anish@example.com",
                result.Email);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.GetEmployeeByIdAsync(1),
                Times.Once);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_WhenEmployeeDoesNotExist_ReturnsNull()
        {
            // Arrange
            _employeeRepositoryMock
                .Setup(repository =>
                    repository.GetEmployeeByIdAsync(999))
                .ReturnsAsync((Employee?)null);

            // Act
            var result =
                await _employeeService
                    .GetEmployeeByIdAsync(999);

            // Assert
            Assert.Null(result);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.GetEmployeeByIdAsync(999),
                Times.Once);
        }

        #endregion

        #region Add Employee Tests

        [Fact]
        public async Task AddEmployeeAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var employee = new Employee
            {
                Name = "Anish Gupta",
                Email = "anish@example.com",
                Department = "Engineering",
                Designation = "Software Trainee"
            };

            _employeeRepositoryMock
                .Setup(repository =>
                    repository.EmailExistsAsync(
                        employee.Email))
                .ReturnsAsync(true);

            // Act
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _employeeService
                        .AddEmployeeAsync(employee));

            // Assert
            Assert.Equal(
                "Employee with email 'anish@example.com' already exists.",
                exception.Message);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.EmailExistsAsync(
                        employee.Email),
                Times.Once);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.AddEmployeeAsync(
                        It.IsAny<Employee>()),
                Times.Never);
        }

        [Fact]
        public async Task AddEmployeeAsync_WhenEmailDoesNotExist_AddsEmployee()
        {
            // Arrange
            var employee = new Employee
            {
                Name = "Anish Gupta",
                Email = "anish@example.com",
                Department = "Engineering",
                Designation = "Software Trainee"
            };

            _employeeRepositoryMock
                .Setup(repository =>
                    repository.EmailExistsAsync(
                        employee.Email))
                .ReturnsAsync(false);

            // Act
            await _employeeService
                .AddEmployeeAsync(employee);

            // Assert
            _employeeRepositoryMock.Verify(
                repository =>
                    repository.EmailExistsAsync(
                        employee.Email),
                Times.Once);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.AddEmployeeAsync(
                        employee),
                Times.Once);
        }

        #endregion

        #region Update Employee Tests

        [Fact]
        public async Task UpdateEmployeeAsync_WhenEmployeeDoesNotExist_DoesNotUpdateEmployee()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 999,
                Name = "Missing Employee",
                Email = "missing@example.com"
            };

            _employeeRepositoryMock
                .Setup(repository =>
                    repository.GetEmployeeByIdAsync(
                        employee.Id))
                .ReturnsAsync((Employee?)null);

            // Act
            await _employeeService
                .UpdateEmployeeAsync(employee);

            // Assert
            _employeeRepositoryMock.Verify(
                repository =>
                    repository.GetEmployeeByIdAsync(999),
                Times.Once);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.UpdateEmployeeAsync(
                        It.IsAny<Employee>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateEmployeeAsync_WhenEmployeeExists_UpdatesEmployeeProperties()
        {
            // Arrange
            var existingEmployee = new Employee
            {
                Id = 1,
                Name = "Old Name",
                Email = "old@example.com",
                Department = "Old Department",
                Designation = "Old Designation",
                Salary = 10000,
                JoiningDate = new DateTime(2025, 1, 1),
                IsActive = false
            };

            var updatedEmployee = new Employee
            {
                Id = 1,
                Name = "New Name",
                Email = "new@example.com",
                Department = "Engineering",
                Designation = "Software Developer",
                Salary = 50000,
                JoiningDate = new DateTime(2026, 1, 1),
                IsActive = true
            };

            _employeeRepositoryMock
                .Setup(repository =>
                    repository.GetEmployeeByIdAsync(
                        updatedEmployee.Id))
                .ReturnsAsync(existingEmployee);

            // Act
            await _employeeService
                .UpdateEmployeeAsync(updatedEmployee);

            // Assert
            Assert.Equal(
                "New Name",
                existingEmployee.Name);

            Assert.Equal(
                "new@example.com",
                existingEmployee.Email);

            Assert.Equal(
                "Engineering",
                existingEmployee.Department);

            Assert.Equal(
                "Software Developer",
                existingEmployee.Designation);

            Assert.Equal(
                50000,
                existingEmployee.Salary);

            Assert.Equal(
                new DateTime(2026, 1, 1),
                existingEmployee.JoiningDate);

            Assert.True(existingEmployee.IsActive);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.UpdateEmployeeAsync(
                        It.Is<Employee>(
                            employee =>
                                employee.Id == 1 &&
                                employee.Name == "New Name" &&
                                employee.Email ==
                                    "new@example.com" &&
                                employee.Department ==
                                    "Engineering" &&
                                employee.Designation ==
                                    "Software Developer" &&
                                employee.Salary == 50000 &&
                                employee.IsActive)),
                Times.Once);
        }
        #endregion

        #region Delete Employee Tests

        [Fact]
        public async Task DeleteEmployeeAsync_WhenEmployeeDoesNotExist_DoesNotDeleteEmployee()
        {
            // Arrange
            _employeeRepositoryMock
                .Setup(repository =>
                    repository.GetEmployeeByIdAsync(999))
                .ReturnsAsync((Employee?)null);

            // Act
            await _employeeService
                .DeleteEmployeeAsync(999);

            // Assert
            _employeeRepositoryMock.Verify(
                repository =>
                    repository.GetEmployeeByIdAsync(999),
                Times.Once);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.DeleteEmployeeAsync(
                        It.IsAny<Employee>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_WhenEmployeeExists_DeletesEmployee()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 1,
                Name = "Anish Gupta",
                Email = "anish@example.com",
                Department = "Engineering",
                Designation = "Software Trainee"
            };

            _employeeRepositoryMock
                .Setup(repository =>
                    repository.GetEmployeeByIdAsync(1))
                .ReturnsAsync(employee);

            // Act
            await _employeeService
                .DeleteEmployeeAsync(1);

            // Assert
            _employeeRepositoryMock.Verify(
                repository =>
                    repository.GetEmployeeByIdAsync(1),
                Times.Once);

            _employeeRepositoryMock.Verify(
                repository =>
                    repository.DeleteEmployeeAsync(employee),
                Times.Once);
        }

        #endregion

    }
}