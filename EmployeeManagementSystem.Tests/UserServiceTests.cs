using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Castle.Core.Logging;
using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using EmployeeManagementSystem.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmployeeManagementSystem.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IEmployeeRepository> _mockEmployeeRepo;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockEmployeeRepo = new Mock<IEmployeeRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();

            _userService = new UserService(
                _mockUserRepo.Object,
                _mockEmployeeRepo.Object,
                _mockLogger.Object);
        }

        // ========== GetAllUsers ==========
        [Fact]
        public async Task GetAllUsers_ReturnsMappedDtos()
        {
            // Arrange
            _mockUserRepo.Setup(Repositories => Repositories.GetAllUsers()).ReturnsAsync(new List<User>
            {
                new User { Id = 1, Username = "aliceSmith", Password = "123", EmployeeId = 1 },
                new User { Id = 2, Username = "ginaJones", Password = "456", EmployeeId = 2 },
                new User { Id = 3, Username = "johnDow", Password = "789", EmployeeId = 3 },
            });

            // Act
            var result = await _userService.GetAllUsers();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, u => u.Id == 1 && u.Username == "aliceSmith" && u.EmployeeId == 1);
            Assert.Contains(result, u => u.Id == 2 && u.Username == "ginaJones" && u.EmployeeId == 2);
            Assert.Contains(result, u => u.Id == 3 && u.Username == "johnDow" && u.EmployeeId == 3);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsEmptyList_WhenNoUsersExist()
        {
            // Arrange
            _mockUserRepo.Setup(Repositories => Repositories.GetAllUsers()).ReturnsAsync(new List<User>());

            // Act
            var result = await _userService.GetAllUsers();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ========== GetUserById ==========
        [Fact]
        public async Task GetUserById_ReturnsMappedUser_WhenUserIsAdminAndUserExists()
        {
            // Arrange
            var userId = 1;
            var userRequestedId = 2;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Admin");

            // fake user
            var mockUser = new User
            {
                Id = userRequestedId,
                Username = "aliceSmith",
                Password = "123",
                EmployeeId = 1
            };

            _mockUserRepo.Setup(r => r.GetUserById(userRequestedId)).ReturnsAsync(mockUser);

            // Act
            var result = await _userService.GetUserId(userRequestedId, principal);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("aliceSmith", result.Username);
            Assert.Equal(1, result.EmployeeId);

            // Verify
            _mockUserRepo.Verify(r => r.GetUserById(userRequestedId), Times.Once);
        }

        [Fact]
        public async Task GetUserById_ReturnsMappedUser_WhenUserRequestsTheirOwnData()
        {
            // Arrange
            var userId = 1;
            var userRequestedId = 1;

            // fake ClaimsPrincipal
            var principal = CreateClaims(userId, "Employee");

            // fake user
            var mockUser = new User
            {
                Id = userRequestedId,
                Username = "aliceSmith",
                Password = "123",
                EmployeeId = 1
            };

            _mockUserRepo.Setup(r => r.GetUserById(userRequestedId)).ReturnsAsync(mockUser);

            // Act
            var result = await _userService.GetUserId(userRequestedId, principal);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("aliceSmith", result.Username);
            Assert.Equal(1, result.EmployeeId);

            // Verify
            _mockUserRepo.Verify(r => r.GetUserById(userRequestedId), Times.Once);
        }

        [Fact]
        public async Task GetUserById_ThrowsException_WhenUserUnauthorized()
        {
            // Arrange
            var userId = 1;
            var userRequestedId = 2;
            var principal = CreateClaims(userId, "Employee");

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _userService.GetUserId(userRequestedId, principal));

            // Verify
            _mockUserRepo.Verify(r => r.GetUserById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetUserById_ReturnsNull_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var userId = 1;
            var nonExistingRequestId = 2;

            var principal = CreateClaims(userId, "Admin");

            _mockUserRepo.Setup(r => r.GetUserById(nonExistingRequestId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUserId(nonExistingRequestId, principal);

            // Assert
            Assert.Null(result);
        }

        // ========== CreateUser ==========
        [Fact]
        public async Task CreateUser_CreatesUser_WhenSuccessful()
        {
            // Arrange
            var userEmployeeId = 99;

            var user = new User()
            {
                Username = "aliceSmith",
                Password = "password",
                EmployeeId = userEmployeeId
            };

            var userCreateDto = new UserCreateDto()
            {
                Username = "aliceSmith",
                Password = "password",
                EmployeeId = userEmployeeId
            };

            _mockUserRepo.Setup(r => r.GetByEmployeeId(userEmployeeId)).ReturnsAsync((User?)null);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(userEmployeeId)).ReturnsAsync(new Employee { Id = userEmployeeId, FirstName = "Placeholder" });
            _mockUserRepo.Setup(r => r.CreateUser(It.IsAny<User>())).ReturnsAsync(user);

            // Act
            var result = await _userService.CreateUser(userCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("aliceSmith", result.Username);
            Assert.Equal(userEmployeeId, result.EmployeeId);

            // Verify
            _mockUserRepo.Verify(r => r.CreateUser(It.Is<User>(user =>
                user.Username == "aliceSmith" &&
                user.EmployeeId == userEmployeeId
            )), Times.Once);
        }

        [Fact]
        public async Task CreateUserThrowsException_WhenUsernameAlreadyExist()
        {
            // Arrange
            var userEmployeeId = 1;

            var userCreateDto = new UserCreateDto()
            {
                Username = "aliceSmith",
                Password = "password",
                EmployeeId = userEmployeeId
            };

            _mockUserRepo.Setup(r => r.GetUserByUsername("aliceSmith")).ReturnsAsync(new User { Id = userEmployeeId, EmployeeId = 1, Username = "aliceSmith" });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.CreateUser(userCreateDto));

            // Verify
            _mockUserRepo.Verify(r => r.GetByEmployeeId(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateUser_ThrowsException_WhenEmployeeIsLinkedToAnotherUser()
        {
            // Arrange
            var userCreateDto = new UserCreateDto()
            {
                Username = "aliceSmith",
                Password = "password",
                EmployeeId = 1
            };

            _mockUserRepo.Setup(r => r.GetByEmployeeId(It.IsAny<int>())).ReturnsAsync(new User { Id = 123, EmployeeId = 1, Username = "aliceSmith" });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.CreateUser(userCreateDto));

            // Verify
            _mockEmployeeRepo.Verify(r => r.GetEmployeeById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateUser_ThrowsException_WhenEmployeeDoesNotExist()
        {
            // Arrange
            var userEmployeeId = 99;

            var userCreateDto = new UserCreateDto()
            {
                Username = "aliceSmith",
                Password = "password",
                EmployeeId = userEmployeeId
            };

            _mockUserRepo.Setup(r => r.GetByEmployeeId(userEmployeeId)).ReturnsAsync((User?)null);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(userEmployeeId)).ReturnsAsync((Employee?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _userService.CreateUser(userCreateDto));

            // Verify
            _mockUserRepo.Verify(r => r.CreateUser(It.IsAny<User>()), Times.Never);
        }

        // ========== DeleteUser ==========
        [Fact]
        public async Task DeleteUser_DeletesUser_WhenSuccessfull()
        {
            // Arrange
            var userId = 1;

            var user = new User()
            {
                Id = userId,
                Username = "aliceSmith",
                Password = "1234",
                EmployeeId = 1
            };

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.DeleteUser(user)).Returns(Task.CompletedTask);

            // Act
            var result = await _userService.DeleteUserById(userId);

            // Assert
            Assert.True(result);

            // Verify
            _mockUserRepo.Verify(r => r.DeleteUser(user), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_ReturnsFalse_WhenUserDoesNotExist()
        {
            // Arrange
            var nonExistingUserId = 99;

            _mockUserRepo.Setup(r => r.GetUserById(nonExistingUserId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.DeleteUserById(nonExistingUserId);

            // Assert
            Assert.False(result);

            // Verify
            _mockUserRepo.Verify(r => r.DeleteUser(It.IsAny<User>()), Times.Never);
        }

        // ========== UpdateUser ==========
        [Fact]
        public async Task UpdateUser_UpdatesUser_WhenUserIsAdmin()
        {
            // Arrange
            var userId = 1;
            var userToUpdateId = 2;
            var employeeUpdateId = 2;

            var userToUpdate = new User()
            {
                Id = userToUpdateId,
                Username = "aliceSmith",
                Password = "1234",
                EmployeeId = employeeUpdateId
            };

            var userUpdated = new User()
            {
                Id = userToUpdateId,
                Username = "aliceSmith",
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var userUpdateDto = new UserUpdateDto()
            {
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var employee = new Employee()
            {
                Id = employeeUpdateId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = 2,
                User = userToUpdate
            };

            var principal = CreateClaims(userId, "Admin");

            _mockUserRepo.Setup(r => r.GetUserById(userToUpdateId)).ReturnsAsync(userToUpdate);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeUpdateId)).ReturnsAsync(employee);
            _mockUserRepo.Setup(r => r.GetByEmployeeId(employeeUpdateId)).ReturnsAsync(userToUpdate);
            _mockUserRepo.Setup(r => r.UpdateUser(userToUpdate)).ReturnsAsync(userUpdated);

            // Act
            var result = await _userService.UpdateUser(userToUpdateId, userUpdateDto, principal);

            // Assert
            Assert.Equal(userToUpdateId, result.Id);
            Assert.Equal("aliceSmith", result.Username);
            Assert.Equal(employeeUpdateId, result.EmployeeId);

            // Verify
            _mockUserRepo.Verify(r => r.UpdateUser(userToUpdate), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_UpdatesUser_WhenUserUpdatesItself()
        {
            // Arrange
            var userId = 1;
            var employeeUpdateId = 2;

            var user = new User()
            {
                Id = userId,
                Username = "aliceSmith",
                Password = "1234",
                EmployeeId = employeeUpdateId
            };

            var userUpdated = new User()
            {
                Id = userId,
                Username = "aliceSmith",
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var userUpdateDto = new UserUpdateDto()
            {
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var employee = new Employee()
            {
                Id = employeeUpdateId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = 2,
                User = user
            };

            var principal = CreateClaims(userId, "Employee");

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync(user);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeUpdateId)).ReturnsAsync(employee);
            _mockUserRepo.Setup(r => r.GetByEmployeeId(employeeUpdateId)).ReturnsAsync(user);
            _mockUserRepo.Setup(r => r.UpdateUser(user)).ReturnsAsync(userUpdated);

            // Act
            var result = await _userService.UpdateUser(userId, userUpdateDto, principal);

            // Assert
            Assert.Equal(userId, result.Id);
            Assert.Equal("aliceSmith", result.Username);
            Assert.Equal(employeeUpdateId, result.EmployeeId);

            // Verify
            _mockUserRepo.Verify(r => r.UpdateUser(user), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ThrowsException_WhenUserUnauthorized()
        {
            // Arrange
            var userId = 1;
            var userToUpdateId = 2;
            var employeeUpdateId = 2;

            var userUpdateDto = new UserUpdateDto()
            {
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var principal = CreateClaims(userId, "Employee");

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _userService.UpdateUser(userToUpdateId, userUpdateDto, principal));

            // Verify
            _mockUserRepo.Verify(r => r.GetUserById(userToUpdateId), Times.Never);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = 1;
            var employeeUpdateId = 2;

            var userUpdateDto = new UserUpdateDto()
            {
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var principal = CreateClaims(userId, "Employee");

            _mockUserRepo.Setup(r => r.GetUserById(userId)).ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateUser(userId, userUpdateDto, principal);

            // Assert
            Assert.Null(result);

            // Verify
            _mockUserRepo.Verify(r => r.UpdateUser(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateUser_ThrowsException_WhenEmployeeNotFound()
        {
            // Arrange
            var userId = 1;
            var userToUpdateId = 2;
            var employeeUpdateId = 2;

            var userToUpdate = new User()
            {
                Id = userToUpdateId,
                Username = "aliceSmith",
                Password = "1234",
                EmployeeId = employeeUpdateId
            };

            var userUpdateDto = new UserUpdateDto()
            {
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var principal = CreateClaims(userId, "Admin");

            _mockUserRepo.Setup(r => r.GetUserById(userToUpdateId)).ReturnsAsync(userToUpdate);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeUpdateId)).ReturnsAsync((Employee?)null);

            // Act && Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.UpdateUser(userToUpdateId, userUpdateDto, principal));

            // Verify
            _mockEmployeeRepo.Verify(r => r.GetEmployeeById(employeeUpdateId), Times.Once);
            _mockUserRepo.Verify(r => r.UpdateUser(It.IsAny<User>()), Times.Never);
        }
        
        [Fact]
        public async Task UpdateUser_ThrowsException_WhenEmployeeLinkedToOtherUser()
        {
            // Arrange
            var userId = 1;
            var userToUpdateId = 2;
            var employeeUpdateId = 2;
            var existingUserId = 3;

            var userToUpdate = new User()
            {
                Id = userToUpdateId,
                Username = "aliceSmith",
                Password = "1234",
                EmployeeId = employeeUpdateId
            };
            
            var existingUser = new User()
            {
                Id = existingUserId,
                Username = "ginaJones",
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var userUpdateDto = new UserUpdateDto()
            {
                Password = "4321",
                EmployeeId = employeeUpdateId
            };

            var employee = new Employee()
            {
                Id = employeeUpdateId,
                FirstName = "Alice",
                LastName = "Smith",
                Age = 30,
                Email = "alice@example.com",
                Phone = "1234567890",
                Position = "Developer",
                DateOfHire = new DateTime(2020, 1, 1),
                RoleId = 2,
                User = userToUpdate
            };

            var principal = CreateClaims(userId, "Admin");

            _mockUserRepo.Setup(r => r.GetUserById(userToUpdateId)).ReturnsAsync(userToUpdate);
            _mockEmployeeRepo.Setup(r => r.GetEmployeeById(employeeUpdateId)).ReturnsAsync(employee);
            _mockUserRepo.Setup(r => r.GetByEmployeeId(employeeUpdateId)).ReturnsAsync(existingUser);

            // Act && Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.UpdateUser(userToUpdateId, userUpdateDto, principal));

            // Verify
            _mockUserRepo.Verify(r => r.UpdateUser(It.IsAny<User>()), Times.Never);
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
