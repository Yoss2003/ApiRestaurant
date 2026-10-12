using SonqoAPI.Models.DTOs;

namespace SonqoAPI.Service.IService
{
    public interface IOrderService
    {
        Task<bool> CreateOrderAsync(OrderCreateDTO orderDto);
        Task<bool> UpdateOrderDetailStatusAsync(int detailId, string status);
        Task<IEnumerable<KitchenOrderDTO>> GetKitchenOrdersAsync();
        Task<ActiveOrderDTO?> GetActiveOrderAsync(int tableId);
        Task<bool> PayOrderAsync(int tableId, PayOrderDTO paymentData);
        Task<DashboardDTO> GetDashboardMetricsAsync();
    }
}