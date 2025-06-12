using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllRoles();
        Task<Role?> GetRoleById(int id);
        Task<Role> CreateRole(Role role);
        Task DeleteRole(Role role);
        Task<Role> UpdateRole(Role role);
    }
}
