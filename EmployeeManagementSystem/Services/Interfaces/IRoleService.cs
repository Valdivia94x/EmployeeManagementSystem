using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EmployeeManagementSystem.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleReadDto>> GetAllRoles();
        Task<RoleReadDto> GetRoleById(int id);
        Task<RoleReadDto> CreateRole(RoleCreateDto roleDto);
        Task<(bool success, string Error)> DeleteRole(int id);
        Task<RoleReadDto> UpdateRole(int id, RoleCreateDto updateRole);
    }
}
