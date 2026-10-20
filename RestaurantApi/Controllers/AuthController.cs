using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RestaurantApi.Models;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Models.Enum;
using RestaurantApi.Repositories.IRepositories;
using RestaurantApi.Service.IService;
using System.Security.Claims;

namespace RestaurantApi.Controllers
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
            var (result, errorMessage) = await authService.LoginAsync(loginDto);

            if (result == null)
            {
                return Unauthorized(new { message = errorMessage });
            }

            return Ok(result);
        }

        [HttpPost("reset-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetAdmin(
            [FromServices] IGenericRepository<User> userRepo,
            [FromServices] IPasswordHasher<User> hasher)
        {
            var users = await userRepo.GetAllAsync(u => u.Username.ToLower() == "admin");
            var admin = users.FirstOrDefault();

            if (admin == null)
            {
                admin = new User
                {
                    Username = "Admin",
                    Role = Role.SuperAdmin,
                    IsActive = true
                };
                admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
                await userRepo.AddAsync(admin);
            }
            else
            {
                admin.Role = Role.SuperAdmin;
                admin.IsActive = true;
                admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
                userRepo.Update(admin);
            }

            await userRepo.SaveChangesAsync();
            return Ok(new { message = "¡Listo! Ya puedes iniciar sesión con Admin y Admin123!" });
        }
    }
}