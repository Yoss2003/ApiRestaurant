using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SonqoAPI.Hubs;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(
        IOrderService orderService,
        IGenericRepository<Table> tableRepository,
        IHubContext<OrderHub> hubContext) : ControllerBase
    {

        [HttpPost]
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
        public async Task<IActionResult> UpdateDetailStatus(int detailId, [FromBody] StatusUpdateDTO dto)
        {
            var success = await orderService.UpdateOrderDetailStatusAsync(detailId, dto.Status);

            if (!success) return NotFound("No se pudo actualizar el estado del plato.");

            await hubContext.Clients.All.SendAsync("ReceiveOrderUpdate", "STATUS_UPDATED");

            return Ok(new { Message = "Estado actualizado correctamente." });
        }

        [HttpGet("kitchen")]
        public async Task<IActionResult> GetKitchenOrders()
        {
            var orders = await orderService.GetKitchenOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("table/{tableId}/active")]
        public async Task<IActionResult> GetActiveOrder(int tableId)
        {
            var order = await orderService.GetActiveOrderAsync(tableId);
            if (order == null) return NotFound(new { message = "No hay ninguna orden activa en esta mesa." });

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
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var metrics = await orderService.GetDashboardMetricsAsync();
            return Ok(metrics);
        }

        [HttpPost("seed-tables")]
        public async Task<IActionResult> SeedTestTables()
        {
            var hasTables = await tableRepository.ExistsAsync(t => true);
            if (hasTables)
            {
                return BadRequest("Las mesas de prueba ya han sido creadas.");
            }

            var table1 = new Table { Number = "Mesa 1", Capacity = 4, IsAvailable = true };
            var table2 = new Table { Number = "Mesa 2", Capacity = 2, IsAvailable = true };
            var table3 = new Table { Number = "Barra 1", Capacity = 1, IsAvailable = true };

            await tableRepository.AddRangeAsync([table1, table2, table3]);
            await tableRepository.SaveChangesAsync();

            return Ok(new { Message = "Mesas de prueba creadas exitosamente." });
        }
    }
}