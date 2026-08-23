using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>
/// 0.8.7 art refinement layer. It sits above the stable 0.8.6 cleanup layer and
/// enlarges/beautifies machinery, process and product art without changing gameplay.
/// </summary>
internal sealed class Production087ArtOverlay
{
    private const int DesignW = 1400;
    private const int DesignH = 820;
    private const int Cell = 96;

    private readonly ModEntry Mod;
    private Texture2D? Atlas;
    private bool LoadTried;
    private bool Warned;

    private static readonly FieldInfo? ProductionSelected = typeof(Production084Menu).GetField("SelectedRecipeKey", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CatalogPage = typeof(ProductCatalog084Menu).GetField("Page", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CatalogFilter = typeof(ProductCatalog084Menu).GetField("Filter", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CatalogSelected = typeof(ProductCatalog084Menu).GetField("SelectedKey", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Color Cream = new(250, 235, 197);
    private static readonly Color Cream2 = new(244, 223, 177);
    private static readonly Color GreenDeep = new(27, 70, 28);
    private static readonly Color Gold = new(224, 171, 58);
    private static readonly Color WoodDeep = new(62, 36, 18);
    private static readonly Color Wood = new(111, 65, 29);
    private static readonly Color Muted = new(143, 115, 78);
    private static readonly Color TitleInk = new(251, 221, 143);

    internal Production087ArtOverlay(ModEntry mod)
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
        if (Atlas is not null && !Atlas.IsDisposed)
            return true;
        if (LoadTried)
            return false;

        LoadTried = true;
        try
        {
            Atlas = Mod.Helper.ModContent.Load<Texture2D>("assets/production_visuals_087.png");
            if (Atlas.Width < Cell * 4 || Atlas.Height < Cell * 4)
                throw new InvalidOperationException($"production_visuals_087.png size {Atlas.Width}x{Atlas.Height} is smaller than 384x384.");
            return true;
        }
        catch (Exception ex)
        {
            if (!Warned)
            {
                Warned = true;
                Mod.Monitor.Log($"0.8.7 art assets could not be loaded; keeping the safe 0.8.6 visuals. {ex.Message}", StardewModdingAPI.LogLevel.Warn);
            }
            Atlas = null;
            return false;
        }
    }

    private void DrawProduction(SpriteBatch b, Production084Menu menu)
    {
        UiTransform t = Transform();
        DrawFrameAccents(b, t);

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < Math.Min(3, lines.Count); i++)
        {
            ProductionLineState line = lines[i];
            int machineSprite = line.LineType switch
            {
                "Fermentation" => 1,
                "Packaging" => 2,
                _ => 0
            };

            // Larger machinery fills the left side of each line card, closer to the approved mockup.
            Rectangle machine = D(t, 43, 247 + i * 126, 126, 94);
            Clear(b, Inflate(machine, S(t, 3)), Cream);
            DrawSprite(b, machineSprite, machine, 1f, true);

            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            ProductionRecipeDefinition? recipe = job is null ? null : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is not null)
            {
                Rectangle icon = D(t, 168, 260 + i * 126, 52, 52);
                Clear(b, Inflate(icon, S(t, 5)), Cream);
                DrawSprite(b, ProductSpriteIndex(recipe), icon, 1f, true);
            }
        }

        string selectedKey = ProductionSelected?.GetValue(menu) as string ?? "";
        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(selectedKey) ?? Mod.Recipes.FirstOrDefault();
        if (selected is not null)
        {
            Rectangle topProduct = D(t, 579, 218, 76, 76);
            Clear(b, D(t, 572, 214, 91, 85), Cream2);
            DrawSprite(b, ProductSpriteIndex(selected), topProduct, 1f, true);

            ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, selected.Key, StringComparison.OrdinalIgnoreCase));
            DrawFlowSprites(b, t, selected, active);
        }

        DrawEmptyInventorySlots(b, t);
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
            Rectangle icon = D(t, dx + Math.Max(0, ((int)nodeW - 60) / 2), 316, 60, 66);
            bool current = active is not null && nodes[i].idx >= 0 && nodes[i].idx < 99 && active.CurrentStageIndex == nodes[i].idx;
            int sprite = nodes[i].idx switch
            {
                -1 => 3,
                99 => ProductSpriteIndex(recipe),
                _ => ProcessSpriteIndex(nodes[i].name)
            };

