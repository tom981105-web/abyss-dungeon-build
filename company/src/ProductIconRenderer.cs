using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductIconRenderer
{
    private readonly ModEntry Mod;
    private Texture2D? Atlas;

    private const int CellSize = 64;
    private const int TemplateCount = 10;
    private const string AtlasAssetPath = "assets/product_icon_templates.png";

    internal ProductIconRenderer(ModEntry mod)
    {
        Mod = mod;
    }

    internal void DrawRecipeIcon(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle bounds, float alpha = 1f)
    {
        Texture2D atlas = GetAtlas();
        int index = Math.Clamp(GetTemplateIndex(recipe), 0, TemplateCount - 1);
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

        try
        {
            Atlas = Mod.Helper.ModContent.Load<Texture2D>(AtlasAssetPath);
            if (Atlas.Width < CellSize * TemplateCount || Atlas.Height < CellSize)
                throw new InvalidOperationException($"Product icon atlas has invalid dimensions {Atlas.Width}x{Atlas.Height}.");
            return Atlas;
        }
        catch (Exception ex)
        {
            Mod.Monitor.Log($"Could not load {AtlasAssetPath}; using a safe generated fallback atlas instead. {ex.Message}", StardewModdingAPI.LogLevel.Warn);
            Atlas = CreateFallbackAtlas();
            return Atlas;
        }
    }

    private static Texture2D CreateFallbackAtlas()
    {
        int width = CellSize * TemplateCount;
        Texture2D texture = new(Game1.graphics.GraphicsDevice, width, CellSize);
        Color[] pixels = new Color[width * CellSize];
        Color[] fills =
        {
            new(202, 70, 48), new(221, 164, 58), new(166, 65, 43), new(208, 178, 106), new(102, 145, 72),
            new(218, 127, 151), new(225, 151, 67), new(216, 166, 48), new(88, 136, 164), new(169, 116, 66)
        };

        for (int cell = 0; cell < TemplateCount; cell++)
        {
            Color fill = fills[cell];
            int startX = cell * CellSize;
            for (int y = 0; y < CellSize; y++)
            {
                for (int x = 0; x < CellSize; x++)
                {
                    bool border = x < 4 || y < 4 || x >= CellSize - 4 || y >= CellSize - 4;
                    Color c = border ? new Color(91, 60, 34) : fill;
                    if (!border && ((x + y + cell * 3) % 17 == 0))
                        c = Color.Lerp(fill, Color.White, 0.25f);
                    pixels[y * width + startX + x] = c;
                }
            }
        }

        texture.SetData(pixels);
        return texture;
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
        catch
        {
            // A missing optional crop icon must never take down the company UI.
        }
    }
}
