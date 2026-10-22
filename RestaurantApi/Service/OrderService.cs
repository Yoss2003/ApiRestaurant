using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RestaurantApi.Hubs;
using RestaurantApi.Models;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Models.Enum;
using RestaurantApi.Repositories.IRepositories;
using RestaurantApi.Service.IService;
using System.Data;

namespace RestaurantApi.Service
{
    public class OrderService(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Table> tableRepository,
        IGenericRepository<Dish> dishRepository,
        IGenericRepository<RecipeDetails> recipeRepository,
        IGenericRepository<Ingredient> ingredientRepository,
        IGenericRepository<OrderDetail> orderDetailRepository,
        IGenericRepository<Payment> paymentRepository,
        IGenericRepository<PaymentMethod> paymentMethodRepository,
        IGenericRepository<CashRegister> cashRegisterRepository,
        IHubContext<OrderHub> orderHub
        ) : IOrderService
    {
        public async Task<int> CreateOrderAsync(OrderCreateDTO orderDto, int? jwtRestaurantId = null)
        {
            var table = await tableRepository.GetQueryable()
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.Id == orderDto.TableId);

            if (table == null || table.IsDeleted || table.Restaurant == null || !table.Restaurant.IsActive)
                return 0;

            int actualRestaurantId = table.RestaurantId;

            if (jwtRestaurantId.HasValue && jwtRestaurantId.Value != 0 && jwtRestaurantId.Value != actualRestaurantId)
            {
                return 0;
            }

            var recipes = await recipeRepository.GetAllAsync();
            var today = DateTime.UtcNow.Date;
            var ingredients = await ingredientRepository.GetAllAsync(i =>
                i.RestaurantId == actualRestaurantId &&
                i.IsActive == true &&
                (!i.IsPerishable || i.ExpirationDate == null || i.ExpirationDate >= today)
            );

            var requiredIngredients = new Dictionary<int, decimal>();
            var validDishes = new Dictionary<int, Dish>();

            foreach (var item in orderDto.Items)
            {
                if (item.Quantity <= 0) continue;
                var dish = await dishRepository.GetByIdAsync(item.DishId);
                if (dish == null || !dish.IsAvailable || dish.RestaurantId != actualRestaurantId) continue;

                validDishes[item.DishId] = dish;

                var dishRecipes = recipes.Where(r => r.DishId == dish.Id);
                int portions = dish.YieldPortions > 0 ? dish.YieldPortions : 1;

                foreach (var detail in dishRecipes)
                {
                    decimal requiredPerPlate = detail.RequiredQuantity / portions;
                    decimal totalNeededForThisItem = requiredPerPlate * item.Quantity;

                    if (requiredIngredients.ContainsKey(detail.IngredientId))
                        requiredIngredients[detail.IngredientId] += totalNeededForThisItem;
                    else
                        requiredIngredients[detail.IngredientId] = totalNeededForThisItem;
                }
            }

            var requestedDishIds = orderDto.Items.Select(i => i.DishId).Distinct().Count();
            if (validDishes.Count != requestedDishIds)
            {
                return 0;
            }

            foreach (var kvp in requiredIngredients)
            {
                var ing = ingredients.FirstOrDefault(i => i.Id == kvp.Key);
                if (ing == null || ing.CurrentStock < kvp.Value)
                {
                    return 0;
                }
            }

            var existingOrders = await orderRepository.GetAllAsync(o =>
                o.TableId == orderDto.TableId &&
                o.RestaurantId == actualRestaurantId &&
                o.Status != OrderStatus.Paid &&
                o.Status != OrderStatus.Cancelled);

            var activeOrder = existingOrders.FirstOrDefault();
            int targetOrderId = 0;

            if (activeOrder != null)
            {
                foreach (var item in orderDto.Items)
                {
                    if (item.Quantity <= 0 || !validDishes.ContainsKey(item.DishId)) continue;
                    var dish = validDishes[item.DishId];

                    if (dish.DailyStock.HasValue)
                    {
                        if (dish.DailyStock.Value < item.Quantity)
                        {
                            return 0;
                        }
                        dish.DailyStock -= item.Quantity;
                        dishRepository.Update(dish);
                    }

                    var subTotal = dish.Price * item.Quantity;

                    activeOrder.TotalAmount += subTotal;
                    activeOrder.OrderDetails.Add(new OrderDetail
                    {
                        DishId = dish.Id,
                        Quantity = item.Quantity,
                        UnitPrice = dish.Price,
                        SubTotal = subTotal,
                        Notes = item.Notes,
                        IsAdicional = true,
                        Status = "Pending"
                    });
                    DeductInventory(dish, item.Quantity, recipes, ingredients);

                    if (dish.DailyStock.HasValue)
                    {
                        dish.DailyStock -= item.Quantity;
                        if (dish.DailyStock < 0) dish.DailyStock = 0;
                        dishRepository.Update(dish);
                    }
                }
                orderRepository.Update(activeOrder);

                try
                {
                    await orderRepository.SaveChangesAsync();
                    targetOrderId = activeOrder.Id;
                }
                catch (DbUpdateConcurrencyException) { return 0; }
            }
            else
            {
                var newOrder = new Order
                {
                    TableId = orderDto.TableId,
                    RestaurantId = actualRestaurantId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0
                };

                decimal total = 0;
                foreach (var item in orderDto.Items)
                {
                    if (item.Quantity <= 0 || !validDishes.ContainsKey(item.DishId)) continue;
                    var dish = validDishes[item.DishId];
                    var subTotal = dish.Price * item.Quantity;
                    total += subTotal;

                    var orderDetail = new OrderDetail
                    {
                        DishId = dish.Id,
                        Quantity = item.Quantity,
                        UnitPrice = dish.Price,
                        SubTotal = subTotal,
                        Notes = item.Notes,
                        IsAdicional = false,
                        Status = "Pending"
                    };
                    newOrder.OrderDetails.Add(orderDetail);
                    DeductInventory(dish, item.Quantity, recipes, ingredients);

                    if (dish.DailyStock.HasValue)
                    {
                        dish.DailyStock -= item.Quantity;
                        if (dish.DailyStock < 0) dish.DailyStock = 0;
                        dishRepository.Update(dish);
                    }
                }

                if (newOrder.OrderDetails.Count == 0) return 0;

                newOrder.TotalAmount = total;
                table.IsAvailable = false;
                tableRepository.Update(table);

                await orderRepository.AddAsync(newOrder);

                try
                {
                    await orderRepository.SaveChangesAsync();
                    targetOrderId = newOrder.Id;
                }
                catch (DbUpdateConcurrencyException) { return 0; }
            }

            if (targetOrderId > 0)
            {
                try
                {
                    await orderHub.Clients.Group(actualRestaurantId.ToString()).SendAsync("ReceiveOrderUpdate", new { orderId = targetOrderId, type = "ORDER_CREATED" });
                }
                catch { /* Ignoramos fallos del WebSocket */ }
            }

            return targetOrderId;
        }

