using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models
{
    public class Restaurant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Ej: "Sonqo - Sede Centro"

        [MaxLength(20)]
        public string Ruc { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<User> Users { get; set; } = [];
        public ICollection<Table> Tables { get; set; } = [];
        public ICollection<Dish> Dishes { get; set; } = [];
        public ICollection<Ingredient> Ingredients { get; set; } = [];
        public ICollection<Order> Orders { get; set; } = [];
        public ICollection<CashRegister> CashRegisters { get; set; } = [];
    }
}