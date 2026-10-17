using SonqoAPI.Models.Enum;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models
{
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public Role Role { get; set; }

        public bool IsActive { get; set; } = true;

        public int? RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }
    }
}