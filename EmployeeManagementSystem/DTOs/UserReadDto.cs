using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.DTOs
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int? EmployeeId { get; set; }
    }
}
