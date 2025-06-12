namespace EmployeeManagementSystem.DTOs
{
    public class EmployeeCreateDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }
        public DateTime DateOfHire { get; set; }
        public int RoleId { get; set; }
    }
}
