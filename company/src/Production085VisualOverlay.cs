using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>
/// 0.8.5 visual-fidelity layer. The 0.8.4 layout remains the functional fallback,
/// while this renderer replaces primitive machine/process/product placeholders with
/// authored pixel-art PNG sprites and adds subtle textured timber rails.
/// Missing assets never close the menu: the 0.8.4 drawings remain visible.
/// </summary>
internal sealed class Production085VisualOverlay
{
    private const int DesignW = 1400;
    private const int DesignH = 820;
    private const int Cell = 96;

    private readonly ModEntry Mod;
    private Texture2D? VisualAtlas;
    private Texture2D? UiTiles;
    private bool LoadTried;
    private bool Warned;

    private static readonly FieldInfo? ProductionSelected = typeof(Production084Menu).GetField("SelectedRecipeKey", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CatalogPage = typeof(ProductCatalog084Menu).GetField("Page", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CatalogFilter = typeof(ProductCatalog084Menu).GetField("Filter", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CatalogSelected = typeof(ProductCatalog084Menu).GetField("SelectedKey", BindingFlags.Instance | BindingFlags.NonPublic);

    internal Production085VisualOverlay(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!TryLoad())
            return;

        if (Game1.activeClickableMenu is Production084Menu production)
            DrawProduction(e.SpriteBatch, production);
        else if (Game1.activeClickableMenu is ProductCatalog084Menu catalog)
            DrawCatalog(e.SpriteBatch, catalog);
    }

    private bool TryLoad()
    {
        if (VisualAtlas is not null && !VisualAtlas.IsDisposed)
            return true;
        if (LoadTried)
            return false;

        LoadTried = true;
        try
        {
            VisualAtlas = Mod.Helper.ModContent.Load<Texture2D>("assets/production_visuals_085.png");
            UiTiles = Mod.Helper.ModContent.Load<Texture2D>("assets/ui_tiles_085.png");
            if (VisualAtlas.Width < Cell * 4 || VisualAtlas.Height < Cell * 4)
                throw new InvalidOperationException($"production_visuals_085.png size {VisualAtlas.Width}x{VisualAtlas.Height} is smaller than 384x384.");
            return true;
        }
        catch (Exception ex)
        {
            if (!Warned)
            {
                Warned = true;
                Mod.Monitor.Log($"0.8.5 visual assets could not be loaded; keeping the safe 0.8.4 fallback visuals. {ex.Message}", StardewModdingAPI.LogLevel.Warn);
            }
            VisualAtlas = null;
            UiTiles = null;
            return false;
        }
    }

    private void DrawProduction(SpriteBatch b, Production084Menu menu)
    {
        var t = Transform();
        DrawFrameTexture(b, t);

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < Math.Min(3, lines.Count); i++)
        {
            ProductionLineState line = lines[i];
            int sprite = line.LineType switch
            {
                "Fermentation" => 1,
                "Packaging" => 2,
                _ => 0
            };
            Rectangle machine = D(t, 51, 266 + i * 126, 112, 77);
            PatchPaper(b, machine, new Color(250, 235, 197));
            DrawSprite(b, sprite, machine, 1f);

            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            ProductionRecipeDefinition? recipe = job is null ? null : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is not null && TrySpecialProductIndex(recipe, out int productIndex))
            {
                Rectangle icon = D(t, 171, 268 + i * 126, 45, 45);
                PatchPaper(b, icon, new Color(250, 235, 197));
                DrawSprite(b, productIndex, icon, 1f);
            }
        }

        string selectedKey = ProductionSelected?.GetValue(menu) as string ?? "";
        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(selectedKey) ?? Mod.Recipes.FirstOrDefault();
        if (selected is null)
            return;

        if (TrySpecialProductIndex(selected, out int selectedIndex))
        {
            Rectangle topProduct = D(t, 593, 231, 57, 57);
            PatchPaper(b, topProduct, new Color(244, 223, 177));
            DrawSprite(b, selectedIndex, topProduct, 1f);
        }

        ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, selected.Key, StringComparison.OrdinalIgnoreCase));
        DrawFlowSprites(b, t, selected, active);
    }

    private void DrawFlowSprites(SpriteBatch b, UiTransform t, ProductionRecipeDefinition recipe, ProductionJob? active)
    {
        List<(string name, int idx)> nodes = new() { ("원재료", -1) };
        foreach (var stage in recipe.Stages.Take(4).Select((s, i) => (s.DisplayName, i)))
            nodes.Add(stage);
        nodes.Add(("완제품", 99));

        int n = nodes.Count;
        float total = 510f;
        float gap = 13f;
        float nodeW = (total - gap * (n - 1)) / n;

        for (int i = 0; i < n; i++)
        {
            int dx = 425 + (int)MathF.Round(i * (nodeW + gap));
            Rectangle icon = D(t, dx + Math.Max(2, ((int)nodeW - 50) / 2), 322, 50, 55);

            int sprite;
            if (nodes[i].idx == -1)
                sprite = 3;
            else if (nodes[i].idx == 99)
                sprite = TrySpecialProductIndex(recipe, out int finished) ? finished : 8;
            else
                sprite = ProcessSpriteIndex(nodes[i].name);

            bool current = active is not null && nodes[i].idx >= 0 && nodes[i].idx < 99 && active.CurrentStageIndex == nodes[i].idx;
            PatchPaper(b, icon, current ? new Color(255, 226, 177) : new Color(250, 235, 197));
            DrawSprite(b, sprite, icon, current ? 1f : 0.96f);
        }
    }

    private void DrawCatalog(SpriteBatch b, ProductCatalog084Menu catalog)
    {
        var t = Transform();
        DrawFrameTexture(b, t);

        // Replace the old mixed-language heading with one clean reference-style title.
        Rectangle title = D(t, 250, 15, 900, 64);
        DrawPlaquePatch(b, title, "생산품 카탈로그", t.Scale);

        int page = CatalogPage?.GetValue(catalog) is int p ? p : 0;
        int filter = CatalogFilter?.GetValue(catalog) is int f ? f : 0;
        string selectedKey = CatalogSelected?.GetValue(catalog) as string ?? "";

        IEnumerable<ProductionRecipeDefinition> query = Mod.Production.GetCatalogRecipes(true);
        if (filter == 1)
            query = query.Where(r => string.Equals(r.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        else if (filter == 2)
            query = query.Where(r => !string.Equals(r.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        List<ProductionRecipeDefinition> rows = query.ToList();
        int start = page * 6;

        for (int i = 0; i < 6; i++)
        {
            int index = start + i;
            if (index >= rows.Count)
                break;
            ProductionRecipeDefinition recipe = rows[index];
            if (!TrySpecialProductIndex(recipe, out int sprite))
                continue;
            int col = i % 2;
            int row = i / 2;
            Rectangle icon = D(t, 59 + col * 405, 216 + row * 168, 67, 67);
            PatchPaper(b, icon, new Color(250, 235, 197));
            DrawSprite(b, sprite, icon, Mod.Production.IsRecipeUnlocked(recipe, out _) ? 1f : 0.45f);
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(selectedKey) ?? rows.FirstOrDefault();
        if (selected is not null && TrySpecialProductIndex(selected, out int selectedSprite))
        {
            Rectangle detail = D(t, 1082, 202, 104, 104);
            PatchPaper(b, detail, new Color(244, 223, 177));
            DrawSprite(b, selectedSprite, detail, Mod.Production.IsRecipeUnlocked(selected, out _) ? 1f : 0.48f);
        }
    }

    private int ProcessSpriteIndex(string? stage)
    {
        string s = stage ?? "";
        if (s.Contains("세척", StringComparison.CurrentCultureIgnoreCase)) return 4;
        if (s.Contains("착즙", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("압착", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("파쇄", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("분쇄", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("절단", StringComparison.CurrentCultureIgnoreCase)) return 5;
        if (s.Contains("살균", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("가열", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("숙성", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("발효", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("염장", StringComparison.CurrentCultureIgnoreCase)) return 6;
        if (s.Contains("병입", StringComparison.CurrentCultureIgnoreCase)) return 7;
        if (s.Contains("포장", StringComparison.CurrentCultureIgnoreCase)
            || s.Contains("세트", StringComparison.CurrentCultureIgnoreCase)) return 11;
        return 5;
    }

    private static bool TrySpecialProductIndex(ProductionRecipeDefinition recipe, out int index)
    {
        string name = recipe.DisplayName ?? "";
        string key = recipe.Key ?? "";
        if (name.Contains("토마토주스", StringComparison.CurrentCultureIgnoreCase) || key.Contains("TomatoJuice", StringComparison.OrdinalIgnoreCase)) { index = 9; return true; }
        if (name.Contains("수박주스", StringComparison.CurrentCultureIgnoreCase) || key.Contains("WatermelonJuice", StringComparison.OrdinalIgnoreCase)) { index = 10; return true; }
        if (name.Contains("선물세트", StringComparison.CurrentCultureIgnoreCase) || name.Contains("선물 세트", StringComparison.CurrentCultureIgnoreCase)) { index = 11; return true; }
        if (name.Contains("펄프", StringComparison.CurrentCultureIgnoreCase)) { index = 12; return true; }
        if (name.Contains("절임", StringComparison.CurrentCultureIgnoreCase) || name.Contains("피클", StringComparison.CurrentCultureIgnoreCase)) { index = 13; return true; }
        if (name.Contains("밀가루", StringComparison.CurrentCultureIgnoreCase) || name.Contains("분말", StringComparison.CurrentCultureIgnoreCase) || name.Contains("가루", StringComparison.CurrentCultureIgnoreCase)) { index = 14; return true; }
        index = -1;
        return false;
    }

    private void DrawFrameTexture(SpriteBatch b, UiTransform t)
    {
        if (UiTiles is null)
            return;
        DrawTiled(b, D(t, 0, 0, 1400, 17), 0, Color.White * 0.72f);
        DrawTiled(b, D(t, 0, 803, 1400, 17), 0, Color.White * 0.72f);
        DrawTiled(b, D(t, 0, 17, 18, 786), 0, Color.White * 0.72f);
        DrawTiled(b, D(t, 1382, 17, 18, 786), 0, Color.White * 0.72f);
    }

    private void DrawPlaquePatch(SpriteBatch b, Rectangle rect, string text, float scale)
    {
        Color woodDeep = new(62, 36, 18);
        Color gold = new(224, 171, 58);
        Fill(b, rect, woodDeep);
        Rectangle mid = Inset(rect, Math.Max(2, (int)MathF.Round(3 * scale)));
        Fill(b, mid, gold);
        Rectangle inner = Inset(rect, Math.Max(3, (int)MathF.Round(7 * scale)));
        if (UiTiles is not null) DrawTiled(b, inner, 2, Color.White);
        else Fill(b, inner, new Color(27, 70, 28));
        float textScale = Math.Max(0.2f, scale * 0.96f);
        Vector2 size = Game1.dialogueFont.MeasureString(text) * textScale;
        b.DrawString(Game1.dialogueFont, text, new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f), new Color(251, 221, 143), 0f, Vector2.Zero, textScale, SpriteEffects.None, 1f);
    }

    private void PatchPaper(SpriteBatch b, Rectangle rect, Color fallback)
    {
        if (UiTiles is not null)
            DrawTiled(b, rect, 1, Color.White);
        else
            Fill(b, rect, fallback);
    }

    private void DrawSprite(SpriteBatch b, int index, Rectangle dest, float alpha)
    {
        if (VisualAtlas is null || index < 0 || index >= 16)
            return;
        Rectangle src = new((index % 4) * Cell, (index / 4) * Cell, Cell, Cell);
        b.Draw(VisualAtlas, dest, src, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, 1f);
    }

    private void DrawTiled(SpriteBatch b, Rectangle dest, int tile, Color tint)
    {
        if (UiTiles is null)
            return;
        Rectangle srcBase = new(tile * 64, 0, 64, 64);
        for (int y = dest.Y; y < dest.Bottom; y += 64)
        {
            for (int x = dest.X; x < dest.Right; x += 64)
            {
                int w = Math.Min(64, dest.Right - x);
                int h = Math.Min(64, dest.Bottom - y);
                Rectangle src = new(srcBase.X, srcBase.Y, w, h);
                b.Draw(UiTiles, new Rectangle(x, y, w, h), src, tint);
            }
        }
    }

    private static UiTransform Transform()
    {
        int uiW = Math.Max(720, Game1.uiViewport.Width);
        int uiH = Math.Max(520, Game1.uiViewport.Height);
        float scale = Math.Min((uiW - 18f) / DesignW, (uiH - 18f) / DesignH);
        scale = Math.Clamp(scale, 0.56f, 1.16f);
        int actualW = (int)MathF.Round(DesignW * scale);
        int actualH = (int)MathF.Round(DesignH * scale);
        return new UiTransform((uiW - actualW) / 2, (uiH - actualH) / 2, scale);
    }

    private static Rectangle D(UiTransform t, int x, int y, int w, int h)
        => new(t.X + (int)MathF.Round(x * t.Scale), t.Y + (int)MathF.Round(y * t.Scale), Math.Max(1, (int)MathF.Round(w * t.Scale)), Math.Max(1, (int)MathF.Round(h * t.Scale)));

    private static Rectangle Inset(Rectangle r, int n)
        => new(r.X + n, r.Y + n, Math.Max(1, r.Width - n * 2), Math.Max(1, r.Height - n * 2));

    private static void Fill(SpriteBatch b, Rectangle r, Color c)
        => b.Draw(Game1.fadeToBlackRect, r, c);

    private readonly record struct UiTransform(int X, int Y, float Scale);
}
