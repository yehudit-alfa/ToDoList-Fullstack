// AuthDtos.cs
using System.ComponentModel.DataAnnotations;

namespace TodoApi
{
    // משמש לרישום משתמש חדש
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // משמש להתחברות משתמש קיים
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}