using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RolesController> _logger;
        public RolesController(IRoleService roleService, ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<RoleReadDto>>> GetAllRoles()
        {
            _logger.LogInformation("GET /roles requested by {User}", User.Identity?.Name);

            try
            {
                var roles = await _roleService.GetAllRoles();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllUsers for user {User}", User.Identity?.Name);
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleReadDto>> GetRoleById(int id)
        {
            _logger.LogInformation("GET /employees/id requested by {User}", User.Identity?.Name);

            try
            {
                var role = await _roleService.GetRoleById(id);
                if (role == null)
                {
                    _logger.LogWarning("GET /roles/{id} - Not Found", id);
                    return NotFound($"Role with id {id} not found.");
                }

                return Ok(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting role with ID {EmployeeId}", id);
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<RoleReadDto>> CreateRole([FromBody] RoleCreateDto role)
        {
            _logger.LogInformation("POST /roles: {RoleName}", role.Name);

            try
            {
                var roleCreated = await _roleService.CreateRole(role);
                return CreatedAtAction(nameof(GetRoleById), new { id = roleCreated.Id }, roleCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating role.");
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteRoleById(int id)
        {
            _logger.LogInformation("DELETE /roles/id requested by {User}", User.Identity?.Name);

            try
            {
                var (success, error) = await _roleService.DeleteRole(id);
                if (!success)
                {
                    _logger.LogWarning("Delete failed for role ID: {Id}. Reason: {Error}", id, error);
                    return error switch
                    {
                        "RoleNotFound" => NotFound($"Role with id {id} not found."),
                        "EmployeeWithRole" => BadRequest($"Role with id {id} linked to an employee."),
                        _ => BadRequest("An unknown error occurred.")
                    };
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting role.");
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<RoleReadDto>> UpdateRole(int id, [FromBody] RoleCreateDto role)
        {
            _logger.LogInformation("PATCH /roles/id requested by {User}", User.Identity?.Name);

            try
            {
                var updatedRole = await _roleService.UpdateRole(id, role);
                if (updatedRole == null)
                {
                    _logger.LogWarning("Role with {id} not found", id);
                    return NotFound($"Role with id {id} not found");
                }

                return Ok(updatedRole);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating role.");
                return StatusCode(500, "Unexpected error occurred.");
            }
        }
    }
}
