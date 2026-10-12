using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Service
{
    public class InventoryService(
        IGenericRepository<Dish> dishRepository,
        IGenericRepository<Ingredient> ingredientRepository,
        IGenericRepository<RecipeDetails> recipeRepository) : IInventoryService
    {
        public async Task<List<CalculatedIngredientDTO>> CalculatePlanningAsync(List<PlanningRequestDTO> planning)
        {
            var dishIds = planning.Select(p => p.DishId).ToList();

            // Obtenemos las recetas usando el repositorio genérico, aplicando el filtro WHERE y el INCLUDE
            var recipesList = await recipeRepository.GetAllAsync(
                filter: r => dishIds.Contains(r.DishId),
                includes: r => r.Ingredient!
            );

            var recipes = recipesList.ToList();

            if (recipes.Count == 0)
            {
                return [];
            }

            var totalIngredients = new Dictionary<int, CalculatedIngredientDTO>();

            foreach (var plannedItem in planning)
            {
                var dishIngredients = recipes.Where(r => r.DishId == plannedItem.DishId).ToList();

                foreach (var recipeDetail in dishIngredients)
                {
                    if (recipeDetail.Ingredient == null) continue;

                    var ingredientId = recipeDetail.IngredientId;
                    var requiredQuantity = recipeDetail.RequiredQuantity * plannedItem.PlannedQuantity;

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

            var finalResult = totalIngredients.Values.Select(i => {
                i.SufficientStock = i.CurrentStock >= i.TotalQuantityRequired;
                return i;
            }).ToList();

            return finalResult;
        }

        public async Task<bool> UpdateIngredientAsync(int id, IngredientCreateDTO dto)
        {
            var ingredient = await ingredientRepository.GetByIdAsync(id);
            if (ingredient == null) return false;

            ingredient.Name = dto.Name;
            ingredient.Category = dto.Category;
            ingredient.UnitOfMeasure = dto.BaseUnit;
            ingredient.CurrentStock = dto.InitialStock;
            ingredient.MinimumStock = dto.MinimumStock;
            ingredient.UnitCost = dto.InitialCost;
            ingredient.IsPerishable = dto.IsPerishable;
            ingredient.ExpirationDate = dto.ExpirationDate?.ToUniversalTime();

            ingredientRepository.Update(ingredient);
            return await ingredientRepository.SaveChangesAsync();
        }

        public async Task<bool> DeleteIngredientAsync(int id)
        {
            var ingredient = await ingredientRepository.GetByIdAsync(id);
            if (ingredient == null) return false;

            ingredient.IsActive = false;
            ingredientRepository.Update(ingredient);
            return await ingredientRepository.SaveChangesAsync();
        }

        public async Task<bool> SeedTestDataAsync()
        {
            // Verificamos si ya existen platos usando el repositorio
            var hasDishes = await dishRepository.ExistsAsync(d => true);
            if (hasDishes) return false;

            // 1. Crear Ingredientes
            var meat = new Ingredient { Name = "Beef", UnitOfMeasure = "g", CurrentStock = 5000, MinimumStock = 1000 };
            var onion = new Ingredient { Name = "Onion", UnitOfMeasure = "g", CurrentStock = 3000, MinimumStock = 500 };
            var potato = new Ingredient { Name = "Yellow Potato", UnitOfMeasure = "g", CurrentStock = 10000, MinimumStock = 2000 };

            await ingredientRepository.AddRangeAsync([meat, onion, potato]);
            await ingredientRepository.SaveChangesAsync();

            // 2. Crear un Plato
            var lomoSaltado = new Dish { Name = "Lomo Saltado", Category = "Main Course", Price = 35.50m, IsAvailable = true };

            await dishRepository.AddAsync(lomoSaltado);
            await dishRepository.SaveChangesAsync();

            // 3. Crear la Receta
            var recipeMeat = new RecipeDetails { DishId = lomoSaltado.Id, IngredientId = meat.Id, RequiredQuantity = 200 };
            var recipeOnion = new RecipeDetails { DishId = lomoSaltado.Id, IngredientId = onion.Id, RequiredQuantity = 100 };
            var recipePotato = new RecipeDetails { DishId = lomoSaltado.Id, IngredientId = potato.Id, RequiredQuantity = 300 };

            await recipeRepository.AddRangeAsync([recipeMeat, recipeOnion, recipePotato]);
            await recipeRepository.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<Ingredient>> GetAllIngredientsAsync()
        {
            return await ingredientRepository.GetAllAsync(i => i.IsActive);
        }

        public async Task<Ingredient> CreateIngredientAsync(IngredientCreateDTO dto)
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
                ExpirationDate = dto.ExpirationDate?.ToUniversalTime()
            };

            await ingredientRepository.AddAsync(newIngredient);
            await ingredientRepository.SaveChangesAsync();
            return newIngredient;
        }

        public async Task<bool> RestockIngredientAsync(RestockDTO dto)
        {
            if (dto.QuantityAdded <= 0 || dto.TotalCostPaid < 0) return false;

            var ingredient = await ingredientRepository.GetByIdAsync(dto.IngredientId);
            if (ingredient == null) return false;

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