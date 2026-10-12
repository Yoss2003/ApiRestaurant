namespace SonqoAPI.Models.DTOs
{
    public class DashboardDTO
    {
        public decimal TotalSales { get; set; }
        public int TablesServed { get; set; }
        public decimal AverageTicket { get; set; }

        public List<PaymentMethodMetric> SalesByPaymentMethod { get; set; } = [];
        public List<TopDishMetric> TopDishes { get; set; } = [];
    }

    public class PaymentMethodMetric
    {
        public string MethodName { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class TopDishMetric
    {
        public string DishName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}