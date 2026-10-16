using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonqoAPI.Service.IService
{
    public interface IDishService
    {
        Task<IEnumerable<DishResponseDTO>> GetAllActiveDishesAsync(int restaurantId);
        Task<Dish?> GetDishByIdAsync(int id, int restaurantId);
        Task<AssignRecipeDTO?> GetRecipeByDishIdAsync(int dishId, int restaurantId);
        Task<Dish> CreateDishAsync(DishUpdateDTO dishDto, int restaurantId);
        Task<bool> UpdateDishAsync(int id, DishUpdateDTO dishDto, int restaurantId);
        Task<bool> SoftDeleteDishAsync(int id, int restaurantId);
        Task<bool> AssignRecipeAsync(AssignRecipeDTO recipeDto, int restaurantId);
        Task<IEnumerable<ShoppingListItemDTO>> GetShoppingListAsync(int restaurantId);
    }
}