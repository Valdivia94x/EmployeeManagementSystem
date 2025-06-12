using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories;
using EmployeeManagementSystem.Repositories.Interfaces;
using EmployeeManagementSystem.Services.Interfaces;

namespace EmployeeManagementSystem.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(IEmployeeRepository employeeRepository, IRoleRepository roleRepository, IUserRepository userRepository, ILogger<EmployeeService> logger)
        {
            _employeeRepository = employeeRepository;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<List<EmployeeReadDto>> GetAllEmployees()
        {
            var employees = await _employeeRepository.GetAllEmployees();
            var employeeDtos = employees.Select(MapToDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} employees.", employeeDtos.Count);

            return employeeDtos;
        }

        public async Task<EmployeeReadDto> GetEmployeeById(int id, ClaimsPrincipal user)
        {
            await checkUserPermissions(id, user, "view");

            var employee = await _employeeRepository.GetEmployeeById(id);

            if (employee == null)
            {
                return null;
            }

            var dto = MapToDto(employee);
            return dto;
        }

        public async Task<EmployeeReadDto> CreateEmployee(EmployeeCreateDto employeeDto)
        {
            var role = await _roleRepository.GetRoleById(employeeDto.RoleId);
            if (role == null)
            {
                return null;
            }

            if (!IsValidEmployeeInput(employeeDto, out var validationError))
            {
                _logger.LogWarning("Invalid employee input: {Error}", validationError);
                throw new ArgumentException(validationError);
            }

            Employee employee = new Employee
            {
                FirstName = employeeDto.FirstName,
                LastName = employeeDto.LastName,
                Age = employeeDto.Age,
                Phone = employeeDto.Phone,
                Email = employeeDto.Email,
                Position = employeeDto.Position,
                DateOfHire = employeeDto.DateOfHire,
                RoleId = employeeDto.RoleId
            };

            var createdEmployee = await _employeeRepository.CreateEmployee(employee);
            _logger.LogInformation("Successfully created employee with ID {Id}.", createdEmployee.Id);

            var employeeWithRole = await _employeeRepository.GetEmployeeById(createdEmployee.Id);
            var employeeCreateDto = MapToDto(employeeWithRole);

            return employeeCreateDto;
        }

        public async Task<(bool success, string Error)> DeleteEmployeeById(int id)
        {
            var employee = await _employeeRepository.GetEmployeeById(id);
            if (employee == null)
            {
                return (false, "EmployeeNotFound");
            }

            if (employee.User != null)
            {
                return (false, "EmployeeWithUser"); 
            }

            await _employeeRepository.DeleteEmployee(employee);
            _logger.LogInformation("Employee with ID {EmployeeId} deleted successfully.", employee.Id);

            return (true, null);
        }

        public async Task<(bool Success, string Error, EmployeeReadDto employeeReadDto)> UpdateEmployee(int id, EmployeeUpdateDto updateEmployee, ClaimsPrincipal user)
        {
            await checkUserPermissions(id, user, "update");

            var employee = await _employeeRepository.GetEmployeeById(id);
            if (employee == null)
            {
                return (false, $"EmployeeNotFound", null);
            }

            if (updateEmployee.RoleId.HasValue)
            {
                var role = await _roleRepository.GetRoleById(updateEmployee.RoleId.Value);
                if (role == null)
                {
                    return (false, $"RoleNotFound", null);
                } 
            }

            if (!IsValidEmployeeUpdateInput(updateEmployee, out var validationError))
            {
                _logger.LogWarning("Invalid employee input: {Error}", validationError);
                throw new ArgumentException(validationError);
            }

            MapUpdate(employee, updateEmployee);

            var savedEmployee = await _employeeRepository.UpdateEmployee(employee);
            _logger.LogInformation("Successfully updated employee with ID {Id}", id);

            var dto = MapToDto(savedEmployee);
            return (true, null, dto);
        }

        private EmployeeReadDto MapToDto(Employee e) => new()
        {
            Id = e.Id,
            FullName = $"{e.FirstName} {e.LastName}",
            Age = e.Age,
            Phone = e.Phone,
            Email = e.Email,
            Position = e.Position,
            DateOfHire = e.DateOfHire.ToString("yyyy-MM-dd"),
            RoleName = e.Role?.Name
        };

        private void MapUpdate(Employee employee, EmployeeUpdateDto dto)
        {
            if (dto.FirstName != null) employee.FirstName = dto.FirstName;
            if (dto.LastName != null) employee.LastName = dto.LastName;
            if (dto.Age.HasValue) employee.Age = dto.Age.Value;
            if (dto.Phone != null) employee.Phone = dto.Phone;
            if (dto.Email != null) employee.Email = dto.Email;
            if (dto.Position != null) employee.Position = dto.Position;
            if (dto.DateOfHire.HasValue) employee.DateOfHire = dto.DateOfHire.Value;
            if (dto.RoleId.HasValue) employee.RoleId = dto.RoleId.Value;
        }

        private async Task checkUserPermissions(int id, ClaimsPrincipal user, string message)
        {
            var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
            _logger.LogDebug("Checking permissions for user ID {UserId} on employee ID {EmployeeId}.", userId, id);

            var userMatched = await _userRepository.GetUserById(userId);
            if (userMatched == null)
            {
                _logger.LogError("User with ID {UserId} not found.", userId);
                throw new UnauthorizedAccessException("User not found.");
            }

            var employeeId = userMatched.EmployeeId;
            var userRole = user.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && userRole != "HR" && employeeId != id)
            {
                _logger.LogWarning("Unauthorized attempt by user ID {UserId} to {Action} data of employee ID {EmployeeId}.", userId, message, id);
                throw new UnauthorizedAccessException($"You are not authorized to {message} this employee's data.");
            }
        }

        private bool IsValidEmployeeInput(EmployeeCreateDto dto, out string error)
        {
            error = null;

            if (dto.Age < 18)
            {
                error = "Age must be more than 18.";
                return false;
            }

            if (!Regex.IsMatch(dto.Phone, @"^\d{10}$"))
            {
                error = "Phone number must be exactly 10 digits.";
                return false;
            }

            if (!Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                error = "Invalid email format.";
                return false;
            }

            return true;
        }

        private bool IsValidEmployeeUpdateInput(EmployeeUpdateDto dto, out string error)
        {
            error = null;

            if (dto.Age.HasValue && (dto.Age < 18 || dto.Age > 65))
            {
                error = "Age must be between 18 and 65.";
                return false;
            }

            if (!string.IsNullOrEmpty(dto.Phone) && !Regex.IsMatch(dto.Phone, @"^\d{10}$"))
            {
                error = "Phone number must be exactly 10 digits.";
                return false;
            }

            if (!string.IsNullOrEmpty(dto.Email) && !Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                error = "Invalid email format.";
                return false;
            }

            return true;
        }
    }
}
