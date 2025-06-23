using System.Security.Claims;
using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using EmployeeManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IEmployeeRepository employeeRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<List<UserReadDto>> GetAllUsers()
        {
            List<User> users = await _userRepository.GetAllUsers();
            var userDtos = users.Select(MapToDto).ToList();
            _logger.LogInformation("Successfully retrieved {Count} users.", users.Count);

            return userDtos;
        }

        public async Task<UserReadDto> GetUserId(int id, ClaimsPrincipal user)
        {
            await checkUserPermissions(id, user, "view");

            User userRequested = await _userRepository.GetUserById(id);
            if (userRequested == null)
            {
                return null;
            }

            var userDto = MapToDto(userRequested);
            return userDto;
        }

        public async Task<UserReadDto> CreateUser(UserCreateDto userDto)
        {
            var existingUser = await _userRepository.GetUserByUsername(userDto.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("User with username {Username} already exists.", userDto.Username);
                throw new InvalidOperationException($"User with username {userDto.Username} already exists.");
            }
            var existingUserWithEmployee = await _userRepository.GetByEmployeeId(userDto.EmployeeId);

            if (existingUserWithEmployee != null)
            {
                _logger.LogWarning("Employee with ID {EmployeeId} is already linked to another user.", userDto.EmployeeId);
                throw new InvalidOperationException($"Employee with ID {userDto.EmployeeId} is already linked to another user.");
            }

            int id = userDto.EmployeeId;
            var employee = await _employeeRepository.GetEmployeeById(id);

            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found.", userDto.EmployeeId);
                throw new KeyNotFoundException($"Employee with id {id} not found.");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            User user = new User()
            {
                Username = userDto.Username,
                Password = hashedPassword,
                EmployeeId = userDto.EmployeeId
            };

            var createdUser = await _userRepository.CreateUser(user);
            _logger.LogInformation("User created with ID {UserId}.", createdUser.Id);

            var userCreateDto = MapToDto(createdUser);

            return userCreateDto;
        }

        public async Task<bool> DeleteUserById(int id)
        {
            var user = await _userRepository.GetUserById(id);
            if (user == null)
            {
                return false;
            }

            await _userRepository.DeleteUser(user);

            _logger.LogInformation("Deleted user with ID {UserId} successfully.", id);
            return true;
        }

        public async Task<UserReadDto> UpdateUser(int id, UserUpdateDto updateUser, ClaimsPrincipal user)
        {
            await checkUserPermissions(id, user, "update");

            var userToUpdate = await _userRepository.GetUserById(id);
            if (userToUpdate == null)
            {
                return null;
            }

            if (updateUser.EmployeeId.HasValue)
            {
                //To check if employee exists
                int idUpdate = updateUser.EmployeeId.Value;

                var employee = await _employeeRepository.GetEmployeeById(idUpdate);

                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found.", idUpdate);
                    throw new KeyNotFoundException($"Employee with id {idUpdate} not found.");
                }

                // To check if employee is linked to another user
                var existingUserWithEmployee = await _userRepository.GetByEmployeeId(idUpdate);

                if (existingUserWithEmployee != null && existingUserWithEmployee.Id != userToUpdate.Id)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} is already linked to another user.", idUpdate);
                    throw new InvalidOperationException($"Employee with ID {updateUser.EmployeeId} is already linked to another user.");
                }

                userToUpdate.EmployeeId = updateUser.EmployeeId.Value;
            }

            if (!string.IsNullOrWhiteSpace(updateUser.Password))
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(updateUser.Password);
                userToUpdate.Password = hashedPassword;
            }

            var savedUser = await _userRepository.UpdateUser(userToUpdate);
            _logger.LogInformation("User with ID {UserId} updated successfully.", id);

            var dto = MapToDto(savedUser);

            return dto;

        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            _logger.LogInformation("Authenticating user with username {Username}.", username);

            var user = await _userRepository.GetUserByUsername(username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                _logger.LogWarning("Authentication failed for username {Username}.", username);
                return null;
            }

            _logger.LogInformation("User {Username} authenticated successfully.", username);
            return user;
        }

        private UserReadDto MapToDto(User u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            EmployeeId = u.EmployeeId
        };

        private async Task checkUserPermissions(int id, ClaimsPrincipal user, string message)
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

            var userRole = user.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && userRole != "HR" && userId != id)
            {
                _logger.LogWarning("Unauthorized access attempt by user ID {UserId} to {Action} user ID {TargetUserId}.", userId, message, id);
                throw new UnauthorizedAccessException($"You are not authorized to {message} this user's data.");
            }
        }
    }
}
