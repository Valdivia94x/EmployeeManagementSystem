using System.Security.Claims;
using System.Threading.Tasks;
using EmployeeManagementSystem.DTOs;
using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserReadDto>> GetAllUsers();
        Task<UserReadDto> GetUserId(int id, ClaimsPrincipal user);
        Task<UserReadDto> CreateUser(UserCreateDto userDto);
        Task<bool> DeleteUserById(int id);
        Task<UserReadDto> UpdateUser(int id, UserUpdateDto updateUser, ClaimsPrincipal user);
        Task<User> AuthenticateAsync(string username, string password);
    }
}
