using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Models;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Repositories.IRepositories;
using RestaurantApi.Service.IService;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace RestaurantApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAdmin")]
    public class SettingsController(IGenericRepository<Restaurant> restaurantRepo) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            int restaurantId = GetCurrentRestaurantId();
            var settings = await restaurantRepo.GetByIdAsync(restaurantId);

            if (settings == null)
            {
                return Ok(new RestaurantSettingsDTO { Name = "", Ruc = "", Address = "" });
            }

            return Ok(new RestaurantSettingsDTO
            {
                Name = settings.Name,
                Ruc = settings.Ruc,
                Address = settings.Address
            });
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllRestaurants()
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (roleClaim != "SuperAdmin")
                return StatusCode(403, new { message = "Acceso denegado. Solo el SuperAdmin puede ver esta lista." });

            var restaurants = await restaurantRepo.GetAllAsync();

            var result = restaurants.Select(r => new {
                r.Id,
                r.Name,
                r.Ruc,
                r.IsLicenseSuspended,
                r.LicenseExpiresAt,
                r.MaxUsers
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] RestaurantSettingsDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int restaurantId = GetCurrentRestaurantId();
            if (restaurantId == 0) return BadRequest("Contexto de restaurante inválido.");

            var settings = await restaurantRepo.GetByIdAsync(restaurantId);

            if (settings == null)
            {
                return NotFound("Restaurante no encontrado. Contacte al SuperAdmin para crear la sucursal primero.");
            }
            else
            {
                settings.Name = dto.Name;
                settings.Ruc = dto.Ruc;
                settings.Address = dto.Address;
                restaurantRepo.Update(settings);
            }

            await restaurantRepo.SaveChangesAsync();

            return Ok(new { message = "Datos del restaurante guardados correctamente." });
        }

        [HttpPost("new-restaurant")]
        public async Task<IActionResult> CreateNewRestaurant([FromBody] RestaurantSettingsDTO dto)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (roleClaim != "SuperAdmin")
            {
                return StatusCode(403, new { message = "Acceso denegado. Solo el SuperAdmin puede crear nuevas sucursales." });
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var newRestaurant = new Restaurant
            {
                Name = dto.Name,
                Ruc = dto.Ruc,
                Address = dto.Address,
                IsActive = true
            };

            await restaurantRepo.AddAsync(newRestaurant);
            await restaurantRepo.SaveChangesAsync();

            return Ok(new { message = "Nueva empresa creada exitosamente.", restaurantId = newRestaurant.Id });
        }

        [HttpPost("update-limits/{restaurantId}")]
        public async Task<IActionResult> UpdateLicenseLimits(
            int restaurantId,
            [FromBody] UpdateLimitsDTO dto,
            [FromServices] IGenericRepository<User> userRepo,
            [FromServices] IPasswordHasher<User> passwordHasher)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            var currentUsername = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (roleClaim != "SuperAdmin")
                return StatusCode(403, new { message = "Solo el SuperAdmin puede cambiar límites." });

            var users = await userRepo.GetAllAsync(u => u.Username == currentUsername);
            var currentUser = users.FirstOrDefault();

            if (currentUser == null)
                return Unauthorized(new { message = "Usuario no encontrado." });

            var verificationResult = passwordHasher.VerifyHashedPassword(currentUser, currentUser.PasswordHash, dto.SuperAdminPassword);

            if (verificationResult == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Contraseña de SuperAdmin incorrecta. Operación denegada." });

            var restaurant = await restaurantRepo.GetByIdAsync(restaurantId);
            if (restaurant == null) return NotFound("Restaurante no encontrado.");

            restaurant.MaxUsers = dto.MaxUsers;
            restaurantRepo.Update(restaurant);
            await restaurantRepo.SaveChangesAsync();

            return Ok(new { message = $"Límite actualizado a {dto.MaxUsers} usuarios." });
        }

        [HttpPost("set-trial/{restaurantId}")]
        public async Task<IActionResult> SetTrialPeriod(int restaurantId, [FromBody] int trialDays)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (roleClaim != "SuperAdmin")
                return StatusCode(403, new { message = "Acceso denegado." });

            var restaurant = await restaurantRepo.GetByIdAsync(restaurantId);
            if (restaurant == null) return NotFound("Restaurante no encontrado.");

            restaurant.LicenseExpiresAt = DateTime.UtcNow.AddDays(trialDays);
            restaurant.IsLicenseSuspended = false;

            restaurantRepo.Update(restaurant);
            await restaurantRepo.SaveChangesAsync();

            return Ok(new { message = $"Periodo de prueba de {trialDays} días activado con éxito hasta el {restaurant.LicenseExpiresAt:dd/MM/yyyy}." });
        }
    }
}