        public async Task<bool> UpdateOrderDetailStatusAsync(int detailId, string status, string userRole, int restaurantId)
        {
            var detail = await orderDetailRepository.GetQueryable()
                .Include(d => d.Order)
                .FirstOrDefaultAsync(d => d.Id == detailId);

            if (detail == null || detail.Order == null || detail.Order.RestaurantId != restaurantId) return false;
            if (detail.Order.Status == OrderStatus.Paid || detail.Order.Status == OrderStatus.Cancelled)
                return false;

            bool isValidTransition = false;
            if (userRole == "Kitchen" || userRole == "Admin" || userRole == "SuperAdmin")
            {
                if (detail.Status == "Pending" && status == "Preparing") isValidTransition = true;
                if (detail.Status == "Preparing" && status == "Ready") isValidTransition = true;
            }
            if (userRole == "Waiter" || userRole == "Admin" || userRole == "SuperAdmin")
            {
                if (detail.Status == "Ready" && status == "Delivered") isValidTransition = true;
            }

            if (!isValidTransition) return false;

            detail.Status = status;
            orderDetailRepository.Update(detail);
            await orderDetailRepository.SaveChangesAsync();
            try
            {
                await orderHub.Clients.Group(restaurantId.ToString()).SendAsync("ReceiveOrderUpdate", new { detailId, status });
            }
            catch { /* Ignoramos fallos del WebSocket */ }
            return true;
        }

