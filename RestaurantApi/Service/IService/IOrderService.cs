using RestaurantApi.Models.DTOs;

namespace RestaurantApi.Service.IService
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(OrderCreateDTO orderDto, int? jwtRestaurantId = null);
        Task<bool> UpdateOrderDetailStatusAsync(int detailId, string status, string userRole, int restaurantId);
        Task<IEnumerable<KitchenOrderDTO>> GetKitchenOrdersAsync(int restaurantId);
        Task<ActiveOrderDTO?> GetActiveOrderAsync(int tableId, int restaurantId);
        Task<(bool Success, string ErrorMessage)> PayOrderAsync(int orderId, PayOrderDTO paymentData, int restaurantId, int userId);
        Task<DashboardDTO> GetDashboardMetricsAsync(int restaurantId);
    }
}