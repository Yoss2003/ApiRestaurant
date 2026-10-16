using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonqoAPI.Service.IService
{
    public interface IInventoryService
    {
        Task<List<CalculatedIngredientDTO>> CalculatePlanningAsync(List<PlanningRequestDTO> planning, int restaurantId);
        Task<bool> SeedTestDataAsync(int restaurantId);
        Task<IEnumerable<Ingredient>> GetAllIngredientsAsync(int restaurantId);
        Task<Ingredient> CreateIngredientAsync(IngredientCreateDTO dto, int restaurantId);
        Task<bool> RestockIngredientAsync(RestockDTO dto, int restaurantId);
        Task<bool> UpdateIngredientAsync(int id, IngredientCreateDTO dto, int restaurantId);
        Task<bool> DeleteIngredientAsync(int id, int restaurantId);
    }
}