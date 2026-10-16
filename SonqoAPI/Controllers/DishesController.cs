using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonqoAPI.Models;
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
    public class DishesController(IDishService dishService) : ControllerBase
    {
        private int GetCurrentRestaurantId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "RestaurantId");
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Dish>>> GetAll()
        {
            int restaurantId = GetCurrentRestaurantId();
            var dishes = await dishService.GetAllActiveDishesAsync(restaurantId);
            return Ok(dishes);
        }

        [HttpGet("{id}/recipe")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> GetRecipe(int id)
        {
            int restaurantId = GetCurrentRestaurantId();
            var recipe = await dishService.GetRecipeByDishIdAsync(id, restaurantId);
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
            int restaurantId = GetCurrentRestaurantId();
            var createdDish = await dishService.CreateDishAsync(dishDto, restaurantId);
            return Ok(createdDish);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> Update(int id, [FromBody] DishUpdateDTO dishDto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await dishService.UpdateDishAsync(id, dishDto, restaurantId);
            if (!success) return NotFound("Plato no encontrado o inactivo.");
            return Ok(new { Message = "Plato actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await dishService.SoftDeleteDishAsync(id, restaurantId);
            if (!success) return NotFound("No se encontró el plato.");
            return Ok(new { Message = "Plato eliminado del menú." });
        }

        [HttpPost("assign-recipe")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> AssignRecipe([FromBody] AssignRecipeDTO recipeDto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await dishService.AssignRecipeAsync(recipeDto, restaurantId);
            if (!success) return BadRequest("Error al procesar la receta. Verifique el plato y los ingredientes.");
            return Ok(new { Message = "Receta guardada y porcionada correctamente." });
        }

        [HttpGet("shopping-list")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> GetShoppingList()
        {
            int restaurantId = GetCurrentRestaurantId();
            var shoppingList = await dishService.GetShoppingListAsync(restaurantId);
            return Ok(shoppingList);
        }
    }
}