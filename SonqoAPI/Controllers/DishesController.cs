using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DishesController(IDishService dishService) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Dish>>> GetAll()
        {
            var dishes = await dishService.GetAllActiveDishesAsync();
            return Ok(dishes);
        }

        [HttpGet("{id}/recipe")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> GetRecipe(int id)
        {
            var recipe = await dishService.GetRecipeByDishIdAsync(id);

            if (recipe == null)
            {
                return Ok(new { dishId = id, yieldPortions = 1, items = Array.Empty<object>() });
            }

            return Ok(recipe);
        }

        [HttpPost]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> CreateDish([FromBody] DishUpdateDTO dishDto)
        {
            var createdDish = await dishService.CreateDishAsync(dishDto);
            return Ok(createdDish);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> Update(int id, [FromBody] DishUpdateDTO dishDto)
        {
            var success = await dishService.UpdateDishAsync(id, dishDto);
            if (!success) return NotFound("Plato no encontrado o inactivo.");
            return Ok(new { Message = "Plato actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var success = await dishService.SoftDeleteDishAsync(id);
            if (!success) return NotFound("No se encontró el plato.");
            return Ok(new { Message = "Plato eliminado del menú." });
        }

        [HttpPost("assign-recipe")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> AssignRecipe([FromBody] AssignRecipeDTO recipeDto)
        {
            var success = await dishService.AssignRecipeAsync(recipeDto);
            if (!success) return BadRequest("Error al procesar la receta. Verifique el plato y los ingredientes.");
            return Ok(new { Message = "Receta guardada y porcionada correctamente." });
        }

        [HttpGet("shopping-list")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetShoppingList()
        {
            var shoppingList = await dishService.GetShoppingListAsync();
            return Ok(shoppingList);
        }
    }
}