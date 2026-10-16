using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController(IInventoryService inventoryService) : ControllerBase
    {
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
        public async Task<IActionResult> UpdateIngredient(int id, [FromBody] IngredientCreateDTO dto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await inventoryService.UpdateIngredientAsync(id, dto, restaurantId);
            if (!success) return NotFound(new { message = "Insumo no encontrado." });
            return Ok(new { message = "Insumo actualizado correctamente." });
        }

        [HttpDelete("ingredients/{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await inventoryService.DeleteIngredientAsync(id, restaurantId);
            if (!success) return NotFound(new { message = "Insumo no encontrado o ya eliminado." });
            return Ok(new { message = "Insumo eliminado correctamente." });
        }

        [HttpPost("seed-test-data")]
        public async Task<IActionResult> SeedTestData()
        {
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
        public async Task<IActionResult> CreateIngredient([FromBody] IngredientCreateDTO dto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var created = await inventoryService.CreateIngredientAsync(dto, restaurantId);
            return Ok(created);
        }

        [HttpPost("ingredients/restock")]
        public async Task<IActionResult> RestockIngredient([FromBody] RestockDTO dto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await inventoryService.RestockIngredientAsync(dto, restaurantId);
            if (!success) return BadRequest(new { message = "Error al procesar el ingreso." });
            return Ok(new { message = "Inventario actualizado y costo recalculado con éxito." });
        }
    }
}