using EmployeeManagementApp.Data;
using EmployeeManagementApp.Interfaces;
using EmployeeManagementApp.Mappings;
using EmployeeManagementApp.Middleware;
using EmployeeManagementApp.Models;
using EmployeeManagementApp.Repositories;
using EmployeeManagementApp.Services;
using EmployeeManagementApp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


var jwtKey =
    Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"];

var permitLimit =
    builder.Configuration.GetValue<int>(
        "RateLimiting:PermitLimit",
        50);

var windowMinutes =
    builder.Configuration.GetValue<int>(
        "RateLimiting:WindowMinutes",
        1);

var segmentsPerWindow =
    builder.Configuration.GetValue<int>(
        "RateLimiting:SegmentsPerWindow",
        6);

var queueLimit =
    builder.Configuration.GetValue<int>(
        "RateLimiting:QueueLimit",
        0);

builder.Services.AddAutoMapper(typeof(EmployeeMappingProfile));

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();

        options.LogTo(
            Console.WriteLine,
            LogLevel.Information);
    }
});

builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer =
                builder.Configuration["Jwt:Issuer"],

            ValidAudience =
                builder.Configuration["Jwt:Audience"],

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey!))
        };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType =
            "application/json";

        await context.HttpContext.Response.WriteAsync(
            """
            {
                "message":"Too many requests. Please try again later."
            }
            """,
            token);
    };

    options.AddSlidingWindowLimiter(
        policyName: "ApiPolicy",
        opt =>
        {
            opt.PermitLimit =
                permitLimit;

            opt.Window =
                TimeSpan.FromMinutes(windowMinutes);

            opt.SegmentsPerWindow =
                segmentsPerWindow;

            opt.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;

            opt.QueueLimit =
                queueLimit;
        });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
             .GetRequiredService<AppDbContext>();

    await DataSeeder.SeedEmployeesAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

Console.WriteLine(
    $"Running Environment: {builder.Environment.EnvironmentName}");

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] =
        "nosniff";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'";

    await next();
});

app.UseMiddleware<ExceptionMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers()
    .RequireRateLimiting("ApiPolicy");

app.Run();

public partial class Program
{
}