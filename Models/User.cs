using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string Role { get; set; } = "User";
    }
}
