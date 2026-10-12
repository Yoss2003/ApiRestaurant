namespace SonqoAPI.Helpers
{
    public static class UnitConverter
    {
        public static decimal ConvertQuantityToBase(decimal quantity, string recipeUnit, string inventoryBaseUnit)
        {
            if (string.IsNullOrWhiteSpace(recipeUnit) || string.IsNullOrWhiteSpace(inventoryBaseUnit))
                return quantity;

            recipeUnit = recipeUnit.ToLower().Trim();
            inventoryBaseUnit = inventoryBaseUnit.ToLower().Trim();

            if (recipeUnit == inventoryBaseUnit) return quantity;

            // De Gramos/Mililitros a Kilos/Litros
            if ((recipeUnit == "g" && inventoryBaseUnit == "kg") ||
                (recipeUnit == "ml" && inventoryBaseUnit == "l"))
                return quantity / 1000m;

            // De Kilos/Litros a Gramos/Mililitros (por si acaso)
            if ((recipeUnit == "kg" && inventoryBaseUnit == "g") ||
                (recipeUnit == "l" && inventoryBaseUnit == "ml"))
                return quantity * 1000m;

            return quantity; // Si es 'unidades', se queda igual
        }
    }
}