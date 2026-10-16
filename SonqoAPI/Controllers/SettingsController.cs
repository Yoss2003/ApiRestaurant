using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SonqoAPI.Controllers
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

        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] RestaurantSettingsDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            int restaurantId = GetCurrentRestaurantId();
            var settings = await restaurantRepo.GetByIdAsync(restaurantId);

            if (settings == null)
            {
                // Si por alguna extraña razón el local no existe en DB, lo creamos
                settings = new Restaurant
                {
                    Id = restaurantId,
                    Name = dto.Name,
                    Ruc = dto.Ruc,
                    Address = dto.Address
                };
                await restaurantRepo.AddAsync(settings);
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
                return Forbid("Acceso denegado. Solo el SuperAdmin puede crear nuevas sucursales o empresas.");
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
    }
}