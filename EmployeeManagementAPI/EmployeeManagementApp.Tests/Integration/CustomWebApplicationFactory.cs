using EmployeeManagementApp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmployeeManagementApp.Tests.Integration
{
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName =
            $"EmployeeManagementIntegrationTests_{Guid.NewGuid()}";

        public CustomWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable(
                "JWT_SECRET_KEY",
                "IntegrationTestingSecretKeyForEmployeeManagementApi123456");
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();

                services.RemoveAll<
                    DbContextOptions<AppDbContext>>();

                services.RemoveAll<
                    IDbContextOptionsConfiguration<AppDbContext>>();

                services.AddDbContext<AppDbContext>(
                    options =>
                    {
                        options.UseInMemoryDatabase(
                            _databaseName);
                    });
            });
        }

        protected override void Dispose(
            bool disposing)
        {
            base.Dispose(disposing);

            Environment.SetEnvironmentVariable(
                "JWT_SECRET_KEY",
                null);
        }
    }
}