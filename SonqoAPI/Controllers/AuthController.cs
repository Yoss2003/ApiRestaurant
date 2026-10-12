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
            var result = await authService.RegisterAsync(dto);

            if (!result)
                return BadRequest(new { message = "No se pudo registrar el usuario (el nombre de usuario ya existe)." });

            return Ok(new { message = "Usuario registrado exitosamente. Ya puedes iniciar sesión." });
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