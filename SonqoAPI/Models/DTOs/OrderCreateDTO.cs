using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models.DTOs
{
    public class OrderCreateDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la mesa es inválido.")]
        public int TableId { get; set; }

        [MinLength(1, ErrorMessage = "Debe haber al menos un plato en el pedido.")]
        public List<OrderDetailCreateDTO> Items { get; set; } = [];
    }

    public class OrderDetailCreateDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "El ID del plato es inválido.")]
        public int DishId { get; set; }

        [Range(1, 100, ErrorMessage = "La cantidad debe ser entre 1 y 100.")]
        public int Quantity { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}