using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin, HR")]
        [HttpGet]
        public async Task<ActionResult<List<UserReadDto>>> GetAllUsers()
        {
            _logger.LogInformation("GET /users requested by {User}", User.Identity?.Name);

            try
            {
                var users = await _userService.GetAllUsers();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllUsers for user {User}", User.Identity?.Name);
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [Authorize(Roles = "Admin, HR, Employee")]
        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserId(id, User);

                if (user == null)
                {
                    _logger.LogWarning("GET /users/{id} - Not Found", id);
                    return NotFound($"User with id {id} not found.");
                }

                return Ok(user);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bearer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting user with ID {UserId}", id);
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [Authorize(Roles = "Admin, HR")]
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser([FromBody] UserCreateDto user)
        {
            _logger.LogInformation("POST /users: {UserName}", user.Username);

            try
            {
                var userCreated = await _userService.CreateUser(user);
                return CreatedAtAction(nameof(GetUserById), new { id = userCreated.Id }, userCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating user.");
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            _logger.LogInformation("DELETE /users/id requested by {User}", User.Identity?.Name);

            try
            {
                var success = await _userService.DeleteUserById(id);

                if (!success)
                {
                    _logger.LogWarning("Cannot delete: user with ID {UserId} not found.", id);
                    return NotFound($"User with id {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting user.");
                return StatusCode(500, "Unexpected error occurred.");
            }
        }

        [Authorize(Roles = "Admin, HR, Employee")]
        [HttpPatch("{id}")]
        public async Task<ActionResult<UserReadDto>> UpdateUser(int id, [FromBody] UserUpdateDto user)
        {
            try
            {
                var updatedUser = await _userService.UpdateUser(id, user, User);

                if (updatedUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return NotFound($"User with id {id} not found.");
                }

                return Ok(updatedUser);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid("Bearer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating user.");
                return StatusCode(500, "Unexpected error occurred.");
            }
        }
    }
}
