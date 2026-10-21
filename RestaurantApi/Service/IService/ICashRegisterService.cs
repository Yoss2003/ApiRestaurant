using RestaurantApi.Models;
using RestaurantApi.Models.DTOs;
using System.Threading.Tasks;

namespace RestaurantApi.Service.IService
{
    public interface ICashRegisterService
    {
        Task<IEnumerable<CashRegister>> GetHistoryAsync(int restaurantId);
        Task<CashRegister?> GetOpenRegisterAsync(int restaurantId);
        Task<OpenRegisterInfoDTO?> GetOpenRegisterInfoAsync(int restaurantId);
        Task<bool> UpdateMovementReasonAsync(int movementId, string newReason, int restaurantId);
        Task<bool> OpenRegisterAsync(int userId, decimal openingBalance, int restaurantId);
        Task<bool> CloseRegisterAsync(int userId, decimal actualBalance, string? notes, int restaurantId);
        Task<bool> RegisterMovementAsync(int userId, int restaurantId, CashMovementCreateDTO dto);
    }
}