using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Service.IService;
using System.Security.Claims;

namespace RestaurantApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CashRegisterController(ICashRegisterService cashService, IAuditService auditService) : ControllerBase
    {
        private int GetCurrentUserId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet("history")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetHistory()
        {
            int restaurantId = GetCurrentRestaurantId();
            var history = await cashService.GetHistoryAsync(restaurantId);
            return Ok(history);
        }

        [HttpGet("current")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetCurrentOpenRegister()
        {
            int restaurantId = GetCurrentRestaurantId();
            var openRegisterInfo = await cashService.GetOpenRegisterInfoAsync(restaurantId);

            if (openRegisterInfo == null)
            {
                return Ok(new { isOpen = false, message = "La caja está cerrada." });
            }

            return Ok(new
            {
                isOpen = true,
                registerId = openRegisterInfo.RegisterId,
                openedByUserId = openRegisterInfo.UserId,
                openingBalance = openRegisterInfo.OpeningBalance,
                openedAt = openRegisterInfo.OpenedAt,
                expectedCashBalance = openRegisterInfo.ExpectedCashBalance,
                movements = openRegisterInfo.Movements,
                paymentBreakdown = openRegisterInfo.PaymentBreakdown
            });
        }

        [HttpPatch("movement/{id}/reason")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateMovementReason(int id, [FromBody] UpdateMovementReasonDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int restaurantId = GetCurrentRestaurantId();
            var success = await cashService.UpdateMovementReasonAsync(id, dto.Reason, restaurantId);

            if (!success)
                return BadRequest(new { message = "No se pudo actualizar. Verifique que la caja siga abierta." });

            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "UPDATE", "CashMovement", id.ToString(), $"Cambió el motivo del movimiento a: '{dto.Reason}'");
            return Ok(new { message = "Motivo corregido correctamente." });
        }

        [HttpPost("open")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> OpenRegister([FromBody] OpenRegisterDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Usuario no válido en el token." });
            }

            int restaurantId = GetCurrentRestaurantId();
            var success = await cashService.OpenRegisterAsync(userId, dto.OpeningBalance, restaurantId);
            if (!success)
            {
                return BadRequest(new { message = "Ya existe una caja abierta. Ciérrela antes de abrir una nueva." });
            }

            await auditService.LogActionAsync(restaurantId, userId, "OPEN", "CashRegister", "0", $"Abrió la caja con S/ {dto.OpeningBalance}");
            return Ok(new { message = "Caja abierta exitosamente." });
        }

        [HttpPost("close")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CloseRegister([FromBody] CloseRegisterDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Usuario no válido en el token." });
            }

            int restaurantId = GetCurrentRestaurantId();
            var success = await cashService.CloseRegisterAsync(userId, dto.ActualBalance, dto.Notes, restaurantId);
            if (!success)
            {
                return BadRequest(new { message = "No hay ninguna caja abierta para cerrar." });
            }

            await auditService.LogActionAsync(restaurantId, userId, "CLOSE", "CashRegister", "0", $"Cerró la caja declarando S/ {dto.ActualBalance}");
            return Ok(new { message = "Caja cerrada exitosamente. Verifique el cuadre (discrepancia)." });
        }

        [HttpPost("movement")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> RegisterMovement([FromBody] CashMovementCreateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { message = "Usuario inválido." });

            int restaurantId = GetCurrentRestaurantId();

            var success = await cashService.RegisterMovementAsync(userId, restaurantId, dto);
            if (!success)
                return BadRequest(new { message = "No hay caja abierta para registrar el movimiento." });

            await auditService.LogActionAsync(restaurantId, userId, "MOVEMENT", "CashMovement", "0", $"Registró {dto.Type} por S/ {dto.Amount}. Motivo: {dto.Reason}");
            return Ok(new { message = "Movimiento registrado correctamente." });
        }
    }
}