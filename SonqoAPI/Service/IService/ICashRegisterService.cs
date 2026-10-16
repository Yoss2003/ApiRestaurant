using SonqoAPI.Models;
using System.Threading.Tasks;

namespace SonqoAPI.Service.IService
{
    public interface ICashRegisterService
    {
        Task<CashRegister?> GetOpenRegisterAsync(int restaurantId);
        Task<bool> OpenRegisterAsync(int userId, decimal openingBalance, int restaurantId);
        Task<bool> CloseRegisterAsync(int userId, decimal actualBalance, string? notes, int restaurantId);
    }
}