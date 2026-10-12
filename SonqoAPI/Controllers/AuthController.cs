using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Models.Enum;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService, IGenericRepository<User> userRepository) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            var result = await authService.RegisterAsync(dto);

            if (!result)
                return BadRequest(new { message = "No se pudo registrar el usuario (el nombre de usuario ya existe)." });

            return Ok(new { message = "Usuario registrado exitosamente. Ya puedes iniciar sesión." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginDto)
        {
            var result = await authService.LoginAsync(loginDto);

            if (result == null)
            {
                return Unauthorized("Credenciales inválidas o usuario inactivo.");
            }

            return Ok(result);
        }

        // Endpoint para poblar los usuarios de prueba tras limpiar la base de datos
        [HttpPost("seed-users")]
        public async Task<IActionResult> SeedUsers()
        {
            var hasUsers = await userRepository.ExistsAsync(u => true);
            if (hasUsers) return BadRequest("Los usuarios ya fueron creados.");

            var admin = new User { Username = "admin", PasswordHash = "admin123", Role = Role.Admin };
            var waiter = new User { Username = "mesero", PasswordHash = "mesero123", Role = Role.Waiter };
            var kitchen = new User { Username = "cocina", PasswordHash = "cocina123", Role = Role.Kitchen };

            await userRepository.AddRangeAsync([admin, waiter, kitchen]);
            await userRepository.SaveChangesAsync();

            return Ok(new { Message = "Usuarios de prueba creados exitosamente." });
        }
    }
}