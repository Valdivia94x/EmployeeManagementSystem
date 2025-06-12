using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly EmployeeContext _employeeContext;
        private readonly ILogger<RoleRepository> _logger;
        public RoleRepository(EmployeeContext employeeContext, ILogger<RoleRepository> logger)
        {
            _employeeContext = employeeContext;
            _logger = logger;
        }

        public async Task<List<Role>> GetAllRoles()
        {
            try
            {
                List<Role> roles = await _employeeContext.Roles.AsNoTracking().ToListAsync();

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetAllRoles.");
                throw;
            }
        }

        public async Task<Role?> GetRoleById(int id)
        {
            try
            {
                Role? role = await _employeeContext.Roles.Include(r => r.Employees).FirstOrDefaultAsync(r => r.Id == id);

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetRoleById({RoleId})", id);
                throw;
            }
        }

        public async Task<Role> CreateRole(Role role)
        {
            try
            {
                _employeeContext.Roles.Add(role);
                await _employeeContext.SaveChangesAsync();

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in CreateRole.");
                throw;
            }
        }

        public async Task DeleteRole(Role role)
        {
            try
            {
                _employeeContext.Roles.Remove(role);
                await _employeeContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in DeleteRole({RoleId})", role.Id);
                throw;
            }
        }

        public async Task<Role> UpdateRole(Role role)
        {
            try
            {
                await _employeeContext.SaveChangesAsync();

                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in UpdateRole with ID {RoleId}.", role.Id);
                throw;
            }
        }
    }
}
