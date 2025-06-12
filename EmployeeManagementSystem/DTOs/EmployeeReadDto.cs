using EmployeeManagementSystem.Models;

namespace EmployeeManagementSystem.DTOs
{
    public class EmployeeReadDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int? Age { get; set; }
        public string? Phone { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public string? DateOfHire{ get; set; }
        public string RoleName { get; set; }
    }
}
