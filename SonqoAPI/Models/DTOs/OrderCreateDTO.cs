namespace SonqoAPI.Models.DTOs
{
    public class OrderCreateDTO
    {
        public int TableId { get; set; }
        public List<OrderDetailCreateDTO> Items { get; set; } = [];
    }

    public class OrderDetailCreateDTO
    {
        public int DishId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}