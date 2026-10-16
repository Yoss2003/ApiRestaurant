using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CashRegisterController(ICashRegisterService cashService) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet("current")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetCurrentOpenRegister()
        {
            int restaurantId = GetCurrentRestaurantId();
            var openRegister = await cashService.GetOpenRegisterAsync(restaurantId);
            if (openRegister == null)
            {
                return Ok(new { isOpen = false, message = "La caja está cerrada." });
            }

            return Ok(new
            {
                isOpen = true,
                registerId = openRegister.Id,
                openedByUserId = openRegister.UserId,
                openingBalance = openRegister.OpeningBalance,
                openedAt = openRegister.OpenedAt
            });
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

            return Ok(new { message = "Caja cerrada exitosamente. Verifique el cuadre (discrepancia)." });
        }
    }
}