using Microsoft.EntityFrameworkCore;
using RestaurantApi.Models;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Repositories.IRepositories;
using RestaurantApi.Service.IService;

namespace RestaurantApi.Service
{
    public class CashRegisterService(
        IGenericRepository<CashRegister> cashRepository,
        IGenericRepository<Payment> paymentRepository,
        IGenericRepository<OrderDetail> orderDetailRepository,
        IGenericRepository<DailySalesSummary> dailySalesRepository,
        IGenericRepository<Dish> dishRepository,
        IGenericRepository<CashMovement> movementRepository
        ) : ICashRegisterService
    {
        public async Task<IEnumerable<CashRegister>> GetHistoryAsync(int restaurantId)
        {
            return await cashRepository.GetQueryable()
                .Where(c => c.RestaurantId == restaurantId && !c.IsOpen)
                .OrderByDescending(c => c.ClosedAt)
                .Take(10)
                .ToListAsync();
        }
        public async Task<CashRegister?> GetOpenRegisterAsync(int restaurantId)
        {
            return await cashRepository.GetQueryable()
                .FirstOrDefaultAsync(c => c.IsOpen && c.RestaurantId == restaurantId);
        }

        public async Task<OpenRegisterInfoDTO?> GetOpenRegisterInfoAsync(int restaurantId)
        {
            var currentOpen = await cashRepository.GetQueryable()
                .FirstOrDefaultAsync(c => c.IsOpen && c.RestaurantId == restaurantId);

            if (currentOpen == null) return null;

            // Sumamos cobros en efectivo (Id de método de pago = 1)
            var cashPaymentsTotal = await paymentRepository.GetQueryable()
                .Where(p => p.CashRegisterId == currentOpen.Id && p.PaymentMethodId == 1)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // Obtenemos todos los movimientos de esta caja
            var movements = await movementRepository.GetQueryable()
                .Include(m => m.PaymentMethod)
                .Where(m => m.CashRegisterId == currentOpen.Id)
                .OrderByDescending(m => m.MovementDate)
                .ToListAsync();

            decimal cashIn = movements.Where(m => m.Type == "IN" && m.PaymentMethodId == 1).Sum(m => m.Amount);
            decimal cashOut = movements.Where(m => m.Type == "OUT" && m.PaymentMethodId == 1).Sum(m => m.Amount);
            decimal expectedCash = currentOpen.OpeningBalance + cashPaymentsTotal + cashIn - cashOut;

            var paymentBreakdown = await paymentRepository.GetQueryable()
                .Include(p => p.PaymentMethod)
                .Where(p => p.CashRegisterId == currentOpen.Id)
                .GroupBy(p => p.PaymentMethod!.Name)
                .Select(g => new
                {
                    Method = g.Key,
                    Total = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            return new OpenRegisterInfoDTO
            {
                RegisterId = currentOpen.Id,
                UserId = currentOpen.UserId,
                OpeningBalance = currentOpen.OpeningBalance,
                OpenedAt = currentOpen.OpenedAt,
                ExpectedCashBalance = expectedCash,
                Movements = movements.Select(m => new
                {
                    m.Id,
                    m.Type,
                    m.Amount,
                    m.Reason,
                    m.MovementDate,
                    Method = m.PaymentMethod?.Name ?? "Efectivo"
                }).ToList(),
                PaymentBreakdown = paymentBreakdown
            };
        }

        public async Task<bool> UpdateMovementReasonAsync(int movementId, string newReason, int restaurantId)
        {
            var movement = await movementRepository.GetQueryable()
                .Include(m => m.CashRegister)
                .FirstOrDefaultAsync(m => m.Id == movementId && m.CashRegister!.RestaurantId == restaurantId);

            if (movement == null || movement.CashRegister == null || !movement.CashRegister.IsOpen)
                return false;

            movement.Reason = newReason;
            movementRepository.Update(movement);
            await movementRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> OpenRegisterAsync(int userId, decimal openingBalance, int restaurantId)
        {
            using var transaction = await cashRepository.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var currentOpen = await GetOpenRegisterAsync(restaurantId);
                if (currentOpen != null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

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
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CloseRegisterAsync(int userId, decimal actualBalance, string? notes, int restaurantId)
        {
            var currentOpen = await GetOpenRegisterAsync(restaurantId);
            if (currentOpen == null) return false;

            var cashPaymentsTotal = await paymentRepository.GetQueryable()
                .Where(p => p.CashRegisterId == currentOpen.Id && p.PaymentMethodId == 1)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
            
            var movements = await movementRepository.GetQueryable()
                .Where(m => m.CashRegisterId == currentOpen.Id)
                .ToListAsync();

            decimal cashIn = movements.Where(m => m.Type == "IN" && m.PaymentMethodId == 1).Sum(m => m.Amount);
            decimal cashOut = movements.Where(m => m.Type == "OUT" && m.PaymentMethodId == 1).Sum(m => m.Amount);
            decimal expectedCashBalance = currentOpen.OpeningBalance + cashPaymentsTotal + cashIn - cashOut;

            currentOpen.ClosingBalance = actualBalance;
            currentOpen.Discrepancy = actualBalance - expectedCashBalance;
            currentOpen.ClosedAt = DateTime.UtcNow;
            currentOpen.IsOpen = false;
            currentOpen.Notes = notes;

            cashRepository.Update(currentOpen);
            await cashRepository.SaveChangesAsync();

            // EJECUTAMOS EL CÁLCULO DE PLATOS AL CERRAR CAJA
            await ProcessDailySalesAndTopDishesAsync(restaurantId);

            return true;
        }

        private async Task ProcessDailySalesAndTopDishesAsync(int restaurantId)
        {
            var today = DateTime.UtcNow.Date;

            // 1. Calcular las ventas del día leyendo los OrderDetails
            var todaysSales = await orderDetailRepository.GetQueryable()
                .Include(od => od.Order)
                .Where(od => od.Order!.RestaurantId == restaurantId
                          && od.Order.Status == Models.Enum.OrderStatus.Paid
                          && od.Order.OrderDate.Date == today)
                .GroupBy(od => od.DishId)
                .Select(g => new
                {
                    DishId = g.Key,
                    TotalQuantity = g.Sum(od => od.Quantity)
                })
                .ToListAsync();

            // 2. Guardar o actualizar en la tabla resumen_ventas_diarias
            foreach (var sale in todaysSales)
            {
                var existingSummary = await dailySalesRepository.GetQueryable()
                    .FirstOrDefaultAsync(d => d.RestaurantId == restaurantId && d.DishId == sale.DishId && d.Date == today);

                if (existingSummary != null)
                {
                    existingSummary.TotalQuantitySold = sale.TotalQuantity;
                    dailySalesRepository.Update(existingSummary);
                }
                else
                {
                    await dailySalesRepository.AddAsync(new DailySalesSummary
                    {
                        RestaurantId = restaurantId,
                        DishId = sale.DishId,
                        Date = today,
                        TotalQuantitySold = sale.TotalQuantity
                    });
                }
            }
            await dailySalesRepository.SaveChangesAsync();

            // 3. Calcular el ganador de la SEMANA (Estrella Azul)
            var weekAgo = today.AddDays(-7);
            var weeklyTopDishId = await dailySalesRepository.GetQueryable()
                .Include(d => d.Dish)
                .Where(d => d.RestaurantId == restaurantId
                         && d.Date >= weekAgo
                         && !d.Dish!.IsFeaturedGeneral)
                .GroupBy(d => d.DishId)
                .Select(g => new { DishId = g.Key, Total = g.Sum(d => d.TotalQuantitySold) })
                .OrderByDescending(x => x.Total)
                .Select(x => x.DishId)
                .FirstOrDefaultAsync();

            // 4. Calcular el ganador del MES (Estrella Rosada)
            var monthAgo = today.AddDays(-30);
            var monthlyTopDishId = await dailySalesRepository.GetQueryable()
                .Include(d => d.Dish)
                .Where(d => d.RestaurantId == restaurantId
                         && d.Date >= monthAgo
                         && !d.Dish!.IsFeaturedGeneral
                         && d.DishId != weeklyTopDishId)
                .GroupBy(d => d.DishId)
                .Select(g => new { DishId = g.Key, Total = g.Sum(d => d.TotalQuantitySold) })
                .OrderByDescending(x => x.Total)
                .Select(x => x.DishId)
                .FirstOrDefaultAsync();

            // 5. Reiniciar las estrellas actuales
            var dishesToReset = await dishRepository.GetQueryable()
                .Where(d => d.RestaurantId == restaurantId && (d.IsTopWeekly || d.IsTopMonthly))
                .ToListAsync();

            foreach (var dish in dishesToReset)
            {
                dish.IsTopWeekly = false;
                dish.IsTopMonthly = false;
                dishRepository.Update(dish);
            }

            // 6. Coronar a los nuevos ganadores
            if (weeklyTopDishId != 0)
            {
                var weeklyDish = await dishRepository.GetByIdAsync(weeklyTopDishId);
                if (weeklyDish != null)
                {
                    weeklyDish.IsTopWeekly = true;
                    dishRepository.Update(weeklyDish);
                }
            }

            if (monthlyTopDishId != 0)
            {
                var monthlyDish = await dishRepository.GetByIdAsync(monthlyTopDishId);
                if (monthlyDish != null)
                {
                    monthlyDish.IsTopMonthly = true;
                    dishRepository.Update(monthlyDish);
                }
            }

            await dishRepository.SaveChangesAsync();
        }

        public async Task<bool> RegisterMovementAsync(int userId, int restaurantId, CashMovementCreateDTO dto)
        {
            var currentOpen = await GetOpenRegisterAsync(restaurantId);
            if (currentOpen == null) return false;

            var movement = new CashMovement
            {
                CashRegisterId = currentOpen.Id,
                UserId = userId,
                Type = dto.Type,
                Amount = dto.Amount,
                Reason = dto.Reason,
                PaymentMethodId = dto.PaymentMethodId,
                MovementDate = DateTime.UtcNow
            };

            await movementRepository.AddAsync(movement);
            await movementRepository.SaveChangesAsync();
            return true;
        }
    }
}