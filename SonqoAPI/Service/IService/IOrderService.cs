using SonqoAPI.Models.DTOs;

namespace SonqoAPI.Service.IService
{
    public interface IOrderService
    {
        Task<bool> CreateOrderAsync(OrderCreateDTO orderDto, int restaurantId);
        Task<bool> UpdateOrderDetailStatusAsync(int detailId, string status, string userRole, int restaurantId);
        Task<IEnumerable<KitchenOrderDTO>> GetKitchenOrdersAsync(int restaurantId);
        Task<ActiveOrderDTO?> GetActiveOrderAsync(int tableId, int restaurantId);
        Task<bool> PayOrderAsync(int orderId, PayOrderDTO paymentDto, int restaurantId);
        Task<DashboardDTO> GetDashboardMetricsAsync(int restaurantId);
    }
}