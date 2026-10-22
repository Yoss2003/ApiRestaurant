namespace RestaurantApi.Models
{
    public class Dish
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public int YieldPortions { get; set; } = 1;
        public int RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }
        public bool IsFeaturedGeneral { get; set; } = false;
        public bool IsTopWeekly { get; set; } = false;
        public bool IsTopMonthly { get; set; } = false;
        public int? DailyStock { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<RecipeDetails> Recipe { get; set; } = [];
    }
}