            Clear(b, Inflate(icon, S(t, 3)), current ? new Color(255, 226, 177) : Cream);
            DrawSprite(b, sprite, icon, current ? 1f : 0.98f, true);
        }
    }

    private void DrawCatalog(SpriteBatch b, ProductCatalog084Menu catalog)
    {
        UiTransform t = Transform();
        DrawFrameAccents(b, t);
        DrawCleanCatalogTitle(b, t);

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
            int col = i % 2;
            int row = i / 2;
            Rectangle clear = D(t, 49 + col * 405, 205 + row * 168, 97, 102);
            Rectangle icon = D(t, 56 + col * 405, 211 + row * 168, 84, 84);
            Clear(b, clear, Cream);
            DrawSprite(b, ProductSpriteIndex(recipe), icon, Mod.Production.IsRecipeUnlocked(recipe, out _) ? 1f : 0.43f, true);
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(selectedKey) ?? rows.FirstOrDefault();
        if (selected is not null)
        {
            Rectangle detailClear = D(t, 1058, 184, 150, 132);
            Rectangle detail = D(t, 1070, 188, 126, 126);
            Clear(b, detailClear, Cream2);
            DrawSprite(b, ProductSpriteIndex(selected), detail, Mod.Production.IsRecipeUnlocked(selected, out _) ? 1f : 0.46f, true);
        }
    }

    private void DrawEmptyInventorySlots(SpriteBatch b, UiTransform t)
    {
        bool noIntermediate = !Mod.Production.GetIntermediateStock().Any(p => p.Quantity > 0);
        bool noFinished = !Mod.State.FinishedGoods.Values.Any(p => p is not null && p.Quantity > 0);
        if (noIntermediate)
            DrawSlotStrip(b, t, 34, 716, 640, 72);
        if (noFinished)
            DrawSlotStrip(b, t, 708, 716, 658, 72);
    }

    private void DrawSlotStrip(SpriteBatch b, UiTransform t, int x, int y, int w, int h)
    {
        Rectangle body = D(t, x, y, w, h);
        Clear(b, body, Cream);
        int gap = 10;
        int slotW = (w - gap * 4) / 3;
        for (int i = 0; i < 3; i++)
        {
            Rectangle slot = D(t, x + gap + i * (slotW + gap), y + 8, slotW, h - 16);
            Fill(b, slot, new Color(218, 193, 145));
            Fill(b, Inset(slot, S(t, 3)), new Color(248, 235, 201));
            Rectangle icon = new(slot.X + S(t, 9), slot.Y + S(t, 9), S(t, 34), S(t, 34));
            DrawGhostCrate(b, icon);
            string text = "빈 슬롯";
            float sc = Math.Max(0.18f, t.Scale * 0.60f);
            b.DrawString(Game1.smallFont, text, new Vector2(slot.X + S(t, 52), slot.Y + (slot.Height - Game1.smallFont.MeasureString(text).Y * sc) / 2f), Muted, 0f, Vector2.Zero, sc, SpriteEffects.None, 1f);
        }
    }

    private void DrawGhostCrate(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(118, 82, 49) * 0.22f);
        Fill(b, Inset(r, Math.Max(1, r.Width / 9)), new Color(188, 150, 96) * 0.20f);
        int line = Math.Max(1, r.Width / 12);
        Fill(b, new Rectangle(r.X + line * 2, r.Y, line, r.Height), new Color(105, 76, 45) * 0.24f);
        Fill(b, new Rectangle(r.Right - line * 3, r.Y, line, r.Height), new Color(105, 76, 45) * 0.24f);
    }

    private void DrawFrameAccents(SpriteBatch b, UiTransform t)
    {
        // Subtle wood grain and corner studs, intentionally restricted to the outer rails.
        Color grain = new Color(73, 41, 19) * 0.28f;
        for (int x = 30; x < 1370; x += 84)
        {
            Fill(b, D(t, x, 6, 48, 2), grain);
            Fill(b, D(t, x + 18, 812, 42, 2), grain);
        }
        for (int y = 26; y < 790; y += 72)
        {
            Fill(b, D(t, 6, y, 2, 36), grain);
            Fill(b, D(t, 1391, y + 12, 2, 36), grain);
        }
        Color stud = new Color(236, 181, 67) * 0.85f;
        foreach (var p in new[] { (10, 10), (1382, 10), (10, 802), (1382, 802) })
            Fill(b, D(t, p.Item1, p.Item2, 8, 8), stud);
    }

    private static int ProcessSpriteIndex(string? stage)
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

    private static int ProductSpriteIndex(ProductionRecipeDefinition recipe)
    {
        string name = recipe.DisplayName ?? "";
        string key = recipe.Key ?? "";
        if (name.Contains("토마토주스", StringComparison.CurrentCultureIgnoreCase) || key.Contains("TomatoJuice", StringComparison.OrdinalIgnoreCase)) return 9;
        if (name.Contains("수박주스", StringComparison.CurrentCultureIgnoreCase) || key.Contains("WatermelonJuice", StringComparison.OrdinalIgnoreCase)) return 10;
        if (name.Contains("잼", StringComparison.CurrentCultureIgnoreCase) || key.Contains("Jam", StringComparison.OrdinalIgnoreCase)) return 15;
        if (name.Contains("선물세트", StringComparison.CurrentCultureIgnoreCase) || name.Contains("선물 세트", StringComparison.CurrentCultureIgnoreCase)) return 11;
        if (name.Contains("펄프", StringComparison.CurrentCultureIgnoreCase)) return 12;
        if (name.Contains("절임", StringComparison.CurrentCultureIgnoreCase) || name.Contains("피클", StringComparison.CurrentCultureIgnoreCase)) return 13;
        if (name.Contains("밀가루", StringComparison.CurrentCultureIgnoreCase)
            || name.Contains("분말", StringComparison.CurrentCultureIgnoreCase)
            || name.Contains("가루", StringComparison.CurrentCultureIgnoreCase)) return 14;
        if (name.Contains("주스", StringComparison.CurrentCultureIgnoreCase) || name.Contains("원액", StringComparison.CurrentCultureIgnoreCase)) return 8;
        if (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase))
        {
            if (name.Contains("세척", StringComparison.CurrentCultureIgnoreCase)) return 3;
            if (name.Contains("베이스", StringComparison.CurrentCultureIgnoreCase)) return 6;
            return 5;
        }
        return 8;
    }

    private void DrawCleanCatalogTitle(SpriteBatch b, UiTransform t)
    {
        Rectangle outer = D(t, 250, 15, 900, 64);
        Fill(b, outer, WoodDeep);
        Fill(b, Inset(outer, S(t, 3)), Gold);
        Fill(b, Inset(outer, S(t, 7)), GreenDeep);
        float textScale = Math.Max(0.2f, t.Scale * 0.96f);
        const string text = "생산품 카탈로그";
        Vector2 size = Game1.dialogueFont.MeasureString(text) * textScale;
        b.DrawString(Game1.dialogueFont, text,
            new Vector2(outer.X + (outer.Width - size.X) / 2f, outer.Y + (outer.Height - size.Y) / 2f),
            TitleInk, 0f, Vector2.Zero, textScale, SpriteEffects.None, 1f);
    }

    private void DrawSprite(SpriteBatch b, int index, Rectangle dest, float alpha, bool shadow)
    {
        if (Atlas is null || index < 0 || index >= 16)
            return;
        Rectangle src = new((index % 4) * Cell, (index / 4) * Cell, Cell, Cell);
        if (shadow)
        {
            int off = Math.Max(1, dest.Width / 30);
            Rectangle sh = new(dest.X + off, dest.Y + off, dest.Width, dest.Height);
            b.Draw(Atlas, sh, src, Color.Black * (0.20f * alpha), 0f, Vector2.Zero, SpriteEffects.None, 0.99f);
        }
        b.Draw(Atlas, dest, src, Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, 1f);
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

    private static int S(UiTransform t, int value) => Math.Max(1, (int)MathF.Round(value * t.Scale));
    private static Rectangle D(UiTransform t, int x, int y, int w, int h)
        => new(t.X + (int)MathF.Round(x * t.Scale), t.Y + (int)MathF.Round(y * t.Scale), Math.Max(1, (int)MathF.Round(w * t.Scale)), Math.Max(1, (int)MathF.Round(h * t.Scale)));
    private static Rectangle Inset(Rectangle r, int n)
        => new(r.X + n, r.Y + n, Math.Max(1, r.Width - n * 2), Math.Max(1, r.Height - n * 2));
    private static Rectangle Inflate(Rectangle r, int n)
        => new(r.X - n, r.Y - n, r.Width + n * 2, r.Height + n * 2);
    private static void Clear(SpriteBatch b, Rectangle r, Color c) => Fill(b, r, c);
    private static void Fill(SpriteBatch b, Rectangle r, Color c) => b.Draw(Game1.fadeToBlackRect, r, c);

    private readonly record struct UiTransform(int X, int Y, float Scale);
}
