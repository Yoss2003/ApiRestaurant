using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Models;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Service.IService;
using System.Security.Claims;

namespace RestaurantApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DishesController(IDishService dishService, IAuditService auditService) : ControllerBase
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

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Dish>>> GetAll([FromQuery] int? restaurantId)
        {
            int resolvedRestaurantId = restaurantId ?? GetCurrentRestaurantId();
            if (resolvedRestaurantId <= 0) resolvedRestaurantId = 1;

            var dishes = await dishService.GetAllActiveDishesAsync(resolvedRestaurantId);
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
            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "CREATE", "Dish", createdDish.Id.ToString(), $"Creó el plato '{dishDto.Name}' con precio S/ {dishDto.Price}");
            return Ok(createdDish);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireKitchen")]
        public async Task<IActionResult> Update(int id, [FromBody] DishUpdateDTO dishDto)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await dishService.UpdateDishAsync(id, dishDto, restaurantId);
            if (!success) return NotFound("Plato no encontrado o inactivo.");
            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "UPDATE", "Dish", id.ToString(), $"Modificó el plato '{dishDto.Name}'. Nuevo precio: S/ {dishDto.Price}");
            return Ok(new { Message = "Plato actualizado correctamente." });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            int restaurantId = GetCurrentRestaurantId();
            var success = await dishService.SoftDeleteDishAsync(id, restaurantId);
            if (!success) return NotFound("No se encontró el plato.");
            await auditService.LogActionAsync(restaurantId, GetCurrentUserId(), "DELETE", "Dish", id.ToString(), "Eliminó un plato del menú");
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