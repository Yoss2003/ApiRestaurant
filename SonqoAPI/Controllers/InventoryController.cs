using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController(IInventoryService inventoryService) : ControllerBase
    {
        [HttpPost("calculate-planning")]
        [Authorize(Roles = "Kitchen,Admin")]
        public async Task<ActionResult<List<CalculatedIngredientDTO>>> CalculatePlanning([FromBody] List<PlanningRequestDTO> planning)
        {
            if (planning == null || planning.Count == 0)
            {
                return BadRequest("Planning data cannot be empty.");
            }

            var result = await inventoryService.CalculatePlanningAsync(planning);

            if (result.Count == 0)
            {
                return NotFound("No recipes found for the provided dishes.");
            }

            return Ok(result);
        }

        [HttpPut("ingredients/{id}")]
        public async Task<IActionResult> UpdateIngredient(int id, [FromBody] IngredientCreateDTO dto)
        {
            // Asumiendo que crearás este método en tu InventoryService
            var success = await inventoryService.UpdateIngredientAsync(id, dto);

            if (!success) return NotFound(new { message = "Insumo no encontrado." });
            return Ok(new { message = "Insumo actualizado correctamente." });
        }

        [HttpDelete("ingredients/{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            // Asumiendo que crearás este método en tu InventoryService
            var success = await inventoryService.DeleteIngredientAsync(id);

            if (!success) return NotFound(new { message = "Insumo no encontrado o ya eliminado." });
            return Ok(new { message = "Insumo eliminado correctamente." });
        }

        [HttpPost("seed-test-data")]
        public async Task<IActionResult> SeedTestData()
        {
            var success = await inventoryService.SeedTestDataAsync();

            if (!success)
            {
                return BadRequest("Database already contains data.");
            }

            return Ok(new { Message = "Test data inserted successfully." });
        }

        [HttpGet("ingredients")]
        public async Task<IActionResult> GetAllIngredients()
        {
            var ingredients = await inventoryService.GetAllIngredientsAsync();
            return Ok(ingredients);
        }

        [HttpPost("ingredients")]
        public async Task<IActionResult> CreateIngredient([FromBody] IngredientCreateDTO dto)
        {
            var created = await inventoryService.CreateIngredientAsync(dto);
            return Ok(created);
        }

        [HttpPost("ingredients/restock")]
        public async Task<IActionResult> RestockIngredient([FromBody] RestockDTO dto)
        {
            var success = await inventoryService.RestockIngredientAsync(dto);
            if (!success) return BadRequest(new { message = "Error al procesar el ingreso." });

            return Ok(new { message = "Inventario actualizado y costo recalculado con éxito." });
        }
    }
}