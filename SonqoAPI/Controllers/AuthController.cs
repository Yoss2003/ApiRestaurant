using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;
using System.Security.Claims;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            string adminRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";
            int adminRestaurantId = int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "RestaurantId")?.Value, out int id) ? id : 0;

            if (adminRole != "SuperAdmin")
            {
                dto.RestaurantId = adminRestaurantId;
            }

            var success = await authService.RegisterAsync(dto, adminRole == "SuperAdmin" ? 99 : 0);
            if (!success) return BadRequest(new { message = "Registro fallido. Verifique roles o usuario duplicado." });
            return Ok(new { message = "Usuario registrado." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("LoginLimit")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginDto)
        {
            var result = await authService.LoginAsync(loginDto);

            if (result == null)
            {
                return Unauthorized("Credenciales inválidas o usuario inactivo.");
            }

            return Ok(result);
        }
    }
}