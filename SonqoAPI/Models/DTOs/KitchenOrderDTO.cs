namespace SonqoAPI.Models.DTOs
{
    public class KitchenOrderDTO
    {
        public int OrderId { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public DateTime OrderTime { get; set; }
        public List<KitchenOrderDetailDTO> Details { get; set; } = [];
    }

    public class KitchenOrderDetailDTO
    {
        public int DetailId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsAdicional { get; set; }
    }
}