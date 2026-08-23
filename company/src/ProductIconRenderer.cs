using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductIconRenderer
{
    private readonly ModEntry Mod;
    private Texture2D? Atlas;

    private const int CellSize = 64;
    private const string AtlasBase64 = "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";

    internal ProductIconRenderer(ModEntry mod)
    {
        Mod = mod;
    }

    internal void DrawRecipeIcon(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle bounds, float alpha = 1f)
    {
        Texture2D atlas = GetAtlas();
        int index = GetTemplateIndex(recipe);
        Rectangle source = new(index * CellSize, 0, CellSize, CellSize);
        b.Draw(atlas, bounds, source, Color.White * alpha);

        if (IsPremium(recipe))
            DrawPremiumFrame(b, bounds, alpha);

        string? sourceItemId = ResolveSourceItemId(recipe);
        if (!string.IsNullOrWhiteSpace(sourceItemId))
            DrawIngredientBadge(b, sourceItemId, bounds, alpha);
    }

    internal void DrawProductIcon(SpriteBatch b, string productOrIntermediateKey, Rectangle bounds, float alpha = 1f)
    {
        ProductionRecipeDefinition? recipe = Mod.Recipes.FirstOrDefault(p =>
            string.Equals(p.Key, productOrIntermediateKey, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(p.OutputIntermediateKey)
                && string.Equals(p.OutputIntermediateKey, productOrIntermediateKey, StringComparison.OrdinalIgnoreCase)));
        if (recipe is not null)
            DrawRecipeIcon(b, recipe, bounds, alpha);
    }

    private Texture2D GetAtlas()
    {
        if (Atlas is not null && !Atlas.IsDisposed)
            return Atlas;
        byte[] bytes = Convert.FromBase64String(AtlasBase64);
        using MemoryStream stream = new(bytes);
        Atlas = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
        return Atlas;
    }

    private int GetTemplateIndex(ProductionRecipeDefinition recipe)
    {
        if (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase)) return 9;
        string name = recipe.DisplayName ?? "";
        if (name.Contains("연구", StringComparison.CurrentCultureIgnoreCase)) return 8;
        if (name.Contains("부케", StringComparison.CurrentCultureIgnoreCase)) return 5;
        if (name.Contains("세트", StringComparison.CurrentCultureIgnoreCase) || name.Contains("선물", StringComparison.CurrentCultureIgnoreCase)) return 7;
        if (name.Contains("퓌레", StringComparison.CurrentCultureIgnoreCase)) return 6;
        if (name.Contains("신선팩", StringComparison.CurrentCultureIgnoreCase) || name.Contains("채소팩", StringComparison.CurrentCultureIgnoreCase) || name.Contains("원료팩", StringComparison.CurrentCultureIgnoreCase) || name.Contains("가공팩", StringComparison.CurrentCultureIgnoreCase)) return 4;
        if (name.Contains("분말", StringComparison.CurrentCultureIgnoreCase) || name.Contains("가루", StringComparison.CurrentCultureIgnoreCase) || name.Contains("전분", StringComparison.CurrentCultureIgnoreCase) || name.Contains("쌀팩", StringComparison.CurrentCultureIgnoreCase) || name.Contains("곡물팩", StringComparison.CurrentCultureIgnoreCase) || name.Contains("커피팩", StringComparison.CurrentCultureIgnoreCase) || name.Contains("녹차팩", StringComparison.CurrentCultureIgnoreCase)) return 3;
        if (name.Contains("소스", StringComparison.CurrentCultureIgnoreCase) || name.Contains("케첩", StringComparison.CurrentCultureIgnoreCase) || name.Contains("시럽", StringComparison.CurrentCultureIgnoreCase)) return 2;
        if (name.Contains("잼", StringComparison.CurrentCultureIgnoreCase) || name.Contains("프리저브", StringComparison.CurrentCultureIgnoreCase) || name.Contains("피클", StringComparison.CurrentCultureIgnoreCase) || name.Contains("절임", StringComparison.CurrentCultureIgnoreCase)) return 1;
        if (recipe.LineType == "Packaging") return 4;
        return 0;
    }

    private string? ResolveSourceItemId(ProductionRecipeDefinition recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientItemId)) return recipe.IngredientItemId;
        string family = recipe.ProductFamily ?? "";
        if (family == "Tomato") return "(O)256";
        string cropFamily = family switch { "Watermelon" => "Watermelon", "KoreanMelon" => "KoreanMelon", "NapaCabbage" => "NapaCabbage", _ => "" };
        if (!string.IsNullOrWhiteSpace(cropFamily)) return Mod.Crops.FirstOrDefault(p => string.Equals(p.Family, cropFamily, StringComparison.OrdinalIgnoreCase))?.ItemId;
        return null;
    }

    private static bool IsPremium(ProductionRecipeDefinition recipe)
        => recipe.RequiredBrandPoints >= 100 || (recipe.DisplayName?.Contains("프리미엄", StringComparison.CurrentCultureIgnoreCase) ?? false);

    private static void DrawPremiumFrame(SpriteBatch b, Rectangle bounds, float alpha)
    {
        Color gold = new Color(224, 174, 62) * alpha;
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), gold);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), gold);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), gold);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), gold);
        Rectangle star = new(bounds.Right - 13, bounds.Y + 3, 9, 9);
        b.Draw(Game1.fadeToBlackRect, star, new Color(248, 211, 93) * alpha);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(star.X + 3, star.Y - 2, 3, 13), new Color(248, 211, 93) * alpha);
    }

    private static void DrawIngredientBadge(SpriteBatch b, string itemId, Rectangle bounds, float alpha)
    {
        try
        {
            Item item = ItemRegistry.Create(itemId, 1, allowNull: true);
            if (item is null) return;
            Rectangle badge = new(bounds.Right - 23, bounds.Bottom - 23, 21, 21);
            b.Draw(Game1.fadeToBlackRect, badge, new Color(250, 238, 198) * (0.94f * alpha));
            b.Draw(Game1.fadeToBlackRect, new Rectangle(badge.X, badge.Y, badge.Width, 1), new Color(94, 69, 42) * alpha);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(badge.X, badge.Bottom - 1, badge.Width, 1), new Color(94, 69, 42) * alpha);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(badge.X, badge.Y, 1, badge.Height), new Color(94, 69, 42) * alpha);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(badge.Right - 1, badge.Y, 1, badge.Height), new Color(94, 69, 42) * alpha);
            item.drawInMenu(b, new Vector2(badge.X + 2, badge.Y + 2), 0.8f, alpha, 0.95f, StackDrawType.Hide, Color.White, drawShadow: false);
        }
        catch { }
    }
}
