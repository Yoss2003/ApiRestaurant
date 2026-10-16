using Microsoft.EntityFrameworkCore;
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
        public async Task<IEnumerable<DishResponseDTO>> GetAllActiveDishesAsync(int restaurantId)
        {
            var dishes = await dishRepository.GetAllAsync(d => d.IsAvailable && d.RestaurantId == restaurantId);
            var recipes = await recipeRepository.GetAllAsync();
            var ingredients = await ingredientRepository.GetAllAsync(i => i.RestaurantId == restaurantId);

            var responseList = new List<DishResponseDTO>();
            foreach (var dish in dishes)
            {
                var dishRecipes = recipes.Where(r => r.DishId == dish.Id);
                decimal totalCost = 0;
                int maxAvailable = int.MaxValue;
                if (!dishRecipes.Any()) maxAvailable = 0;

                int portions = dish.YieldPortions > 0 ? dish.YieldPortions : 1;

                foreach (var detail in dishRecipes)
                {
                    var ing = ingredients.FirstOrDefault(i => i.Id == detail.IngredientId);
                    if (ing != null)
                    {
                        totalCost += detail.RequiredQuantity * ing.UnitCost;
                        if (detail.RequiredQuantity > 0)
                        {
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

                decimal portionCost = totalCost / portions;
                decimal margin = dish.Price > 0 ? ((dish.Price - portionCost) / dish.Price) * 100m : 0m;

                responseList.Add(new DishResponseDTO
                {
                    Id = dish.Id,
                    Name = dish.Name,
                    Category = dish.Category,
                    Price = dish.Price,
                    YieldPortions = portions,
                    RecipeCost = Math.Round(portionCost, 2),
                    ProfitMargin = Math.Round(margin, 2),
                    MaxAvailable = maxAvailable
                });
            }
            return responseList;
        }

        public async Task<Dish?> GetDishByIdAsync(int id, int restaurantId)
        {
            var dish = await dishRepository.GetByIdAsync(id);
            if (dish != null && dish.RestaurantId == restaurantId)
            {
                return dish;
            }
            return null;
        }

        public async Task<AssignRecipeDTO?> GetRecipeByDishIdAsync(int dishId, int restaurantId)
        {
            var dish = await dishRepository.GetByIdAsync(dishId);
            if (dish == null || dish.RestaurantId != restaurantId) return null;

            var recipeDetails = await recipeRepository.GetAllAsync(r => r.DishId == dishId);
            var ingredients = await ingredientRepository.GetAllAsync(i => i.RestaurantId == restaurantId);

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

        public async Task<Dish> CreateDishAsync(DishUpdateDTO dishDto, int restaurantId)
        {
            var newDish = new Dish
            {
                Name = dishDto.Name,
                Category = dishDto.Category,
                Price = dishDto.Price,
                IsAvailable = true,
                RestaurantId = restaurantId
            };
            await dishRepository.AddAsync(newDish);
            await dishRepository.SaveChangesAsync();
            return newDish;
        }

        public async Task<bool> UpdateDishAsync(int id, DishUpdateDTO dishDto, int restaurantId)
        {
            var dish = await dishRepository.GetByIdAsync(id);
            if (dish == null || !dish.IsAvailable || dish.RestaurantId != restaurantId) return false;

            dish.Name = dishDto.Name;
            dish.Category = dishDto.Category;
            dish.Price = dishDto.Price;
            dishRepository.Update(dish);
            return await dishRepository.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteDishAsync(int id, int restaurantId)
        {
            var dish = await dishRepository.GetByIdAsync(id);
            if (dish == null || dish.RestaurantId != restaurantId) return false;

            dish.IsAvailable = false;
            dishRepository.Update(dish);
            return await dishRepository.SaveChangesAsync();
        }

        public async Task<bool> AssignRecipeAsync(AssignRecipeDTO recipeDto, int restaurantId)
        {
            var dish = await dishRepository.GetByIdAsync(recipeDto.DishId);
            if (dish == null || dish.RestaurantId != restaurantId) return false;

            if (recipeDto.Items != null && recipeDto.Items.Count > 0)
            {
                var ingredientIds = recipeDto.Items.Select(i => i.IngredientId).Distinct().ToList();
                var validIngredients = await ingredientRepository.GetQueryable()
                    .Where(i => ingredientIds.Contains(i.Id) && i.RestaurantId == restaurantId)
                    .ToListAsync();

                if (validIngredients.Count != ingredientIds.Count)
                {
                    return false;
                }

                using var transaction = await dishRepository.BeginTransactionAsync();
                try
                {
                    dish.YieldPortions = recipeDto.YieldPortions > 0 ? recipeDto.YieldPortions : 1;
                    dishRepository.Update(dish);

                    var existingRecipes = await recipeRepository.GetAllAsync(r => r.DishId == recipeDto.DishId);
                    foreach (var oldRecipe in existingRecipes)
                    {
                        recipeRepository.Delete(oldRecipe);
                    }
                    await recipeRepository.SaveChangesAsync();

                    foreach (var item in recipeDto.Items)
                    {
                        if (item.TotalQuantity <= 0) continue;

                        var ingredient = validIngredients.First(i => i.Id == item.IngredientId);

                        decimal normalizedQuantity = item.TotalQuantity;
                        string recipeUnit = (item.Unit ?? "").ToLower().Trim();
                        string baseUnit = (ingredient.UnitOfMeasure ?? "").ToLower().Trim();

                        if (recipeUnit != baseUnit)
                        {
                            if ((recipeUnit == "g" && baseUnit == "kg") || (recipeUnit == "ml" && baseUnit == "l"))
                            {
                                normalizedQuantity = item.TotalQuantity / 1000m;
                            }
                            else if ((recipeUnit == "kg" && baseUnit == "g") || (recipeUnit == "l" && baseUnit == "ml"))
                            {
                                normalizedQuantity = item.TotalQuantity * 1000m;
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return false;
                            }
                        }

                        normalizedQuantity = Math.Round(normalizedQuantity, 3, MidpointRounding.AwayFromZero);
                        if (normalizedQuantity <= 0) continue;

                        var newRecipeDetail = new RecipeDetails
                        {
                            DishId = recipeDto.DishId,
                            IngredientId = item.IngredientId,
                            RequiredQuantity = normalizedQuantity
                        };
                        await recipeRepository.AddAsync(newRecipeDetail);
                    }

                    await recipeRepository.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }

            return true;
        }

        public async Task<IEnumerable<ShoppingListItemDTO>> GetShoppingListAsync(int restaurantId)
        {
            var dishes = await dishRepository.GetAllAsync(d => d.IsAvailable && d.RestaurantId == restaurantId);
            var recipes = await recipeRepository.GetAllAsync();
            var ingredients = await ingredientRepository.GetAllAsync(i => i.RestaurantId == restaurantId);

            var requiredMap = new Dictionary<int, decimal>();
            foreach (var dish in dishes)
            {
                var dishRecipes = recipes.Where(r => r.DishId == dish.Id);
                foreach (var detail in dishRecipes)
                {
                    decimal totalNeededForDish = detail.RequiredQuantity;

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