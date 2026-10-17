using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models.DTOs
{
    public class DishUpdateDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MinLength(2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Price { get; set; }
    }

    public class RecipeItemDTO
    {
        [Range(1, int.MaxValue)]
        public int IngredientId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public decimal TotalQuantity { get; set; }

        [Required]
        public string Unit { get; set; } = string.Empty;
    }

    public class AssignRecipeDTO
    {
        public int DishId { get; set; }
        [Range(1, 100)]
        public int YieldPortions { get; set; }
        public List<RecipeItemDTO> Items { get; set; } = [];
    }

    public class DishResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // CAMPOS CALCULADOS
        public decimal RecipeCost { get; set; }
        public decimal ProfitMargin { get; set; }
        public int YieldPortions { get; set; }
        public int MaxAvailable { get; set; }
    }

    public class ShoppingListItemDTO
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal RequiredStock { get; set; }
        public decimal MissingQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
    }
}