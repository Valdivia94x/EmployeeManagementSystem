using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using EmployeeManagementSystem.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmployeeManagementSystem.Tests
{
    public class RoleServiceTests
    {
        private readonly Mock<IRoleRepository> _mockRoleRepo;
        private readonly Mock<ILogger<RoleService>> _mockLogger;
        private readonly RoleService _roleService;

        public RoleServiceTests()
        {
            _mockRoleRepo = new Mock<IRoleRepository>();
            _mockLogger = new Mock<ILogger<RoleService>>();

            _roleService = new RoleService(
                _mockRoleRepo.Object,
                _mockLogger.Object);
        }

        // ========== GetAllRoles ==========
        [Fact]
        public async Task GetAllRoles_ReturnsMappedDtos()
        {
            // Arrange
            _mockRoleRepo.Setup(Repositories => Repositories.GetAllRoles()).ReturnsAsync(new List<Role>
            {
                new Role { Id = 1, Name = "Admin"},
                new Role { Id = 2, Name = "HR"},
                new Role { Id = 3, Name = "Employee"}
            });

            // Act
            var result = await _roleService.GetAllRoles();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains(result, e => e.Id == 1 && e.Name == "Admin");
            Assert.Contains(result, e => e.Id == 2 && e.Name == "HR");
            Assert.Contains(result, e => e.Id == 3 && e.Name == "Employee");
        }

        [Fact]
        public async Task GetAllRoles_ReturnsEmptyList_WhenNoRolesExist()
        {
            // Arrange
            _mockRoleRepo.Setup(r => r.GetAllRoles()).ReturnsAsync(new List<Role>());

            // Act
            var result = await _roleService.GetAllRoles();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // ========== GetRoleById ==========
        [Fact]
        public async Task GetRoleById_ReturnsMappedRole_WhenRoleExists()
        {
            var roleRequestedId = 1;

            // fake role
            var mockRole = new Role
            {
                Id = 1,
                Name = "Admin"
            };

            _mockRoleRepo.Setup(r => r.GetRoleById(roleRequestedId)).ReturnsAsync(mockRole);

            // Act
            var result = await _roleService.GetRoleById(roleRequestedId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Admin", result.Name);
        }

        [Fact]
        public async Task GetRoleById_ReturnsNull_WhenRoleDoesNotExist()
        {
            // Arrange
            var nonExistingRequestedId = 4;

            _mockRoleRepo.Setup(r => r.GetRoleById(nonExistingRequestedId)).ReturnsAsync((Role?)null);

            // Act
            var result = await _roleService.GetRoleById(nonExistingRequestedId);

            // Assert
            Assert.Null(result);
        }

        //  ========== CreateRole ==========
        [Fact]
        public async Task CreateRole_CreatesRole_WhenSuccessful()
        {
            // fake role
            var mockRole = new Role
            {
                Id = 1,
                Name = "Admin"
            };

            var mockRoleCreateDto = CreateTestRoleDto();

            _mockRoleRepo.Setup(r => r.CreateRole(It.IsAny<Role>())).ReturnsAsync(mockRole);

            // Act
            var result = await _roleService.CreateRole(mockRoleCreateDto);

            // Assert
            Assert.Equal(1, result.Id);
            Assert.Equal("Admin", result.Name);

            // Verify
            _mockRoleRepo.Verify(r => r.CreateRole(It.Is<Role>(role =>
                role.Name == "Admin"
            )), Times.Once);
        }

        // ========== DeleteRole ==========
        [Fact]
        public async Task DeleteRole_DeletesRole_WhenRoleExists()
        {
            // Arrange
            var roleId = 1;

            var mockRole = new Role
            {
                Id = roleId,
                Name = "Admin"
            };

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync(mockRole);

            // Act
            var result = await _roleService.DeleteRole(roleId);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.Error);

            // Verify
            _mockRoleRepo.Verify(r => r.DeleteRole(mockRole), Times.Once);
        }

        [Fact]
        public async Task DeleteRole_ReturnsFalse_WhenRoleDoesNotExist()
        {
            // Arrange
            _mockRoleRepo.Setup(r => r.GetRoleById(It.IsAny<int>())).ReturnsAsync((Role?)null);

            // Act
            var result = await _roleService.DeleteRole(1);

            // Assert
            Assert.False(result.success);
            Assert.Equal("RoleNotFound", result.Error);

            // Verify
            _mockRoleRepo.Verify(r => r.DeleteRole(It.IsAny<Role>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRole_ReturnsFalse_WhenEmployeeHasRole()
        {
            // Arrange
            var roleId = 1;

            var mockRole = new Role
            {
                Id = roleId,
                Name = "Admin",
                Employees = new List<Employee>
                {
                    new Employee { Id = 101, FirstName = "Alice", LastName = "Smith" }
                }
            };

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync(mockRole);

            // Act
            var result = await _roleService.DeleteRole(roleId);

            // Assert
            Assert.False(result.success);
            Assert.Equal("EmployeeWithRole", result.Error);

            // Verify
            _mockRoleRepo.Verify(r => r.DeleteRole(It.IsAny<Role>()), Times.Never);
        }

        // ========== UpdateRole ==========
        [Fact]
        public async Task UpdateRole_UpdatesRole_WhenSuccessful()
        {
            // Arrange
            var roleId = 1;

            var mockRole = new Role
            {
                Id = roleId,
                Name = "Admin"
            };

            var mockRoleCreateDto = new RoleCreateDto
            {
                Name = "HR"
            };

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync(mockRole);
            _mockRoleRepo.Setup(r => r.UpdateRole(It.IsAny<Role>())).ReturnsAsync((Role r) => r);

            // Act
            var result = await _roleService.UpdateRole(roleId, mockRoleCreateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleId, result.Id);
            Assert.Equal("HR", result.Name);

            // Verify
            _mockRoleRepo.Verify(repo => repo.UpdateRole(It.Is<Role>(r =>
                r.Id == roleId &&
                r.Name == "HR"
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateRole_ReturnsNull_WhenRoleDoesNotExist()
        {
            // Assert
            var roleId = 1;

            var mockRoleCreateDto = new RoleCreateDto
            {
                Name = "HR"
            };

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync((Role?) null);

            // Act
            var result = await _roleService.UpdateRole(roleId, mockRoleCreateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateRole_DoesNotChangeName_WhenNameIsNull()
        {
            // Arrange
            var roleId = 1;
            var originalName = "Admin";

            var mockRole = new Role
            {
                Id = roleId,
                Name = originalName
            };

            var mockRoleCreateDto = new RoleCreateDto
            {
                Name = null
            };

            _mockRoleRepo.Setup(r => r.GetRoleById(roleId)).ReturnsAsync(mockRole);
            _mockRoleRepo.Setup(r => r.UpdateRole(It.IsAny<Role>())).ReturnsAsync((Role r) => r);

            // Act
            var result = await _roleService.UpdateRole(roleId, mockRoleCreateDto);

            // Assert
            Assert.Equal(originalName, result.Name);
        }

        private RoleCreateDto CreateTestRoleDto()
        {
            return new RoleCreateDto
            {
                Name = "Admin"
            };
        }
    }
}
