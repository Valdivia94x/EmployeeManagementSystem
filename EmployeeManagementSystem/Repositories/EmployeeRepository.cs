using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementSystem.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly EmployeeContext _employeeContext;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(EmployeeContext employeeContext, ILogger<EmployeeRepository> logger)
        {
            _employeeContext = employeeContext;
            _logger = logger;
        }

        public async Task<List<Employee>> GetAllEmployees()
        {
            try
            {
                List<Employee> employees = await _employeeContext.Employees
                    .Include(e => e.Role)
                    .AsNoTracking()
                    .ToListAsync();

                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetAllEmployees.");
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeById(int id)
        {
            try
            {
                Employee? employee = await _employeeContext.Employees
                    .Include(e => e.Role)
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == id);

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetEmployeeById({EmployeeId})", id);
                throw;
            }
        }

        public async Task<Employee> CreateEmployee(Employee employee)
        {
            try
            {
                _employeeContext.Employees.Add(employee);
                await _employeeContext.SaveChangesAsync();

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in CreateEmployee.");
                throw;
            }
        }

        public async Task DeleteEmployee(Employee employee)
        {
            try
            {
                _employeeContext.Employees.Remove(employee);
                await _employeeContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in DeleteEmployee with ID {EmployeeId}.", employee.Id);
                throw;
            }
        }

        public async Task<Employee> UpdateEmployee(Employee employee)
        {
            try
            {
                await _employeeContext.SaveChangesAsync();

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in UpdateEmployee with ID {EmployeeId}.", employee.Id);
                throw;
            }
        }
    }
}
