using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Service.IService;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RestaurantApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireStaff")]
    public class InventoryController(IInventoryService inventoryService, IAuditService auditService) : ControllerBase
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

        [HttpPost("calculate-planning")]
        [Authorize(Roles = "Kitchen,Admin")]
        public async Task<ActionResult<List<CalculatedIngredientDTO>>> CalculatePlanning([FromBody] List<PlanningRequestDTO> planning)
        {
            if (planning == null || planning.Count == 0)
            {
                return BadRequest("Planning data cannot be empty.");
            }
            int restaurantId = GetCurrentRestaurantId();
            var result = await inventoryService.CalculatePlanningAsync(planning, restaurantId);

            if (result.Count == 0)
            {
                return NotFound("No recipes found for the provided dishes.");
            }
            return Ok(result);
        }

        [HttpPut("ingredients/{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateIngredient(int id, [FromBody] IngredientCreateDTO dto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await inventoryService.UpdateIngredientAsync(id, dto, restaurantId);
            if (!success) return NotFound(new { message = "Insumo no encontrado." });
            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "UPDATE", "Ingredient", id.ToString(), $"Alteró el registro del insumo '{dto.Name}'");
            return Ok(new { message = "Insumo actualizado correctamente." });
        }

        [HttpDelete("ingredients/{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await inventoryService.DeleteIngredientAsync(id, restaurantId);
            if (!success) return NotFound(new { message = "Insumo no encontrado o ya eliminado." });
            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "DELETE", "Ingredient", id.ToString(), $"Eliminó el insumo con ID {id}");
            return Ok(new { message = "Insumo eliminado correctamente." });
        }

        [HttpPost("seed-test-data")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SeedTestData()
        {
            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsDevelopment())
            {
                return Forbid("El sembrado de datos está deshabilitado en entornos de producción.");
            }
            int restaurantId = GetCurrentRestaurantId();
            var success = await inventoryService.SeedTestDataAsync(restaurantId);
            if (!success)
            {
                return BadRequest("Database already contains data.");
            }
            return Ok(new { Message = "Test data inserted successfully." });
        }

        [HttpGet("ingredients")]
        public async Task<IActionResult> GetAllIngredients()
        {
            int restaurantId = GetCurrentRestaurantId();
            var ingredients = await inventoryService.GetAllIngredientsAsync(restaurantId);
            return Ok(ingredients);
        }

        [HttpPost("ingredients")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateIngredient([FromBody] IngredientCreateDTO dto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var created = await inventoryService.CreateIngredientAsync(dto, restaurantId);
            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "CREATE", "Ingredient", created.Id.ToString(), $"Creó el insumo '{dto.Name}' (Stock inicial: {dto.InitialStock})");
            return Ok(created);
        }

        [HttpPost("ingredients/restock")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> RestockIngredient([FromBody] RestockDTO dto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await inventoryService.RestockIngredientAsync(dto, restaurantId);
            if (!success) return BadRequest(new { message = "Error al procesar el ingreso." });
            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "RESTOCK", "Ingredient", dto.IngredientId.ToString(), $"Agregó {dto.QuantityAdded} unidades al inventario por un costo de S/ {dto.TotalCostPaid}");
            return Ok(new { message = "Inventario actualizado y costo recalculado con éxito." });
        }
    }
}