using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ITokenService jwtService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _tokenService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation("Login attempt for user: {Username}", loginDto.Username);

            var user = await _userService.AuthenticateAsync(loginDto.Username, loginDto.Password);
            if (user == null)
            {
                _logger.LogWarning("Invalid login attempt for user: {Username}", loginDto.Username);
                return Unauthorized("Invalid username or password");
            }

            var token = _tokenService.GenerateToken(user.Id, user.Username, user.Employee.Role.Name);
            _logger.LogInformation("Token generated for user: {Username}", loginDto.Username);

            return Ok(new { token = $"Bearer {token}" });
        }
    }
}
