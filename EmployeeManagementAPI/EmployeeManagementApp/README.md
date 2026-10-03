# Employee Management API 🚀

A production-inspired ASP.NET Core Web API built as a hands-on backend engineering project to learn modern API development, authentication, authorization, architecture patterns, performance optimization, security hardening, testing, and deployment.

---

# Project Goals

This project was built to learn backend engineering from fundamentals instead of only creating a CRUD application.

Major learning objectives:

- ASP.NET Core Web API
- C#
- SQL Server
- Entity Framework Core
- Dependency Injection
- Layered Architecture
- Repository Pattern
- DTO Pattern
- AutoMapper
- Validation
- Logging
- Exception Handling
- JWT Authentication
- Role-Based Authorization
- Pagination
- Filtering
- Sorting
- Searching
- API Performance
- Security Hardening
- Docker
- Redis
- Kubernetes
- Cloud Deployment

---

# High Level Architecture

```text
Client
  ↓
Controller
  ↓
Service
  ↓
Repository
  ↓
AppDbContext
  ↓
SQL Server
```

## Layer Responsibilities

### Controllers

Responsible for:

- Receiving HTTP requests
- Returning HTTP responses
- Calling appropriate services

Examples:

```text
EmployeeController
AuthController
```

---

### Services

Responsible for:

- Business logic
- Application workflows
- Orchestrating repositories

Examples:

```text
EmployeeService
AuthService
```

---

### Repositories

Responsible for:

- Database queries
- Filtering
- Sorting
- Searching
- Pagination
- Data persistence

Examples:

```text
EmployeeRepository
AuthRepository
```

---

### AppDbContext

Responsible for:

- Entity Framework communication
- Database mapping
- SQL Server interaction

---

### DTOs

Responsible for:

- API request contracts
- API response contracts
- Validation boundaries

---

# Technologies Used

Backend:

```text
ASP.NET Core
C#
.NET
```

Database:

```text
SQL Server
Entity Framework Core
```

Authentication:

```text
JWT Authentication
Password Hashing
Role-Based Authorization
```

Architecture:

```text
Dependency Injection
Repository Pattern
Service Layer
DTO Pattern
AutoMapper
```

Infrastructure:

```text
Logging
Exception Middleware
Async/Await
```

---

# Features Implemented

## Employee Management

### Create Employee

```http
POST /api/employee
```

### Get Employee By Id

```http
GET /api/employee/{id}
```

### Get All Employees

```http
GET /api/employee
```

### Update Employee

```http
PUT /api/employee
```

### Delete Employee

```http
DELETE /api/employee/{id}
```

Admin only.

---

## Authentication

### Register User

```http
POST /api/auth/register
```

Features:

- Username validation
- Password hashing
- Duplicate username prevention

---

### Login User

```http
POST /api/auth/login
```

Features:

- Password verification
- JWT generation
- Role claim generation

---

## Authorization

### Protected Endpoints

```csharp
[Authorize]
```

### Role Based Access

```csharp
[Authorize(Roles = "Admin")]
```

Example:

```text
Admin -> Delete Employee ✅

User -> Delete Employee ❌
```

---

# Employee Model

```csharp
public class Employee
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string Department { get; set; }

    public string Designation { get; set; }

    public decimal Salary { get; set; }

    public DateTime JoiningDate { get; set; }

    public bool IsActive { get; set; }
}
```

---

# DTOs

## Employee DTOs

```text
CreateEmployeeDTO
UpdateEmployeeDTO
ResponseEmployeeDTO
PagedResult<T>
EmployeeQueryParametersDTO
```

---

## Authentication DTOs

```text
RegisterUserDTO
LoginUserDTO
ResponseUserDTO
LoginResponseDTO
```

---

# AutoMapper

Implemented mappings:

```csharp
CreateMap<CreateEmployeeDTO, Employee>();

CreateMap<UpdateEmployeeDTO, Employee>();

CreateMap<Employee, ResponseEmployeeDTO>();
```

Benefits:

- Cleaner code
- Reduced manual mapping
- Easier maintenance

---

# Validation

Implemented validation attributes:

```csharp
[Required]

[StringLength]

[Range]

[EmailAddress]
```

Benefits:

- Reject invalid input
- Prevent bad data
- Consistent validation

---

# Logging

Implemented:

```csharp
ILogger<T>
```

Across:

```text
Controllers
Services
Repositories
Authentication Flow
Employee Flow
```

Example:

```csharp
_logger.LogInformation(
    "Fetching employee with ID {Id}",
    id);
```

