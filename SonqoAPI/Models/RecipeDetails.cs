using System.ComponentModel.DataAnnotations.Schema;

namespace SonqoAPI.Models
{
    public class RecipeDetails
    {
        public int DishId { get; set; }
        public Dish? Dish { get; set; }
        public int IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal RequiredQuantity { get; set; }
    }
}
