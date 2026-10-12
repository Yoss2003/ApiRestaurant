namespace SonqoAPI.Models.DTOs
{
    public class ActiveOrderDTO
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<ActiveOrderDetailDTO> Items { get; set; } = [];
    }

    public class ActiveOrderDetailDTO
    {
        public int DetailId { get; set; }
        public int Quantity { get; set; }
        public string DishName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public bool IsAdicional { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}