---

# Exception Middleware

Implemented centralized exception handling.

Flow:

```text
Exception
    ↓
Middleware
    ↓
Logged
    ↓
Controlled Response
```

Benefits:

- Cleaner controllers
- Consistent error handling
- Centralized diagnostics

---

# Async Programming

Implemented:

```csharp
async
await
```

Examples:

```csharp
ToListAsync()

CountAsync()

SaveChangesAsync()

FirstOrDefaultAsync()
```

Benefits:

- Better scalability
- Non-blocking I/O
- Modern .NET practices

---

# Authentication Flow

## Registration

```text
Username
Password
Role
    ↓
Hash Password
    ↓
Store User
```

---

## Login

```text
Username
Password
    ↓
Validate Credentials
    ↓
Generate JWT
    ↓
Return Token
```

---

# Password Security

Implemented:

```csharp
IPasswordHasher<User>
```

Passwords are:

```text
Never stored as plain text
```

Instead:

```text
Password
    ↓
Hash
    ↓
Database
```

---

# JWT Authentication

JWT Contains:

```text
UserId
UserName
Role
```

JWT Features:

```text
Claims
Expiration
Signing
Validation
Authentication Middleware
```

---

# Query Features

---

## Pagination

Supported:

```http
GET /api/employee?page=1&pageSize=10
```

Returns:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalRecords": 57,
  "totalPages": 6
}
```

Concepts Learned:

```text
CountAsync
Skip
Take
PagedResult<T>
```

---

## Filtering

Supported:

```http
?department=IT
```

```http
?designation=Software Engineer
```

```http
?isActive=true
```

Examples:

```http
GET /api/employee?department=IT
```

---

## Sorting

Supported:

```http
?sortBy=name
```

```http
?sortBy=salary
```

```http
?sortBy=joiningdate
```

Descending:

```http
?descending=true
```

Examples:

```http
GET /api/employee?sortBy=salary&descending=true
```

---

## Searching

Supported:

```http
?search=anish
```

Searches:

```text
Name
Email
Department
Designation
```

Examples:

```http
GET /api/employee?search=software
```

---

## Combined Query

Example:

```http
GET /api/employee?
department=IT
&designation=Software Engineer
&isActive=true
&search=anish
&sortBy=salary
&descending=true
&page=1
&pageSize=5
```

---

# Project Structure

```text
EmployeeManagementApp

├── Controllers
│   ├── EmployeeController
│   └── AuthController
│
├── Services
│   ├── EmployeeService
│   └── AuthService
│
├── Interfaces
│
├── Repositories
│   ├── EmployeeRepository
│   └── AuthRepository
│
├── DTOs
│
├── Data
│   └── AppDbContext
│
├── Models
│   ├── Employee
│   └── User
│
├── Middleware
│   └── ExceptionMiddleware
│
├── Mappings
│   └── EmployeeMappingProfile
│
└── Security
    └── JwtService
```

---

# Local Setup

## Prerequisites

Install:

```text
.NET SDK
SQL Server
Visual Studio
```

---

## Restore Packages

```bash
dotnet restore
```

---

## Build

```bash
dotnet build
```

---

# Database Setup

## Connection String

Update:

```json
appsettings.json
```

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection":
      "Server=(localdb)\\MSSQLLocalDB;Database=EmployeeDB;Trusted_Connection=True;"
  }
}
```

---

## Apply Migrations

Create migrations:

```bash
dotnet ef migrations add InitialCreate
```

Update database:

```bash
dotnet ef database update
```

---

# JWT Configuration

Example:

```json
"Jwt": {
  "Key": "SuperSecretKey123456",
  "Issuer": "EmployeeAPI",
  "Audience": "EmployeeAPIUsers"
}
```

Purpose:

```text
Key        -> Signs token
Issuer     -> Who created token
Audience   -> Who can use token
```

---

# Running the Application

Run using:

```bash
dotnet run
```

OR

```bash
F5
```

inside Visual Studio.

Swagger/OpenAPI endpoint:

```text
/swagger
```

(if enabled)

---

# Recommended Testing Flow

## Register

```http
POST /api/auth/register
```

---

## Login

```http
POST /api/auth/login
```

Copy JWT token.

---

## Authorize Requests

Add:

```http
Authorization: Bearer <JWT>
```

---

## Test Employee APIs

```http
POST /api/employee
```

```http
GET /api/employee
```

```http
PUT /api/employee
```

```http
DELETE /api/employee/{id}
```

---

# Common Issues

## JWT Returning