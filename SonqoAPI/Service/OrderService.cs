using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SonqoAPI.Hubs;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Models.Enum;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;
using System.Data;

namespace SonqoAPI.Service
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
        public async Task<bool> CreateOrderAsync(OrderCreateDTO orderDto, int? jwtRestaurantId = null)
        {
            var table = await tableRepository.GetQueryable()
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.Id == orderDto.TableId);

            if (table == null || table.IsDeleted || table.Restaurant == null || !table.Restaurant.IsActive)
                return false;

            int actualRestaurantId = table.RestaurantId;

            if (jwtRestaurantId.HasValue && jwtRestaurantId.Value != 0 && jwtRestaurantId.Value != actualRestaurantId)
            {
                return false;
            }

            var recipes = await recipeRepository.GetAllAsync();
            var ingredients = await ingredientRepository.GetAllAsync(i => i.RestaurantId == actualRestaurantId);

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

            foreach (var kvp in requiredIngredients)
            {
                var ing = ingredients.FirstOrDefault(i => i.Id == kvp.Key);
                if (ing == null || ing.CurrentStock < kvp.Value)
                {
                    return false;
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
                }
                orderRepository.Update(activeOrder);

                try
                {
                    await orderRepository.SaveChangesAsync();
                    targetOrderId = activeOrder.Id;
                }
                catch (DbUpdateConcurrencyException) { return false; }
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
                }

                if (newOrder.OrderDetails.Count == 0) return false;

                newOrder.TotalAmount = total;
                table.IsAvailable = false;
                tableRepository.Update(table);

                await orderRepository.AddAsync(newOrder);

                try
                {
                    await orderRepository.SaveChangesAsync();
                    targetOrderId = newOrder.Id;
                }
                catch (DbUpdateConcurrencyException) { return false; }
            }

            if (targetOrderId > 0)
            {
                await orderHub.Clients.Group(actualRestaurantId.ToString()).SendAsync("ReceiveOrderUpdate", new { orderId = targetOrderId, type = "ORDER_CREATED" });
            }

            return true;
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
            await orderHub.Clients.Group(restaurantId.ToString()).SendAsync("ReceiveOrderUpdate", new { detailId, status });
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

        public async Task<bool> PayOrderAsync(int orderId, PayOrderDTO paymentData, int restaurantId)
        {
            var openRegister = await cashRegisterRepository.GetQueryable()
                .FirstOrDefaultAsync(c => c.RestaurantId == restaurantId && c.IsOpen);

            if (openRegister == null) return false;

            using var transaction = await orderRepository.BeginTransactionAsync();
            try
            {
                var order = await orderRepository.GetQueryable()
                    .Include(o => o.OrderDetails)
                    .Include(o => o.Table)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.RestaurantId == restaurantId);

                if (order == null || order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                decimal calculatedTotal = order.OrderDetails.Sum(d => Math.Round(d.SubTotal, 2, MidpointRounding.AwayFromZero));
                order.TotalAmount = calculatedTotal;

                decimal amountAlreadyPaid = order.Payments.Sum(p => Math.Round(p.Amount, 2, MidpointRounding.AwayFromZero));
                decimal remainingBalance = calculatedTotal - amountAlreadyPaid;

                decimal incomingPaymentTotal = 0;

                if (paymentData.SplitPayments != null && paymentData.SplitPayments.Count > 0)
                {
                    incomingPaymentTotal = paymentData.SplitPayments.Sum(p => Math.Round(p.Amount, 2, MidpointRounding.AwayFromZero));
                }
                else
                {
                    incomingPaymentTotal = Math.Round(paymentData.TotalAmountPaid, 2, MidpointRounding.AwayFromZero);
                }

                if (Math.Abs(incomingPaymentTotal - remainingBalance) > 0.01m)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                if (paymentData.SplitPayments != null && paymentData.SplitPayments.Count > 0)
                {
                    foreach (var split in paymentData.SplitPayments)
                    {
                        if (split.Amount <= 0) continue;
                        order.Payments.Add(new Payment
                        {
                            OrderId = order.Id,
                            PaymentMethodId = split.PaymentMethodId,
                            Amount = Math.Round(split.Amount, 2, MidpointRounding.AwayFromZero),
                            PaymentDate = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    if (paymentData.TotalAmountPaid > 0)
                    {
                        order.Payments.Add(new Payment
                        {
                            OrderId = order.Id,
                            PaymentMethodId = paymentData.PrimaryPaymentMethodId,
                            Amount = Math.Round(paymentData.TotalAmountPaid, 2, MidpointRounding.AwayFromZero),
                            PaymentDate = DateTime.UtcNow
                        });
                    }
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
                await orderHub.Clients.Group(order.RestaurantId.ToString()).SendAsync("ReceiveOrderUpdate", new { orderId = order.Id, type = "ORDER_PAID" });

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<DashboardDTO> GetDashboardMetricsAsync(int restaurantId)
        {
            var today = DateTime.UtcNow.Date;
            var paidOrders = await orderRepository.GetAllAsync(o =>
                o.RestaurantId == restaurantId &&
                o.Status == OrderStatus.Paid &&
                o.OrderDate >= today);

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