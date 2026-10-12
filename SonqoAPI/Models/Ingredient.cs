using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonqoAPI.Models
{
    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "Insumo";

        [Required]
        [MaxLength(20)]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentStock { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MinimumStock { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal UnitCost { get; set; }

        public bool IsPerishable { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime? ExpirationDate { get; set; }
        public ICollection<RecipeDetails>? Recipe { get; set; }
    }
}