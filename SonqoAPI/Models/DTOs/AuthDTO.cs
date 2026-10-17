using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models.DTOs
{
    public class RegisterDTO
    {
        [Required]
        [MinLength(4, ErrorMessage = "Mínimo 4 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int Role { get; set; }

        public int? RestaurantId { get; set; }
    }

    public class LoginDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}