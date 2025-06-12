using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;
        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin, HR")]
        [HttpGet]
        public async Task<ActionResult<List<EmployeeReadDto>>> GetAllEmployees()
        {
            _logger.LogInformation("GET /employees requested by {User}", User.Identity?.Name);

            try
            {
                var employees = await _employeeService.GetAllEmployees();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllEmployees for user {User}", User.Identity?.Name);
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [Authorize(Roles = "Admin, HR, Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeReadDto>> GetEmployeeById(int id)
        {
            _logger.LogInformation("GET /employees/id requested by {User}", User.Identity?.Name);

            try
            {
                var employee = await _employeeService.GetEmployeeById(id, User);
                if (employee == null)
                {
                    _logger.LogWarning("GET /employees/{id} - Not Found", id);
                    return NotFound($"Employee with id {id} not found.");
                }

                return Ok(employee);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bearer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting employee with ID {EmployeeId}", id);
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [Authorize(Roles = "Admin, HR")]
        [HttpPost]
        public async Task<ActionResult<EmployeeReadDto>> CreateEmployee([FromBody] EmployeeCreateDto employee)
        {
            _logger.LogInformation("POST /employees: {FirstName} {LastName}", employee.FirstName, employee.LastName);

            try
            {
                var employeeCreated = await _employeeService.CreateEmployee(employee);

                if (employeeCreated == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found", employee.RoleId);
                    return BadRequest("Invalid role ID");
                }

                return CreatedAtAction(nameof(GetEmployeeById), new { id = employeeCreated.Id }, employeeCreated);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating employee.");
                return StatusCode(500, new { error = "Unexpected error occurred.", details = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            _logger.LogInformation("DELETE /employees/id requested by {User}", User.Identity?.Name);

            try
            {
                var (success, error) = await _employeeService.DeleteEmployeeById(id);
                if (!success)
                {
                    _logger.LogWarning("Delete failed for Employee ID: {Id}. Reason: {Error}", id, error);
                    return error switch
                    {
                        "EmployeeNotFound" => NotFound($"Employee with id {id} not found."),
                        "EmployeeWithUser" => BadRequest($"Employee with id {id} linked to a user."),
                        _ => BadRequest("An unknown error occurred.")
                    };
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting employee.");
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [Authorize(Roles = "Admin, HR, Employee")]
        [HttpPatch("{id}")]
        public async Task<ActionResult<EmployeeReadDto>> UpdateEmployee(int id, [FromBody] EmployeeUpdateDto employee)
        {
            _logger.LogInformation("PATCH /employees/id requested by {User}", User.Identity?.Name);

            try
            {
                var (success, error, updatedEmployee) = await _employeeService.UpdateEmployee(id, employee, User);
                if (!success)
                {
                    _logger.LogWarning("Update failed for Employee ID: {Id}. Reason: {Error}", id, error);
                    return error switch
                    {
                        "EmployeeNotFound" => NotFound($"Employee with id {id} not found."),
                        "RoleNotFound" => NotFound($"Role with id {employee.RoleId} doesn't exist."),
                        _ => BadRequest("An unknown error occurred.")
                    };
                }

                return Ok(updatedEmployee);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bearer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating employee.");
                return StatusCode(500, new { error = "Unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
