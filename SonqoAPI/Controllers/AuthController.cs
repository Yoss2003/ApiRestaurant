using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;

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
            var success = await authService.RegisterAsync(dto, 0);
            if (!success) return BadRequest(new { message = "Registro fallido." });
            return Ok(new { message = "Usuario registrado." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
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