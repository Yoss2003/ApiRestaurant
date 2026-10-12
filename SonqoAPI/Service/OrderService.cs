using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Models.Enum;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;

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
        IGenericRepository<PaymentMethod> paymentMethodRepository
        ) : IOrderService
    {
        public async Task<bool> CreateOrderAsync(OrderCreateDTO orderDto)
        {
            var table = await tableRepository.GetByIdAsync(orderDto.TableId);
            if (table == null || table.IsDeleted) return false;

            var recipes = await recipeRepository.GetAllAsync();
            var ingredients = await ingredientRepository.GetAllAsync();
            var existingOrders = await orderRepository.GetAllAsync(o => o.TableId == orderDto.TableId && o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled);
            var activeOrder = existingOrders.FirstOrDefault();

            if (activeOrder != null)
            {
                foreach (var item in orderDto.Items)
                {
                    var dish = await dishRepository.GetByIdAsync(item.DishId);

                    if (dish != null && dish.IsAvailable)
                    {
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
                }

                orderRepository.Update(activeOrder);
                await orderRepository.SaveChangesAsync();
                await ingredientRepository.SaveChangesAsync();
                return true;
            }
            else
            {
                var newOrder = new Order
                {
                    TableId = orderDto.TableId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0
                };

                decimal total = 0;

                foreach (var item in orderDto.Items)
                {
                    var dish = await dishRepository.GetByIdAsync(item.DishId);

                    if (dish != null && dish.IsAvailable)
                    {
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
                }

                if (newOrder.OrderDetails.Count == 0) return false;

                newOrder.TotalAmount = total;
                table.IsAvailable = false;
                tableRepository.Update(table);

                await orderRepository.AddAsync(newOrder);
                await orderRepository.SaveChangesAsync();
                await ingredientRepository.SaveChangesAsync();

                return true;
            }
        }

        public async Task<bool> UpdateOrderDetailStatusAsync(int detailId, string status)
        {
            var detail = await orderDetailRepository.GetByIdAsync(detailId);
            if (detail == null) return false;

            detail.Status = status;
            orderDetailRepository.Update(detail);
            await orderDetailRepository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<KitchenOrderDTO>> GetKitchenOrdersAsync()
        {
            // Traemos las órdenes que están activas
            var activeOrders = await orderRepository.GetAllAsync(o => o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled);
            var allTables = await tableRepository.GetAllAsync();
            var allDishes = await dishRepository.GetAllAsync();

            // Traemos todos los detalles (aprovechando que inyectaste orderDetailRepository)[cite: 13]
            var allDetails = await orderDetailRepository.GetAllAsync();

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
                    // La cocina solo debe ver los platos que faltan por entregar
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

                // Solo enviamos el ticket a la cocina si tiene platos pendientes
                if (dto.Details.Count != 0)
                {
                    kitchenOrders.Add(dto);
                }
            }

            return kitchenOrders;
        }

        public async Task<ActiveOrderDTO?> GetActiveOrderAsync(int tableId)
        {
            // Buscamos la orden que no esté ni pagada ni cancelada
            var activeOrders = await orderRepository.GetAllAsync(o => o.TableId == tableId && o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled);
            var order = activeOrders.FirstOrDefault();

            if (order == null) return null;

            var allDishes = await dishRepository.GetAllAsync();
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

        public async Task<bool> PayOrderAsync(int tableId, PayOrderDTO paymentData)
        {
            var activeOrders = await orderRepository.GetAllAsync(o => o.TableId == tableId && o.Status != OrderStatus.Paid && o.Status != OrderStatus.Cancelled);
            var order = activeOrders.FirstOrDefault();

            if (order == null) return false;

            // 1. Procesamos los pagos y los guardamos en BD
            if (paymentData.SplitPayments != null && paymentData.SplitPayments.Count != 0)
            {
                foreach (var split in paymentData.SplitPayments)
                {
                    var payment = new Payment
                    {
                        OrderId = order.Id,
                        PaymentMethodId = split.PaymentMethodId,
                        Amount = split.Amount,
                        PaymentDate = DateTime.UtcNow
                    };
                    await paymentRepository.AddAsync(payment);
                }
            }
            else
            {
                var payment = new Payment
                {
                    OrderId = order.Id,
                    PaymentMethodId = paymentData.PrimaryPaymentMethodId,
                    Amount = paymentData.TotalAmountPaid,
                    PaymentDate = DateTime.UtcNow
                };
                await paymentRepository.AddAsync(payment);
            }

            // Guardamos todos los pagos insertados
            await paymentRepository.SaveChangesAsync();

            // 2. Cambiamos el estado de la orden a Pagada
            order.Status = OrderStatus.Paid;
            orderRepository.Update(order);
            await orderRepository.SaveChangesAsync();

            // 3. Liberamos la mesa para que vuelva a estar verde
            var table = await tableRepository.GetByIdAsync(tableId);
            if (table != null)
            {
                table.IsAvailable = true;
                tableRepository.Update(table);
                await tableRepository.SaveChangesAsync();
            }

            return true;
        }

        public async Task<DashboardDTO> GetDashboardMetricsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var paidOrders = await orderRepository.GetAllAsync(o => o.Status == OrderStatus.Paid && o.OrderDate >= today);
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
                    dashboard.SalesByPaymentMethod.Add(new PaymentMethodMetric
                    {
                        MethodName = method.Name,
                        Total = totalForMethod
                    });
                }
            }

            var allDetails = await orderDetailRepository.GetAllAsync(d => orderIds.Contains(d.OrderId));
            var allDishes = await dishRepository.GetAllAsync();

            var topDishes = allDetails
                .GroupBy(d => d.DishId)
                .Select(g => new TopDishMetric
                {
                    DishName = allDishes.FirstOrDefault(dish => dish.Id == g.Key)?.Name ?? "Desconocido",
                    Quantity = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            dashboard.TopDishes = topDishes;

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

                    if (ing.CurrentStock < 0) ing.CurrentStock = 0;

                    ingredientRepository.Update(ing);
                }
            }
        }
    }
}