        public async Task<IEnumerable<KitchenOrderDTO>> GetKitchenOrdersAsync(int restaurantId)
        {
            var activeOrders = await orderRepository.GetAllAsync(o =>
                o.RestaurantId == restaurantId &&
                o.Status != OrderStatus.Paid &&
                o.Status != OrderStatus.Cancelled);

            var allTables = await tableRepository.GetAllAsync(t => t.RestaurantId == restaurantId);
            var allDishes = await dishRepository.GetAllAsync(d => d.RestaurantId == restaurantId);
            var orderIds = activeOrders.Select(o => o.Id).ToList();
            var allDetails = await orderDetailRepository.GetAllAsync(d => orderIds.Contains(d.OrderId));

            var kitchenOrders = new List<KitchenOrderDTO>();
            foreach (var order in activeOrders)
            {
                var table = allTables.FirstOrDefault(t => t.Id == order.TableId);
                var details = allDetails.Where(d => d.OrderId == order.Id).ToList();

                var dto = new KitchenOrderDTO
                {
                    OrderId = order.Id,
                    TableNumber = table?.Number ?? "Mesa ?",
                    OrderTime = order.OrderDate
                };

                foreach (var detail in details)
                {
                    if (detail.Status == "Pending" || detail.Status == "Preparing")
                    {
                        var dish = allDishes.FirstOrDefault(d => d.Id == detail.DishId);
                        dto.Details.Add(new KitchenOrderDetailDTO
                        {
                            DetailId = detail.Id,
                            DishName = dish?.Name ?? "Desconocido",
                            Quantity = detail.Quantity,
                            Notes = detail.Notes ?? string.Empty,
                            Status = detail.Status,
                            IsAdicional = detail.IsAdicional
                        });
                    }
                }
                if (dto.Details.Count != 0) kitchenOrders.Add(dto);
            }
            return kitchenOrders;
        }

        public async Task<ActiveOrderDTO?> GetActiveOrderAsync(int tableId, int restaurantId)
        {
            var activeOrders = await orderRepository.GetAllAsync(o =>
                o.TableId == tableId &&
                o.RestaurantId == restaurantId &&
                o.Status != OrderStatus.Paid &&
                o.Status != OrderStatus.Cancelled);

            var order = activeOrders.FirstOrDefault();
            if (order == null) return null;

            var allDishes = await dishRepository.GetAllAsync(d => d.RestaurantId == restaurantId);
            var allDetails = await orderDetailRepository.GetAllAsync();
            var orderDetails = allDetails.Where(d => d.OrderId == order.Id).ToList();

            var dto = new ActiveOrderDTO
            {
                OrderId = order.Id,
                TotalAmount = order.TotalAmount
            };

            foreach (var item in orderDetails)
            {
                var dish = allDishes.FirstOrDefault(d => d.Id == item.DishId);
                dto.Items.Add(new ActiveOrderDetailDTO
                {
                    DetailId = item.Id,
                    Quantity = item.Quantity,
                    DishName = dish?.Name ?? "Plato eliminado",
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal,
                    IsAdicional = item.IsAdicional,
                    Status = item.Status
                });
            }
            return dto;
        }

        public async Task<(bool Success, string ErrorMessage)> PayOrderAsync(int orderId, PayOrderDTO paymentData, int restaurantId, int userId)
        {
            using var transaction = await orderRepository.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var openRegister = await cashRegisterRepository.GetQueryable()
                    .FirstOrDefaultAsync(c => c.RestaurantId == restaurantId && c.IsOpen);
                if (openRegister == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "No se puede cobrar: La caja chica está cerrada. Abra un turno primero.");
                }

