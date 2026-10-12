using Microsoft.EntityFrameworkCore;
using SonqoAPI.Helpers;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Service
{
    public class DishService(
        IGenericRepository<Dish> dishRepository,
        IGenericRepository<RecipeDetails> recipeRepository,
        IGenericRepository<Ingredient> ingredientRepository) : IDishService
    {
        // 1. OBTENEMOS LOS PLATOS Y CALCULAMOS SU COSTO AL VUELO
        public async Task<IEnumerable<DishResponseDTO>> GetAllActiveDishesAsync()
        {
            var dishes = await dishRepository.GetAllAsync(d => d.IsAvailable);
            var recipes = await recipeRepository.GetAllAsync();
            var ingredients = await ingredientRepository.GetAllAsync();

            var responseList = new List<DishResponseDTO>();

            foreach (var dish in dishes)
            {
                var dishRecipes = recipes.Where(r => r.DishId == dish.Id);
                decimal totalCost = 0;
                int maxAvailable = int.MaxValue;

                if (!dishRecipes.Any()) maxAvailable = 0;

                foreach (var detail in dishRecipes)
                {
                    var ing = ingredients.FirstOrDefault(i => i.Id == detail.IngredientId);
                    if (ing != null)
                    {
                        totalCost += detail.RequiredQuantity * ing.UnitCost;

                        if (detail.RequiredQuantity > 0)
                        {
                            int portions = dish.YieldPortions > 0 ? dish.YieldPortions : 1;
                            decimal requiredPerPlate = detail.RequiredQuantity / portions;
                            int possiblePlates = (int)Math.Floor(ing.CurrentStock / requiredPerPlate);

                            if (possiblePlates < maxAvailable)
                            {
                                maxAvailable = possiblePlates;
                            }
                        }
                    }
                }

                if (maxAvailable == int.MaxValue) maxAvailable = 0;

                decimal margin = dish.Price > 0 ? ((dish.Price - totalCost) / dish.Price) * 100m : 0m;

                responseList.Add(new DishResponseDTO
                {
                    Id = dish.Id,
                    Name = dish.Name,
                    Category = dish.Category,
                    Price = dish.Price,
                    YieldPortions = dish.YieldPortions > 0 ? dish.YieldPortions : 1,
                    RecipeCost = Math.Round(totalCost, 2),
                    ProfitMargin = Math.Round(margin, 2),
                    MaxAvailable = maxAvailable
                });
            }

            return responseList;
        }

        public async Task<Dish?> GetDishByIdAsync(int id)
        {
            return await dishRepository.GetByIdAsync(id);
        }

        public async Task<AssignRecipeDTO?> GetRecipeByDishIdAsync(int dishId)
        {
            var dish = await dishRepository.GetByIdAsync(dishId);
            if (dish == null) return null;

            var recipeDetails = await recipeRepository.GetAllAsync(r => r.DishId == dishId);
            var ingredients = await ingredientRepository.GetAllAsync();

            var recipeDto = new AssignRecipeDTO
            {
                DishId = dish.Id,
                YieldPortions = dish.YieldPortions > 0 ? dish.YieldPortions : 1,
                Items = [.. recipeDetails.Select(detail =>
                {
                    var ing = ingredients.FirstOrDefault(i => i.Id == detail.IngredientId);
                    return new RecipeItemDTO
                    {
                        IngredientId = detail.IngredientId,
                        TotalQuantity = detail.RequiredQuantity,
                        Unit = ing != null ? ing.UnitOfMeasure : "kg"
                    };
                })]
            };

            return recipeDto;
        }

        public async Task<Dish> CreateDishAsync(DishUpdateDTO dishDto)
        {
            var newDish = new Dish
            {
                Name = dishDto.Name,
                Category = dishDto.Category,
                Price = dishDto.Price,
                IsAvailable = true
            };

            await dishRepository.AddAsync(newDish);
            await dishRepository.SaveChangesAsync();

            return newDish;
        }

        public async Task<bool> UpdateDishAsync(int id, DishUpdateDTO dishDto)
        {
            var dish = await dishRepository.GetByIdAsync(id);
            if (dish == null || !dish.IsAvailable) return false;

            dish.Name = dishDto.Name;
            dish.Category = dishDto.Category;
            dish.Price = dishDto.Price;

            dishRepository.Update(dish);
            return await dishRepository.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteDishAsync(int id)
        {
            var dish = await dishRepository.GetByIdAsync(id);
            if (dish == null) return false;

            dish.IsAvailable = false;
            dishRepository.Update(dish);

            return await dishRepository.SaveChangesAsync();
        }

        // 2. CONVERTIMOS LA UNIDAD ANTES DE GUARDAR
        public async Task<bool> AssignRecipeAsync(AssignRecipeDTO recipeDto)
        {
            var dish = await dishRepository.GetByIdAsync(recipeDto.DishId);
            if (dish == null) return false;

            // 1. Guardamos las porciones del plato
            dish.YieldPortions = recipeDto.YieldPortions > 0 ? recipeDto.YieldPortions : 1;
            dishRepository.Update(dish);
            await dishRepository.SaveChangesAsync();

            // 2. Obtenemos y borramos la receta anterior
            var existingRecipe = await recipeRepository.GetAllAsync(r => r.DishId == recipeDto.DishId);
            foreach (var detail in existingRecipe)
            {
                recipeRepository.Delete(detail);
            }

            await recipeRepository.SaveChangesAsync();

            // 3. Agregamos los nuevos ingredientes limpios de conflictos
            foreach (var item in recipeDto.Items)
            {
                decimal quantityPerPortion = item.TotalQuantity;

                var ingredient = await ingredientRepository.GetByIdAsync(item.IngredientId);
                if (ingredient == null) continue;

                decimal finalQuantity = UnitConverter.ConvertQuantityToBase(quantityPerPortion, item.Unit, ingredient.UnitOfMeasure);
                finalQuantity = Math.Round(finalQuantity, 3);

                var newDetail = new RecipeDetails
                {
                    DishId = recipeDto.DishId,
                    IngredientId = item.IngredientId,
                    RequiredQuantity = finalQuantity
                };

                await recipeRepository.AddAsync(newDetail);
            }

            // 4. Guardamos las nuevas inserciones exitosamente
            return await recipeRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<ShoppingListItemDTO>> GetShoppingListAsync()
        {
            var dishes = await dishRepository.GetAllAsync(d => d.IsAvailable);
            var recipes = await recipeRepository.GetAllAsync();
            var ingredients = await ingredientRepository.GetAllAsync();

            // Diccionario para acumular lo requerido globalmente por ingrediente
            var requiredMap = new Dictionary<int, decimal>();

            foreach (var dish in dishes)
            {
                int portions = dish.YieldPortions > 0 ? dish.YieldPortions : 1;
                var dishRecipes = recipes.Where(r => r.DishId == dish.Id);

                foreach (var detail in dishRecipes)
                {
                    decimal totalNeededForDish = detail.RequiredQuantity * portions;

                    if (requiredMap.ContainsKey(detail.IngredientId))
                    {
                        requiredMap[detail.IngredientId] += totalNeededForDish;
                    }
                    else
                    {
                        requiredMap[detail.IngredientId] = totalNeededForDish;
                    }
                }
            }

            var shoppingList = new List<ShoppingListItemDTO>();

            foreach (var kvp in requiredMap)
            {
                int ingredientId = kvp.Key;
                decimal totalRequired = kvp.Value;

                var ing = ingredients.FirstOrDefault(i => i.Id == ingredientId);
                if (ing == null) continue;

                decimal currentStock = ing.CurrentStock;

                if (totalRequired > currentStock)
                {
                    decimal missing = totalRequired - currentStock;
                    decimal unitCost = ing.UnitCost;

                    shoppingList.Add(new ShoppingListItemDTO
                    {
                        IngredientId = ing.Id,
                        IngredientName = ing.Name,
                        CurrentStock = Math.Round(currentStock, 3),
                        RequiredStock = Math.Round(totalRequired, 3),
                        MissingQuantity = Math.Round(missing, 3),
                        Unit = ing.UnitOfMeasure,
                        EstimatedCost = Math.Round(missing * unitCost, 2)
                    });
                }
            }

            return shoppingList;
        }
    }
}