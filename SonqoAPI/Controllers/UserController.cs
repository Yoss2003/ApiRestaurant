using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;
using System.Security.Claims;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class UsersController(IAuthService authService) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        private string GetCurrentRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            int restaurantId = GetCurrentRestaurantId();
            var users = await authService.GetAllUsersAsync(restaurantId);
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int restaurantId = GetCurrentRestaurantId();
            var newUser = await authService.CreateUserAsync(dto, restaurantId, GetCurrentRole());
            if (newUser == null)
            {
                return BadRequest(new { message = "El nombre de usuario ya está en uso o no tiene los permisos suficientes." });
            }
            return Ok(newUser);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int restaurantId = GetCurrentRestaurantId();
            var result = await authService.UpdateUserAsync(id, dto, restaurantId, GetCurrentRole());
            if (!result)
            {
                return BadRequest(new { message = "No se pudo actualizar. Verifique que no haya duplicidad o falta de permisos." });
            }
            return Ok(new { message = "Usuario actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            int restaurantId = GetCurrentRestaurantId();

            var result = await authService.DeleteUserAsync(id, restaurantId, GetCurrentRole());

            if (!result)
            {
                return BadRequest(new { message = "No se pudo eliminar el usuario." });
            }
            return Ok(new { message = "Usuario eliminado correctamente." });
        }
    }
}