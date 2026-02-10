namespace AfghanPay.API.Models
{
    public class AdminUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; }= string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin"; // e.g., "Admin", "SuperAdmin"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
