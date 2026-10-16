using Microsoft.EntityFrameworkCore;
using SonqoAPI.Models;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Service
{
    public class CashRegisterService(
        IGenericRepository<CashRegister> cashRepository,
        IGenericRepository<Payment> paymentRepository
        ) : ICashRegisterService
    {
        public async Task<CashRegister?> GetOpenRegisterAsync(int restaurantId)
        {
            return await cashRepository.GetQueryable()
                .FirstOrDefaultAsync(c => c.IsOpen && c.RestaurantId == restaurantId);
        }

        public async Task<bool> OpenRegisterAsync(int userId, decimal openingBalance, int restaurantId)
        {
            var currentOpen = await GetOpenRegisterAsync(restaurantId);
            if (currentOpen != null) return false;

            var newRegister = new CashRegister
            {
                UserId = userId,
                OpeningBalance = openingBalance,
                OpenedAt = DateTime.UtcNow,
                IsOpen = true,
                RestaurantId = restaurantId
            };

            await cashRepository.AddAsync(newRegister);
            await cashRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CloseRegisterAsync(int userId, decimal actualBalance, string? notes, int restaurantId)
        {
            var currentOpen = await GetOpenRegisterAsync(restaurantId);
            if (currentOpen == null) return false;

            var cashPaymentsTotal = await paymentRepository.GetQueryable()
                .Where(p => p.PaymentDate >= currentOpen.OpenedAt &&
                            p.PaymentMethodId == 1 &&
                            p.Order!.RestaurantId == restaurantId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            decimal expectedCashBalance = currentOpen.OpeningBalance + cashPaymentsTotal;

            currentOpen.ClosingBalance = actualBalance;
            currentOpen.Discrepancy = actualBalance - expectedCashBalance;
            currentOpen.ClosedAt = DateTime.UtcNow;
            currentOpen.IsOpen = false;
            currentOpen.Notes = notes;

            cashRepository.Update(currentOpen);
            await cashRepository.SaveChangesAsync();
            return true;
        }
    }
}