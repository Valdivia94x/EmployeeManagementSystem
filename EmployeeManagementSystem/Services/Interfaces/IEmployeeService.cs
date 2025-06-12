using System.Security.Claims;
using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<EmployeeReadDto>> GetAllEmployees();
        Task<EmployeeReadDto> GetEmployeeById(int id, ClaimsPrincipal user);
        Task<EmployeeReadDto> CreateEmployee(EmployeeCreateDto employeeDto);
        Task<(bool success, string Error)> DeleteEmployeeById(int id);
        Task<(bool Success, string Error, EmployeeReadDto employeeReadDto)> UpdateEmployee(int id, EmployeeUpdateDto updateEmployee, ClaimsPrincipal user);
    }
}
