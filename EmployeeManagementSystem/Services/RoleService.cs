using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using EmployeeManagementSystem.Services.Interfaces;

namespace EmployeeManagementSystem.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<RoleService> _logger;
        public RoleService(IRoleRepository roleRepository, ILogger<RoleService> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<List<RoleReadDto>> GetAllRoles()
        {
            var roles = await _roleRepository.GetAllRoles();
            var roleDtos = roles.Select(MapToDto).ToList();

            _logger.LogInformation("Successfully fetched {Count} roles.", roleDtos.Count);

            return roleDtos;
        }

        public async Task<RoleReadDto> GetRoleById(int id)
        {
            var role = await _roleRepository.GetRoleById(id);
            if (role == null)
            {
                return null;
            }

            var dto = MapToDto(role);

            return dto;
        }

        public async Task<RoleReadDto> CreateRole(RoleCreateDto roleDto)
        {
            Role role = new Role
            {
                Name = roleDto.Name
            };

            var createdRole = await _roleRepository.CreateRole(role);
            _logger.LogInformation("Created role with ID {Id}.", createdRole.Id);

            var dto = MapToDto(createdRole);

            return dto;
        }

        public async Task<(bool success, string Error)> DeleteRole(int id)
        {
            var role = await _roleRepository.GetRoleById(id);

            if (role == null)
            {
                return (false, "RoleNotFound");
            }

            if(role.Employees != null && role.Employees.Any())
            {
                return (false, "EmployeeWithRole");
            }

            await _roleRepository.DeleteRole(role);
            _logger.LogInformation("Deleted role with ID {Id}.", id);

            return (true, null);
        }

        public async Task<RoleReadDto> UpdateRole(int id, RoleCreateDto updateRole)
        {
            var role = await _roleRepository.GetRoleById(id);

            if (role == null)
            {
                return null;
            }

            if(updateRole.Name != null)
                role.Name = updateRole.Name;

            var savedRole = await _roleRepository.UpdateRole(role);
            _logger.LogInformation("Updated role with ID {Id}.", id);

            var dto = MapToDto(savedRole);

            return dto;
        }

        private RoleReadDto MapToDto(Role r) => new()
        {
            Id = r.Id,
            Name = r.Name
        };
    }
}
