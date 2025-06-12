using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllUsers();
        Task<User?> GetUserById(int id);
        Task<User> GetUserByUsername(string username);
        Task<User> CreateUser(User user);
        Task DeleteUser(User user);
        Task<User> UpdateUser(User user);
        Task<User?> GetByEmployeeId(int employeeId);
    }
}
