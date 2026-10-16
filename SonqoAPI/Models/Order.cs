using SonqoAPI.Models.Enum;

namespace SonqoAPI.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Relación con la Mesa
        public int TableId { get; set; }
        public Table? Table { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // Total a pagar por este pedido
        public decimal TotalAmount { get; set; }
        public int RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Relación: Un pedido tiene muchos productos (detalles)
        public ICollection<OrderDetail> OrderDetails { get; set; } = [];
        public ICollection<Payment> Payments { get; set; } = [];
    }
}
