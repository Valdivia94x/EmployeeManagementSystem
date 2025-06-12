using EmployeeManagementSystem.Data;
using EmployeeManagementSystem.Models;
using EmployeeManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EmployeeManagementSystem.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly EmployeeContext _employeeContext;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(EmployeeContext employeeContext, ILogger<UserRepository> logger)
        {
            _employeeContext = employeeContext;
            _logger = logger;
        }

        public async Task<List<User>> GetAllUsers()
        {
            try
            {
                List<User> users = await _employeeContext.Users.ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetAllUsers.");
                throw;
            }
        }

        public async Task<User?> GetUserById(int id)
        {
            try
            {
                User? user = await _employeeContext.Users.FirstOrDefaultAsync(x => x.Id == id);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetUserById({UserId})", id);
                throw;
            }
        }

        public async Task<User> GetUserByUsername(string username)
        {
            try
            {
                var user = await _employeeContext.Users.Include(u => u.Employee)
                    .ThenInclude(e => e.Role).FirstOrDefaultAsync(u => u.Username == username);

                if(user == null)
                {
                    _logger.LogWarning("User with username {Username} not found.", username);
                }
                else
                {
                    _logger.LogInformation("User with username {Username} fetched successfully.", username);
                }
                    return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in GetUSerByUsername({Username})", username);
                throw;
            }
        }

        public async Task<User> CreateUser(User user)
        {
            try
            {
                _employeeContext.Users.Add(user);
                await _employeeContext.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in CreateUser.");
                throw;
            }
        }

        public async Task DeleteUser(User user)
        {
            try
            {
                _employeeContext.Users.Remove(user);
                await _employeeContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in DeleteUser{UserId}.", user.Id);
                throw;
            }
        }

        public async Task<User> UpdateUser(User user)
        {
            try
            {
                await _employeeContext.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error in UpdateUser with ID {UserId}.", user.Id);
                throw;
            }
        }

        public async Task<User?> GetByEmployeeId(int employeeId)
        {
            try
            {
                var user = await _employeeContext.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

                if (user == null)
                {
                    _logger.LogWarning("User with employee ID {EmployeeId} not found.", employeeId);
                }
                else
                {
                    _logger.LogInformation("User with employee ID {EmployeeId} fetched successfully.", employeeId);
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user with employee ID {EmployeeId}.", employeeId);
                throw;
            }
        }

    }
}
