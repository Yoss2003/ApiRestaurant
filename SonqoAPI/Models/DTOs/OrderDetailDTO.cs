namespace SonqoAPI.Models.DTOs
{
    // Representa lo que el cliente selecciona en la interfaz
    public class OrderDetailDTO
    {
        public int DishId { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; } = string.Empty;
    }
}