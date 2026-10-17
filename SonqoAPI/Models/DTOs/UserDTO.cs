using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models.DTOs
{
    // DTO para la respuesta (lo que enviamos a Angular)
    public class UserResponseDTO
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
    }

    // DTO para crear un usuario nuevo
    public class UserCreateDTO
    {
        [Required]
        [MinLength(4, ErrorMessage = "El nombre de usuario debe tener al menos 4 caracteres.")]
        public string? Username { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string? Password { get; set; }

        [Required]
        public string? Role { get; set; }
    }

    // DTO para actualizar un usuario existente
    public class UserUpdateDTO
    {
        [Required]
        [MinLength(4, ErrorMessage = "El nombre de usuario debe tener al menos 4 caracteres.")]
        public string? Username { get; set; }

        public string? Role { get; set; }

        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string? Password { get; set; }
    }
    public class RestaurantSettingsDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}