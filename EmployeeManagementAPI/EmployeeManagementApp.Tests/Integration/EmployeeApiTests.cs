using System.Net;
using Xunit;
using System.Net.Http.Json;
namespace EmployeeManagementApp.Tests.Integration
{
	public class EmployeeApiTests
		: IClassFixture<CustomWebApplicationFactory>
	{
		private readonly HttpClient _client;

		public EmployeeApiTests(
			CustomWebApplicationFactory factory)
		{
			_client = factory.CreateClient();
		}

		[Fact]
		public async Task GetEmployeeById_WhenEmployeeDoesNotExist_ReturnsNotFound()
		{
			// Arrange
			const int missingEmployeeId = 999999;

			// Act
			var response =
				await _client.GetAsync(
					$"/api/Employee/{missingEmployeeId}");

			// Assert
			Assert.Equal(
				HttpStatusCode.NotFound,
				response.StatusCode);
		}

		[Fact]
		public async Task AddEmployee_WhenEmployeeIsValid_ReturnsCreated()
		{
			// Arrange
			var employeeDto = new CreateEmployeeDTO
			{
				Name = "Anish Gupta",
				Email = "anish.integration@test.com",
				Department = "Engineering",
				Designation = "Software Trainee",
				Salary = 50000,
				JoiningDate = DateTime.UtcNow,
				IsActive = true
			};

			// Act
			var response =
				await _client.PostAsJsonAsync(
					"/api/Employee",
					employeeDto);

			// Assert
			Assert.Equal(
				HttpStatusCode.Created,
				response.StatusCode);
		}

		[Fact]
		public async Task AddEmployee_WhenEmailAlreadyExists_ReturnsErrorResponse()
		{
			// Arrange

			var employeeDto = new CreateEmployeeDTO
			{
				Name = "Anish Gupta",
				Email = "duplicate@test.com",
				Department = "Engineering",
				Designation = "Software Trainee",
				Salary = 50000,
				JoiningDate = DateTime.UtcNow,
				IsActive = true
			};

			// First insert

			var firstResponse =
				await _client.PostAsJsonAsync(
					"/api/Employee",
					employeeDto);

			Assert.Equal(
				HttpStatusCode.Created,
				firstResponse.StatusCode);

			// Second insert with same email

			var secondResponse =
				await _client.PostAsJsonAsync(
					"/api/Employee",
					employeeDto);

			// Assert

			Assert.False(secondResponse.IsSuccessStatusCode);
		}

		[Fact]
		public async Task GetAllEmployees_WithoutToken_ReturnsUnauthorized()
		{
			// Act
			var response =
				await _client.GetAsync(
					"/api/Employee");

			// Assert
			Assert.Equal(
				HttpStatusCode.Unauthorized,
				response.StatusCode);
		}
	}
}