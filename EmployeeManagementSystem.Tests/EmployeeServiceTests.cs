using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using EmployeeManagementSystem.Services;
using EmployeeManagementSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmployeeManagementSystem.Tests
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _mockEmployeeRepo;
        private readonly Mock<IRoleRepository> _mockRoleRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<ILogger<EmployeeService>> _mockLogger;
        private readonly EmployeeService _employeeService;

        public EmployeeServiceTests()
        {
            _mockEmployeeRepo = new Mock<IEmployeeRepository>();
            _mockRoleRepo = new Mock<IRoleRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<EmployeeService>>();

            _employeeService = new EmployeeService(
                _mockEmployeeRepo.Object,
                _mockRoleRepo.Object,
                _mockUserRepo.Object,
                _mockLogger.Object);
        }

        // ========== GetAllEmployees ==========
        [Fact]
        public async Task GetAllEmployees_ReturnsMappedDtos()
        {
            // Arrange
            _mockEmployeeRepo.Setup(repo => repo.GetAllEmployees()).ReturnsAsync(new List<Employee>
            {
                new Employee { Id = 1, FirstName = "John", LastName = "Doe", Age = 30, Phone = "1234567890", Email = "john@example.com", Position = "Engineer", DateOfHire = new DateTime(2022, 1, 1), Role = new Role { Name = "Developer" } },
                new Employee { Id = 2, FirstName = "Jane", LastName = "Smith", Age = 28, Phone = "0987654321", Email = "jane@example.com", Position = "Manager", DateOfHire = new DateTime(2021, 5, 10), Role = new Role { Name = "Manager" } }
            });

            // Act
            var result = await _employeeService.GetAllEmployees();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.FullName == "John Doe" && e.RoleName == "Developer");
            Assert.Contains(result, e => e.FullName == "Jane Smith" && e.RoleName == "Manager");
        }

        [Fact]
        public async Task GetAllEmployees_ReturnsEmptyList_WhenNoEmployeesExist()
        {
            // Arrange
            _mockEmployeeRepo.Setup(r => r.GetAllEmployees()).ReturnsAsync(new List<Employee>());

            // Act
            var result = await _employeeService.GetAllEmployees();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ========== GetEmployeeById ==========
        [Fact]
        public async Task GetEmployeeById_ReturnsMappedEmployee_WhenUserIsAdminAndEmployeeExists()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 2;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Admin");

            // fake user & employee
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };
            var mockEmployee = new Employee
            {
                Id = employeeRequestedId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                Role = new Role { Name = "Engineer" }
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeRequestedId)).ReturnsAsync(mockEmployee);

            // Act
            var result = await _employeeService.GetEmployeeById(employeeRequestedId, principal);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Alice Smith", result.FullName);
            Assert.Equal("Engineer", result.RoleName);

            // Verify
            _mockEmployeeRepo.Verify(r => r.GetEmployeeById(employeeRequestedId), Times.Once);
        }

        [Fact]
        public async Task GetEmployeeById_ReturnsMappedEmployee_WhenUserRequestsTheirOwnData()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 1;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Employee");

            // fake user & employee
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };
            var mockEmployee = new Employee
            {
                Id = employeeRequestedId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                Role = new Role { Name = "Engineer" }
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeRequestedId)).ReturnsAsync(mockEmployee);

            // Act
            var result = await _employeeService.GetEmployeeById(employeeRequestedId, principal);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Alice Smith", result.FullName);
            Assert.Equal("Engineer", result.RoleName);
        }

        [Fact]
        public async Task GetEmployeeById_ThrowsException_WhenUserUnauthorized()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 2;

            var principal = CreateClaims(userId, "Employee");

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _employeeService.GetEmployeeById(employeeRequestedId, principal));

            // Verify
            _mockEmployeeRepo.Verify(r => r.GetEmployeeById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetEmployeeById_ReturnsNull_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var nonExistingRequestedId = 2;

            var principal = CreateClaims(userId, "Admin");

            // fake user
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(nonExistingRequestedId)).ReturnsAsync((Employee?)null);

            // Act
            var result = await _employeeService.GetEmployeeById(nonExistingRequestedId, principal);

            // Assert
            Assert.Null(result);
        }

        // ========== CreateEmployee ==========
        [Fact]
        public async Task CreateEmployee_CreatesEmployee_WhenSuccessful()
        {
            // Arrange
            var roleId = 3;
            var employeeId = 1;

            // fake role & employee
            var mockRole = new Role { Id = roleId, Name = "Engineer" };
            var mockEmployee = new Employee
            {
                Id = employeeId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = roleId,
                Role = mockRole
            };

            var mockEmployeeCreateDto = CreateTestEmployeeDto(roleId);

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync(mockRole);
            _mockEmployeeRepo.Setup(r => r.CreateEmployee(It.IsAny<Employee>())).ReturnsAsync(mockEmployee);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeId)).ReturnsAsync(mockEmployee);

            // Act
            var result = await _employeeService.CreateEmployee(mockEmployeeCreateDto);

            // Assert
            Assert.Equal("Alice Smith", result.FullName);
            Assert.Equal(30, result.Age);
            Assert.Equal("alice@example.com", result.Email);
            Assert.Equal("1234567890", result.Phone);
            Assert.Equal("Developer", result.Position);
            Assert.Equal(new DateTime(2020, 1, 1).ToString("yyyy-MM-dd"), result.DateOfHire);
            Assert.Equal("Engineer", result.RoleName);

            // Verify
            _mockEmployeeRepo.Verify(r => r.CreateEmployee(It.Is<Employee>(employee =>
                employee.FirstName == "Alice" &&
                employee.LastName == "Smith" &&
                employee.Email == "alice@example.com" &&
                employee.Age == 30 &&
                employee.Phone == "1234567890" &&
                employee.Position == "Developer" &&
                employee.RoleId == roleId
            )), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_ReturnsNull_WhenRoleDoesNotExist()
        {
            // Arrange
            var roleId = 3;

            // fake employee
            var mockEmployeeCreateDto = CreateTestEmployeeDto(roleId);

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync((Role?)null);

            // Act
            var result = await _employeeService.CreateEmployee(mockEmployeeCreateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateEmployee_ThrowsException_WhenEmployeeDataIsInvalid()
        {
            // Arrange
            var roleId = 3;
            var employeeId = 1;

            // fake role & employee
            var mockRole = new Role { Id = roleId, Name = "Engineer" };

            var mockInvalidEmployeeDto = new EmployeeCreateDto
            {
                FirstName = "Alice",
                LastName = "Smith",
                Age = -50,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = roleId
            };

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync(mockRole);

            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _employeeService.CreateEmployee(mockInvalidEmployeeDto));

            // Assert
            Assert.Contains("Age must be more than 18.", ex.Message);
        }

        // ========== DeleteEmployeeById ==========
        [Fact]
        public async Task DeleteEmployeeById_DeletesEmployee_WhenEmployeeExistsWithoutUser()
        {
            // Arrange
            var employeeId = 1;
            var mockEmployee = new Employee
            {
                Id = employeeId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = 2
            };

            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeId)).ReturnsAsync(mockEmployee);

            // Act
            var result = await _employeeService.DeleteEmployeeById(employeeId);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.Error);

            // Verify
            _mockEmployeeRepo.Verify(r => r.DeleteEmployee(mockEmployee), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeById_ReturnsFalse_WhenEmployeeDoesNotExist()
        {
            // Arrange
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(It.IsAny<int>())).ReturnsAsync((Employee?)null);

            // Act
            var result = await _employeeService.DeleteEmployeeById(1);

            // Assert
            Assert.False(result.success);
            Assert.Equal("EmployeeNotFound", result.Error);

            // Verify
            _mockEmployeeRepo.Verify(r => r.DeleteEmployee(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployeeById_ReturnsFalse_WhenEmployeeHasLinkedUser()
        {
            // Arrange
            var employeeId = 1;
            var mockEmployee = new Employee
            {
                Id = employeeId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = 2,
                User = new User { Username = "Test", Password = "123" }
            };

            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeId)).ReturnsAsync(mockEmployee);

            // Act
            var result = await _employeeService.DeleteEmployeeById(employeeId);

            // Assert
            Assert.False(result.success);
            Assert.Equal("EmployeeWithUser", result.Error);

            // Verify
            _mockEmployeeRepo.Verify(r => r.DeleteEmployee(It.IsAny<Employee>()), Times.Never);
        }

        // ========== UpdateEmployee ==========
        [Fact]
        public async Task UpdateEmployee_UpdatesEmployee_WhenIsAdmin()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 2;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Admin");

            // fake user & employee
            var mockRole = new Role { Id = 1, Name = "Engineer" };
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };
            var mockEmployee = new Employee
            {
                Id = employeeRequestedId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                Role = new Role { Name = "Engineer" }
            };

            var mockEmployeeUpdateDto = new EmployeeUpdateDto
            {
                FirstName = "Gina",
                LastName = "Jones",
                Age = 20,
                Email = "gina@example.com"
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeRequestedId)).ReturnsAsync(mockEmployee);
            _mockRoleRepo.Setup(r => r.GetRoleById(It.IsAny<int>())).ReturnsAsync(mockRole);
            _mockEmployeeRepo.Setup(r => r.UpdateEmployee(It.IsAny<Employee>())).ReturnsAsync(mockEmployee);

            // Act
            var result = await _employeeService.UpdateEmployee(employeeRequestedId, mockEmployeeUpdateDto, principal);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.Equal("Gina Jones", result.employeeReadDto.FullName);
            Assert.Equal(20, result.employeeReadDto.Age);
            Assert.Equal("gina@example.com", result.employeeReadDto.Email);
            Assert.Equal("1234567890", result.employeeReadDto.Phone);
            Assert.Equal("Developer", result.employeeReadDto.Position);
            Assert.Equal("2020-01-01", result.employeeReadDto.DateOfHire);
            Assert.Equal("Engineer", result.employeeReadDto.RoleName);

            // Verify
            _mockEmployeeRepo.Verify(repo => repo.UpdateEmployee(It.Is<Employee>(e =>
                e.FirstName == "Gina" &&
                e.LastName == "Jones" &&
                e.Age == 20 &&
                e.Email == "gina@example.com"
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateEmployee_UpdatesEmployee_WhenUserUpdatesTheirOwnData()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 1;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Employee");

            // fake user & employee
            var mockRole = new Role { Id = 1, Name = "Engineer" };
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };
            var mockEmployee = new Employee
            {
                Id = employeeRequestedId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                Role = new Role { Name = "Engineer" }
            };

            var mockEmployeeUpdateDto = new EmployeeUpdateDto
            {
                FirstName = "Gina",
                LastName = "Jones",
                Age = 20,
                Email = "gina@example.com"
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeRequestedId)).ReturnsAsync(mockEmployee);
            _mockRoleRepo.Setup(r => r.GetRoleById(It.IsAny<int>())).ReturnsAsync(mockRole);
            _mockEmployeeRepo.Setup(r => r.UpdateEmployee(It.IsAny<Employee>())).ReturnsAsync(mockEmployee);

            // Act
            var result = await _employeeService.UpdateEmployee(employeeRequestedId, mockEmployeeUpdateDto, principal);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.Equal("Gina Jones", result.employeeReadDto.FullName);
            Assert.Equal(20, result.employeeReadDto.Age);
            Assert.Equal("gina@example.com", result.employeeReadDto.Email);
            Assert.Equal("1234567890", result.employeeReadDto.Phone);
            Assert.Equal("Developer", result.employeeReadDto.Position);
            Assert.Equal("2020-01-01", result.employeeReadDto.DateOfHire);
            Assert.Equal("Engineer", result.employeeReadDto.RoleName);

            // Verify
            _mockEmployeeRepo.Verify(repo => repo.UpdateEmployee(It.Is<Employee>(e =>
                e.FirstName == "Gina" &&
                e.LastName == "Jones" &&
                e.Age == 20 &&
                e.Email == "gina@example.com"
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateEmployee_ReturnsFalse_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 2;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Admin");

            // fake user & employee
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };

            var mockEmployeeUpdateDto = new EmployeeUpdateDto
            {
                FirstName = "Gina",
                LastName = "Jones",
                Age = 20,
                Email = "gina@example.com"
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeRequestedId)).ReturnsAsync((Employee?)null);

            // Act
            var result = await _employeeService.UpdateEmployee(employeeRequestedId, mockEmployeeUpdateDto, principal);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("EmployeeNotFound", result.Error);
            Assert.Null(result.employeeReadDto);
        }

        [Fact]
        public async Task UpdateEmployee_ReturnsFalse_WhenRoleDoesNotExist()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 2;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Admin");

            // fake user & employee
            var mockRole = new Role { Id = 1, Name = "Engineer" };
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };
            var mockEmployee = new Employee
            {
                Id = employeeRequestedId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                Role = new Role { Name = "Engineer" }
            };

            var mockEmployeeUpdateDto = new EmployeeUpdateDto
            {
                FirstName = "Gina",
                LastName = "Jones",
                Age = 20,
                Email = "gina@example.com",
                RoleId = 3
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeRequestedId)).ReturnsAsync(mockEmployee);
            _mockRoleRepo.Setup(r => r.GetRoleById(It.IsAny<int>())).ReturnsAsync((Role?)null);
            
            // Act
            var result = await _employeeService.UpdateEmployee(employeeRequestedId, mockEmployeeUpdateDto, principal);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("RoleNotFound", result.Error);
            Assert.Null(result.employeeReadDto);
        }

        [Fact]
        public async Task UpdateEmployee_ThrowsException_WhenEmployeeDataIsInvalid()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 2;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Admin");

            // fake user & employee
            var mockRole = new Role { Id = 1, Name = "Engineer" };
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };
            var mockEmployee = new Employee
            {
                Id = employeeRequestedId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                Role = new Role { Name = "Engineer" }
            };

            var mockInvalidEmployeeDto = new EmployeeUpdateDto
            {
                FirstName = "Gina",
                LastName = "Jones",
                Age = 20,
                Email = "gina@example.com",
                Phone = "3324"
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeRequestedId)).ReturnsAsync(mockEmployee);
            _mockRoleRepo.Setup(r => r.GetRoleById(It.IsAny<int>())).ReturnsAsync(mockRole);
            
            // Act
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _employeeService.UpdateEmployee(employeeRequestedId, mockInvalidEmployeeDto, principal));

            // Assert
            Assert.Contains("Phone number must be exactly 10 digits.", ex.Message);
        }

        [Fact]
        public async Task UpdateEmployee_ThrowsException_WhenUnauthorized()
        {
            // Arrange
            var userEmployeeId = 1;
            var userId = 99;
            var employeeRequestedId = 2;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Employee");

            // fake user & employee
            var mockRole = new Role { Id = 1, Name = "Engineer" };
            var mockUser = new User { Id = userId, EmployeeId = userEmployeeId };

            var mockEmployeeUpdateDto = new EmployeeUpdateDto
            {
                FirstName = "Gina",
                LastName = "Jones",
                Age = 20,
                Email = "gina@example.com"
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(mockUser);

            // Act
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _employeeService.UpdateEmployee(employeeRequestedId, mockEmployeeUpdateDto, principal));
        }


        private EmployeeCreateDto CreateTestEmployeeDto(int roleId)
        {
            return new EmployeeCreateDto
            {
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = roleId
            };
        }

        private ClaimsPrincipal CreateClaims(int userId, string roleName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, roleName)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }
    }
}