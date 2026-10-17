using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models.DTOs
{
    public class IngredientCreateDTO
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Insumo";
        [Required]
        public string BaseUnit { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal InitialStock { get; set; }
        [Range(0, double.MaxValue)]
        public decimal InitialCost { get; set; }
        [Range(0, double.MaxValue)]
        public decimal MinimumStock { get; set; }

        public bool IsPerishable { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class RestockDTO
    {
        public int IngredientId { get; set; }
        [Range(0.001, double.MaxValue, ErrorMessage = "Debe agregar una cantidad mayor a 0.")]
        public decimal QuantityAdded { get; set; }
        [Range(0, double.MaxValue)]
        public decimal TotalCostPaid { get; set; }
        public DateTime? NewExpirationDate { get; set; }
    }
}