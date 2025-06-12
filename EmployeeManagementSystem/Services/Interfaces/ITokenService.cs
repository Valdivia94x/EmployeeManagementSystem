using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(int userId, string username, string role);
    }
}
