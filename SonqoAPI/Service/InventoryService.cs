using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SonqoAPI.Service
{
    public class InventoryService(
        IGenericRepository<Dish> dishRepository,
        IGenericRepository<Ingredient> ingredientRepository,
        IGenericRepository<RecipeDetails> recipeRepository) : IInventoryService
    {
        public async Task<List<CalculatedIngredientDTO>> CalculatePlanningAsync(List<PlanningRequestDTO> planning, int restaurantId)
        {
            var dishIds = planning.Select(p => p.DishId).ToList();

            var validDishes = await dishRepository.GetAllAsync(d => dishIds.Contains(d.Id) && d.RestaurantId == restaurantId);
            var validDishIds = validDishes.Select(d => d.Id).ToList();

            var recipesList = await recipeRepository.GetAllAsync(
                filter: r => validDishIds.Contains(r.DishId),
                includes: r => r.Ingredient!
            );

            var recipes = recipesList.ToList();
            if (recipes.Count == 0) return [];

            var totalIngredients = new Dictionary<int, CalculatedIngredientDTO>();
            foreach (var plannedItem in planning)
            {
                var dishIngredients = recipes.Where(r => r.DishId == plannedItem.DishId).ToList();

                var dish = validDishes.FirstOrDefault(d => d.Id == plannedItem.DishId);
                int portions = (dish != null && dish.YieldPortions > 0) ? dish.YieldPortions : 1;

                foreach (var recipeDetail in dishIngredients)
                {
                    if (recipeDetail.Ingredient == null || recipeDetail.Ingredient.RestaurantId != restaurantId) continue;

                    var ingredientId = recipeDetail.IngredientId;

                    decimal requiredPerPlate = recipeDetail.RequiredQuantity / portions;
                    decimal requiredQuantity = requiredPerPlate * plannedItem.PlannedQuantity;

                    if (totalIngredients.TryGetValue(ingredientId, out CalculatedIngredientDTO? value))
                    {
                        value.TotalQuantityRequired += requiredQuantity;
                    }
                    else
                    {
                        totalIngredients.Add(ingredientId, new CalculatedIngredientDTO
                        {
                            IngredientId = ingredientId,
                            Name = recipeDetail.Ingredient.Name,
                            BaseUnitOfMeasure = recipeDetail.Ingredient.UnitOfMeasure,
                            TotalQuantityRequired = requiredQuantity,
                            CurrentStock = recipeDetail.Ingredient.CurrentStock
                        });
                    }
                }
            }

            return [.. totalIngredients.Values.Select(i => {
        i.SufficientStock = i.CurrentStock >= i.TotalQuantityRequired;
        return i;
    })];
        }

        public async Task<bool> UpdateIngredientAsync(int id, IngredientCreateDTO dto, int restaurantId)
        {
            var ingredient = await ingredientRepository.GetByIdAsync(id);
            if (ingredient == null || ingredient.RestaurantId != restaurantId) return false;

            if (ingredient.CurrentStock > 0 && !ingredient.UnitOfMeasure.Equals(dto.BaseUnit, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            ingredient.Name = dto.Name;
            ingredient.Category = dto.Category;
            ingredient.UnitOfMeasure = dto.BaseUnit;
            ingredient.MinimumStock = dto.MinimumStock;
            ingredient.IsPerishable = dto.IsPerishable;
            ingredient.ExpirationDate = dto.ExpirationDate?.ToUniversalTime();

            ingredientRepository.Update(ingredient);
            return await ingredientRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteIngredientAsync(int id, int restaurantId)
        {
            var ingredient = await ingredientRepository.GetByIdAsync(id);
            if (ingredient == null || ingredient.RestaurantId != restaurantId) return false;

            ingredient.IsActive = false;
            ingredientRepository.Update(ingredient);
            return await ingredientRepository.SaveChangesAsync();
        }

        public async Task<bool> SeedTestDataAsync(int restaurantId)
        {
            var hasDishes = await dishRepository.ExistsAsync(d => d.RestaurantId == restaurantId);
            if (hasDishes) return false;

            var meat = new Ingredient { Name = "Beef", UnitOfMeasure = "g", CurrentStock = 5000, MinimumStock = 1000, RestaurantId = restaurantId };
            var onion = new Ingredient { Name = "Onion", UnitOfMeasure = "g", CurrentStock = 3000, MinimumStock = 500, RestaurantId = restaurantId };
            var potato = new Ingredient { Name = "Yellow Potato", UnitOfMeasure = "g", CurrentStock = 10000, MinimumStock = 2000, RestaurantId = restaurantId };

            await ingredientRepository.AddRangeAsync([meat, onion, potato]);
            await ingredientRepository.SaveChangesAsync();

            var lomoSaltado = new Dish { Name = "Lomo Saltado", Category = "Main Course", Price = 35.50m, IsAvailable = true, RestaurantId = restaurantId };
            await dishRepository.AddAsync(lomoSaltado);
            await dishRepository.SaveChangesAsync();

            var recipeMeat = new RecipeDetails { DishId = lomoSaltado.Id, IngredientId = meat.Id, RequiredQuantity = 200 };
            var recipeOnion = new RecipeDetails { DishId = lomoSaltado.Id, IngredientId = onion.Id, RequiredQuantity = 100 };
            var recipePotato = new RecipeDetails { DishId = lomoSaltado.Id, IngredientId = potato.Id, RequiredQuantity = 300 };

            await recipeRepository.AddRangeAsync([recipeMeat, recipeOnion, recipePotato]);
            await recipeRepository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Ingredient>> GetAllIngredientsAsync(int restaurantId)
        {
            return await ingredientRepository.GetAllAsync(i => i.IsActive && i.RestaurantId == restaurantId);
        }

        public async Task<Ingredient> CreateIngredientAsync(IngredientCreateDTO dto, int restaurantId)
        {
            var newIngredient = new Ingredient
            {
                Name = dto.Name,
                Category = dto.Category,
                UnitOfMeasure = dto.BaseUnit,
                CurrentStock = dto.InitialStock,
                MinimumStock = dto.MinimumStock,
                UnitCost = dto.InitialCost,
                IsPerishable = dto.IsPerishable,
                ExpirationDate = dto.ExpirationDate?.ToUniversalTime(),
                RestaurantId = restaurantId
            };

            await ingredientRepository.AddAsync(newIngredient);
            await ingredientRepository.SaveChangesAsync();
            return newIngredient;
        }

        public async Task<bool> RestockIngredientAsync(RestockDTO dto, int restaurantId)
        {
            if (dto.QuantityAdded <= 0 || dto.TotalCostPaid < 0) return false;

            var ingredient = await ingredientRepository.GetByIdAsync(dto.IngredientId);
            if (ingredient == null || ingredient.RestaurantId != restaurantId) return false;

            var effectiveCurrentStock = ingredient.CurrentStock > 0 ? ingredient.CurrentStock : 0;
            var currentTotalValue = effectiveCurrentStock * ingredient.UnitCost;
            var newTotalValue = currentTotalValue + dto.TotalCostPaid;
            var newTotalStock = effectiveCurrentStock + dto.QuantityAdded;

            ingredient.CurrentStock = newTotalStock;
            ingredient.UnitCost = newTotalValue / newTotalStock;

            if (ingredient.IsPerishable && dto.NewExpirationDate.HasValue)
            {
                ingredient.ExpirationDate = dto.NewExpirationDate.Value.ToUniversalTime();
            }

            ingredientRepository.Update(ingredient);
            return await ingredientRepository.SaveChangesAsync();
        }
    }
}