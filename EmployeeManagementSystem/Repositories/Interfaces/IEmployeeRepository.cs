using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Repositories.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllEmployees();
        Task<Employee?> GetEmployeeById(int id);
        Task<Employee> CreateEmployee(Employee employee);
        Task DeleteEmployee(Employee employee);
        Task<Employee> UpdateEmployee(Employee employee);
    }
}
