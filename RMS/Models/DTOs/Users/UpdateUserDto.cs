namespace RMS.Models.DTOs.Users
{
    public class UpdateUserDto
    {
        public string FullName { get; set; } = string.Empty;

        
        public string? Password { get; set; }

        
        public string Role { get; set; } = "Waiter";
    }
}
