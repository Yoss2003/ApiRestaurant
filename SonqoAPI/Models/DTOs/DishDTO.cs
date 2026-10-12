namespace SonqoAPI.Models.DTOs
{
    public class DishUpdateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class RecipeItemDTO
    {
        public int IngredientId { get; set; }
        public decimal TotalQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class AssignRecipeDTO
    {
        public int DishId { get; set; }
        public int YieldPortions { get; set; }
        public List<RecipeItemDTO> Items { get; set; } = [];
    }

    // NUEVO: Lo que le enviaremos a Angular
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