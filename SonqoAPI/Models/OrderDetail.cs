namespace SonqoAPI.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int DishId { get; set; }
        public Dish? Dish { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public string? Notes { get; set; } = string.Empty;

        public bool IsAdicional { get; set; } = false;
        public string Status { get; set; } = "Pending";
    }
}