using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using SonqoAPI.Hubs;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;
using System.Security.Claims;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController(
        IOrderService orderService) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("OrderLimit")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDTO orderDto)
        {
            int? jwtRestaurantId = null;
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                jwtRestaurantId = id;
            }

            var success = await orderService.CreateOrderAsync(orderDto, jwtRestaurantId);
            if (!success)
                return BadRequest(new { message = "No se pudo procesar el pedido. Verifique stock y mesa." });

            return Ok(new { message = "Pedido creado exitosamente." });
        }

        [HttpPatch("details/{detailId}/status")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> UpdateOrderDetailStatus(int detailId, [FromBody] StatusUpdateDTO statusDto)
        {
            string userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Client";
            int restaurantId = GetCurrentRestaurantId();

            var success = await orderService.UpdateOrderDetailStatusAsync(detailId, statusDto.Status, userRole, restaurantId);
            if (!success)
                return BadRequest(new { message = "Transición inválida o rol no autorizado." });

            return Ok(new { message = "Estado actualizado correctamente." });
        }

        [HttpGet("kitchen")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> GetKitchenOrders()
        {
            int restaurantId = GetCurrentRestaurantId();
            var orders = await orderService.GetKitchenOrdersAsync(restaurantId);
            return Ok(orders);
        }

        [HttpGet("table/{tableId}/active")]
        public async Task<IActionResult> GetActiveOrder(int tableId)
        {
            int restaurantId = GetCurrentRestaurantId();
            var order = await orderService.GetActiveOrderAsync(tableId, restaurantId);
            if (order == null) return NoContent();
            return Ok(order);
        }

        [HttpPost("{orderId}/pay")]
        [Authorize(Policy = "RequireStaff")]
        public async Task<IActionResult> PayOrder(int orderId, [FromBody] PayOrderDTO paymentData)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await orderService.PayOrderAsync(orderId, paymentData, restaurantId);
            if (!success)
                return BadRequest(new { message = "No se pudo cobrar. Verifique orden y estado de caja." });

            return Ok(new { message = "Orden cobrada y mesa liberada exitosamente." });
        }

        [HttpGet("dashboard")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            int restaurantId = GetCurrentRestaurantId();
            var metrics = await orderService.GetDashboardMetricsAsync(restaurantId);
            return Ok(metrics);
        }
    }
}