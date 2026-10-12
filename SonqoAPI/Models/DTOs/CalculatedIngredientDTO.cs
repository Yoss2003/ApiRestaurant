namespace SonqoAPI.Models.DTOs
{
    public class CalculatedIngredientDTO
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BaseUnitOfMeasure { get; set; } = string.Empty;
        public decimal TotalQuantityRequired { get; set; }
        public decimal CurrentStock { get; set; }
        public bool SufficientStock { get; set; }
    }
}
