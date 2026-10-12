namespace SonqoAPI.Models
{
    public class Table
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Relación: Una mesa puede tener un pedido activo (o historial de pedidos)
        public ICollection<Order> Orders { get; set; } = [];
    }
}
