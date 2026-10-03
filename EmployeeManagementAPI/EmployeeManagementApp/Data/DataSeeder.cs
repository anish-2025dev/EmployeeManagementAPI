using EmployeeManagementApp.Models;

namespace EmployeeManagementApp.Data
{
    public static class DataSeeder
    {
        public static async Task SeedEmployeesAsync(AppDbContext context)
        {
            if (context.Employees.Any())
            {
                return;
            }

            var random = new Random();

            string[] departments =
            {
                "IT",
                "HR",
                "Finance",
                "Sales",
                "Marketing",
                "Operations"
            };

            string[] designations =
            {
                "Intern",
                "Developer",
                "Senior Developer",
                "Lead",
                "Manager",
                "Architect"
            };

            var employees = new List<Employee>();

            for (int i = 1; i <= 1000; i++)
            {
                employees.Add(new Employee
                {
                    Name = $"Employee {i}",

                    Email = $"employee{i}@company.com",

                    Department =
                        departments[random.Next(departments.Length)],

                    Designation =
                        designations[random.Next(designations.Length)],

                    Salary =
                        random.Next(25000, 200001),

                    JoiningDate =
                        DateTime.Today.AddDays(
                            -random.Next(0, 365 * 5)),

                    IsActive =
                        random.Next(100) < 80
                });
            }

            context.Employees.AddRange(employees);

            await context.SaveChangesAsync();
        }
    }
}