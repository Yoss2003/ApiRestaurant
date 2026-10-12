using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;

namespace SonqoAPI.Service.IService
{
    public interface IDishService
    {
        Task<IEnumerable<DishResponseDTO>> GetAllActiveDishesAsync();
        Task<Dish?> GetDishByIdAsync(int id);
        Task<AssignRecipeDTO?> GetRecipeByDishIdAsync(int dishId);
        Task<Dish> CreateDishAsync(DishUpdateDTO dishDto);
        Task<bool> UpdateDishAsync(int id, DishUpdateDTO dishDto);
        Task<bool> SoftDeleteDishAsync(int id);
        Task<bool> AssignRecipeAsync(AssignRecipeDTO recipeDto);
        Task<IEnumerable<ShoppingListItemDTO>> GetShoppingListAsync();
    }
}