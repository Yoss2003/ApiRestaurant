using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SonqoAPI.Hubs;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController(
        IOrderService orderService,
        IGenericRepository<Table> tableRepository,
        IConfiguration configuration) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        private int GetCurrentUserId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet("table-session/{restaurantId}/{tableId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTableSession(int restaurantId, int tableId)
        {
            var table = await tableRepository.GetQueryable()
                .Include(t => t.Restaurant)
                .FirstOrDefaultAsync(t => t.Id == tableId && t.RestaurantId == restaurantId);

            if (table == null || table.IsDeleted || table.Restaurant == null || !table.Restaurant.IsActive)
            {
                return BadRequest(new { message = "La mesa o el restaurante no existen o no están disponibles." });
            }

            var secretKey = configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(secretKey)) return StatusCode(500, new { message = "Configuración JWT inválida en el servidor." });

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var claims = new[]
            {
                new Claim("TableId", tableId.ToString()),
                new Claim("RestaurantId", restaurantId.ToString()),
                new Claim("SessionType", "PublicQR")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = configuration["JwtSettings:Issuer"],
                Audience = configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { sessionToken = tokenHandler.WriteToken(token) });
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting("OrderLimit")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDTO orderDto)
        {
            int? jwtRestaurantId = null;
            int? jwtTableId = null;

            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            if (roleClaim != null)
            {
                if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "RestaurantId")?.Value, out int id))
                {
                    jwtRestaurantId = id;
                }
            }
            else
            {
                // P05: Cliente anónimo. Debe tener el token del QR.
                var sessionType = User.Claims.FirstOrDefault(c => c.Type == "SessionType")?.Value;
                if (sessionType != "PublicQR")
                {
                    return Unauthorized(new { message = "Sesión inválida. Escanea el código QR de nuevo." });
                }

                if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "RestaurantId")?.Value, out int rId)) jwtRestaurantId = rId;
                if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "TableId")?.Value, out int tId)) jwtTableId = tId;

                if (jwtTableId.HasValue && jwtTableId.Value != orderDto.TableId)
                {
                    return BadRequest(new { message = "El pedido no coincide con su mesa autorizada. Vuelva a escanear el QR." });
                }
            }

            var orderId = await orderService.CreateOrderAsync(orderDto, jwtRestaurantId);
            if (orderId == 0)
                return BadRequest(new { message = "No se pudo procesar el pedido. Verifique stock y mesa." });

            return Ok(new { message = "Pedido creado exitosamente.", orderId = orderId });
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
            int userId = GetCurrentUserId();

            var success = await orderService.PayOrderAsync(orderId, paymentData, restaurantId, userId);

            if (!success)
                return BadRequest(new { message = "No se pudo cobrar. Verifique orden, concurrencia y estado de caja." });

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