using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.RateLimiting;
using SonqoAPI.Hubs;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController(
        IOrderService orderService,
        IHubContext<OrderHub> hubContext) : ControllerBase
    {

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("OrderLimit")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDTO orderDto)
        {
            var success = await orderService.CreateOrderAsync(orderDto);

            if (!success)
            {
                return BadRequest(new { message = "No se pudo procesar el pedido. Verifique que la mesa exista y los platos estén disponibles." });
            }

            await hubContext.Clients.All.SendAsync("ReceiveOrderUpdate", "ORDER_CREATED");

            return Ok(new { message = "Pedido creado exitosamente." });
        }

        [HttpPatch("details/{detailId}/status")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> UpdateDetailStatus(int detailId, [FromBody] StatusUpdateDTO dto)
        {
            var success = await orderService.UpdateOrderDetailStatusAsync(detailId, dto.Status);

            if (!success) return NotFound("No se pudo actualizar el estado del plato.");

            await hubContext.Clients.All.SendAsync("ReceiveOrderUpdate", "STATUS_UPDATED");

            return Ok(new { Message = "Estado actualizado correctamente." });
        }

        [HttpGet("kitchen")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> GetKitchenOrders()
        {
            var orders = await orderService.GetKitchenOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("table/{tableId}/active")]
        public async Task<IActionResult> GetActiveOrder(int tableId)
        {
            var order = await orderService.GetActiveOrderAsync(tableId);
            if (order == null) return NoContent();
            return Ok(order);
        }

        [HttpPost("table/{tableId}/pay")]
        public async Task<IActionResult> PayOrder(int tableId, [FromBody] PayOrderDTO paymentData)
        {
            var success = await orderService.PayOrderAsync(tableId, paymentData);

            if (!success)
                return BadRequest(new { message = "No se pudo cobrar la orden. Verifique que exista una orden activa." });

            await hubContext.Clients.All.SendAsync("ReceiveOrderUpdate", "ORDER_PAID");

            return Ok(new { message = "Orden cobrada y mesa liberada exitosamente." });
        }

        [HttpGet("dashboard")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var metrics = await orderService.GetDashboardMetricsAsync();
            return Ok(metrics);
        }
    }
}