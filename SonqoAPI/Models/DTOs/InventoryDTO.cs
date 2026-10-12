namespace SonqoAPI.Models.DTOs
{
    public class IngredientCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Insumo";
        public string BaseUnit { get; set; } = string.Empty;
        public decimal InitialStock { get; set; }
        public decimal InitialCost { get; set; }
        public decimal MinimumStock { get; set; }
        public bool IsPerishable { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class RestockDTO
    {
        public int IngredientId { get; set; }
        public decimal QuantityAdded { get; set; }
        public decimal TotalCostPaid { get; set; }
        public DateTime? NewExpirationDate { get; set; }
    }
}