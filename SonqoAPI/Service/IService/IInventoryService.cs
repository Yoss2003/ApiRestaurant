using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;

namespace SonqoAPI.Service.IService
{
    public interface IInventoryService
    {
        Task<List<CalculatedIngredientDTO>> CalculatePlanningAsync(List<PlanningRequestDTO> planning);
        Task<bool> SeedTestDataAsync();
        Task<IEnumerable<Ingredient>> GetAllIngredientsAsync();
        Task<Ingredient> CreateIngredientAsync(IngredientCreateDTO dto);
        Task<bool> RestockIngredientAsync(RestockDTO dto);
        Task<bool> UpdateIngredientAsync(int id, IngredientCreateDTO dto);
        Task<bool> DeleteIngredientAsync(int id);
    }
}