                var order = await orderRepository.GetQueryable()
                    .Include(o => o.OrderDetails)
                    .Include(o => o.Table)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.RestaurantId == restaurantId);

                if (order == null || order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
                {
                    await transaction.RollbackAsync();
                    return (false, "El pedido no existe, o ya fue cobrado/cancelado.");
                }

                var activeMethods = await paymentMethodRepository.GetAllAsync(m => m.IsActive);
                var activeMethodIds = activeMethods.Select(m => m.Id).ToList();
                bool isSplitPayment = paymentData.SplitPayments != null && paymentData.SplitPayments.Any();

                if (isSplitPayment)
                {
                    if (paymentData.SplitPayments!.Any(sp => !activeMethodIds.Contains(sp.PaymentMethodId)))
                    {
                        await transaction.RollbackAsync();
                        return (false, "Uno o más métodos de pago seleccionados no son válidos.");
                    }
                }
                else
                {
                    if (!activeMethodIds.Contains(paymentData.PrimaryPaymentMethodId))
                    {
                        await transaction.RollbackAsync();
                        return (false, "El método de pago seleccionado es inválido.");
                    }
                }

                decimal calculatedTotal = order.OrderDetails.Sum(d => Math.Round(d.SubTotal, 2, MidpointRounding.AwayFromZero));
                order.TotalAmount = calculatedTotal;

                decimal amountAlreadyPaid = order.Payments.Sum(p => Math.Round(p.Amount, 2, MidpointRounding.AwayFromZero));
                decimal remainingBalance = calculatedTotal - amountAlreadyPaid;

                decimal incomingPaymentTotal = isSplitPayment
                    ? paymentData.SplitPayments!.Sum(p => Math.Round(p.Amount, 2, MidpointRounding.AwayFromZero))
                    : Math.Round(paymentData.TotalAmountPaid, 2, MidpointRounding.AwayFromZero);

                if (incomingPaymentTotal != remainingBalance)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Descuadre matemático: El sistema intentó pagar S/ {incomingPaymentTotal} pero la mesa debe S/ {remainingBalance}.");
                }

                if (isSplitPayment)
                {
                    foreach (var split in paymentData.SplitPayments!)
                    {
                        if (split.Amount <= 0) continue;
                        order.Payments.Add(new Payment
                        {
                            OrderId = order.Id,
                            PaymentMethodId = split.PaymentMethodId,
                            Amount = Math.Round(split.Amount, 2, MidpointRounding.AwayFromZero),
                            PaymentDate = DateTime.UtcNow,
                            CashRegisterId = openRegister.Id,
                            UserId = userId
                        });
                    }
                }
                else if (paymentData.TotalAmountPaid > 0)
                {
                    order.Payments.Add(new Payment
                    {
                        OrderId = order.Id,
                        PaymentMethodId = paymentData.PrimaryPaymentMethodId,
                        Amount = Math.Round(paymentData.TotalAmountPaid, 2, MidpointRounding.AwayFromZero),
                        PaymentDate = DateTime.UtcNow,
                        CashRegisterId = openRegister.Id,
                        UserId = userId
                    });
                }

                order.Status = OrderStatus.Paid;
                orderRepository.Update(order);

                if (order.Table != null)
                {
                    order.Table.IsAvailable = true;
                    tableRepository.Update(order.Table);
                }

                await orderRepository.SaveChangesAsync();
                await tableRepository.SaveChangesAsync();
                await transaction.CommitAsync();

                try
                {
                    await orderHub.Clients.Group(restaurantId.ToString()).SendAsync("ReceiveOrderUpdate", new { orderId = order.Id, type = "ORDER_PAID" });
                }
                catch { /* Ignoramos fallos de WebSocket */ }

                return (true, string.Empty);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return (false, "Error interno del servidor al intentar procesar el pago.");
            }
        }

        public async Task<DashboardDTO> GetDashboardMetricsAsync(int restaurantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            DateTime start;
            if (startDate.HasValue)
            {
                start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            }
            else
            {
                start = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            }

            DateTime end;
            if (endDate.HasValue)
            {
                end = DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1);
            }
            else
            {
                end = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1);
            }

            var paidOrders = await orderRepository.GetAllAsync(o =>
                o.RestaurantId == restaurantId &&
                o.Status == OrderStatus.Paid &&
                o.OrderDate >= start && o.OrderDate <= end);

            var orderIds = paidOrders.Select(o => o.Id).ToList();

            var dashboard = new DashboardDTO
            {
                TablesServed = paidOrders.Count(),
                TotalSales = paidOrders.Sum(o => o.TotalAmount)
            };

            dashboard.AverageTicket = dashboard.TablesServed > 0 ? dashboard.TotalSales / dashboard.TablesServed : 0;

            if (dashboard.TablesServed == 0) return dashboard;

            var allPayments = await paymentRepository.GetAllAsync(p => orderIds.Contains(p.OrderId));
            var methods = await paymentMethodRepository.GetAllAsync();

            foreach (var method in methods)
            {
                var totalForMethod = allPayments.Where(p => p.PaymentMethodId == method.Id).Sum(p => p.Amount);
                if (totalForMethod > 0)
                {
                    dashboard.SalesByPaymentMethod.Add(new PaymentMethodMetric { MethodName = method.Name, Total = totalForMethod });
                }
            }

            var allDetails = await orderDetailRepository.GetAllAsync(d => orderIds.Contains(d.OrderId));
            var allDishes = await dishRepository.GetAllAsync(d => d.RestaurantId == restaurantId);

            dashboard.TopDishes = [.. allDetails
                .GroupBy(d => d.DishId)
                .Select(g => new TopDishMetric
                {
                    DishName = allDishes.FirstOrDefault(dish => dish.Id == g.Key)?.Name ?? "Desconocido",
                    Quantity = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)];

            return dashboard;
        }

        private void DeductInventory(Dish dish, int quantity, IEnumerable<RecipeDetails> recipes, IEnumerable<Ingredient> ingredients)
        {
            var dishRecipes = recipes.Where(r => r.DishId == dish.Id);
            int portions = dish.YieldPortions > 0 ? dish.YieldPortions : 1;

            foreach (var detail in dishRecipes)
            {
                var ing = ingredients.FirstOrDefault(i => i.Id == detail.IngredientId);
                if (ing != null)
                {
                    decimal requiredPerPlate = detail.RequiredQuantity / portions;
                    decimal totalToDeduct = requiredPerPlate * quantity;
                    ing.CurrentStock -= totalToDeduct;
                    ingredientRepository.Update(ing);
                }
            }
        }
    }
}