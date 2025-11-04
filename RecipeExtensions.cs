using Lumina.Excel.GeneratedSheets;

namespace SupplyMissionHelper;

public static class RecipeExtensions
{
    public static uint GetIngredientItemId(this Recipe recipe, int slot)
        => slot switch
        {
            0 => recipe.ItemIngredient0.RowId,
            1 => recipe.ItemIngredient1.RowId,
            2 => recipe.ItemIngredient2.RowId,
            3 => recipe.ItemIngredient3.RowId,
            4 => recipe.ItemIngredient4.RowId,
            5 => recipe.ItemIngredient5.RowId,
            6 => recipe.ItemIngredient6.RowId,
            7 => recipe.ItemIngredient7.RowId,
            8 => recipe.ItemIngredient8.RowId,
            9 => recipe.ItemIngredient9.RowId,
            _ => 0
        };

    public static int GetIngredientAmount(this Recipe recipe, int slot)
        => slot switch
        {
            0 => recipe.AmountIngredient0,
            1 => recipe.AmountIngredient1,
            2 => recipe.AmountIngredient2,
            3 => recipe.AmountIngredient3,
            4 => recipe.AmountIngredient4,
            5 => recipe.AmountIngredient5,
            6 => recipe.AmountIngredient6,
            7 => recipe.AmountIngredient7,
            8 => recipe.AmountIngredient8,
            9 => recipe.AmountIngredient9,
            _ => 0
        };
}
