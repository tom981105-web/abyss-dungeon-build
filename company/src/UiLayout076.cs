using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

/// <summary>
/// 0.7.6 unified menu layout controller.
/// All menu coordinates are screen-relative UI coordinates and intentionally use
/// uiViewport Width/Height only, matching Stardew Valley's own menu centering pattern.
/// </summary>
internal sealed class UiLayout076
{
    private const string VersionText = "0.7.6";
    private readonly ModEntry Mod;

    private static readonly FieldInfo? CompanyTabsField = typeof(CompanyMenu).GetField("Tabs", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CompanySelectedTabField = typeof(CompanyMenu).GetField("SelectedTab", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? SelectedRecipeField = typeof(Production2Menu).GetField("SelectedRecipeKey", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CloseButtonField = typeof(IClickableMenu).GetField("upperRightCloseButton", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly Color SidebarGreen = new(48, 78, 58);
    private static readonly Color Paper = new(248, 235, 199);
    private static readonly Color Wood = new(83, 53, 30);
    private static readonly Color WoodDark = new(55, 37, 24);
    private static readonly Color Green = new(50, 91, 49);
    private static readonly Color Muted = new(104, 84, 59);
    private static readonly Color Blue = new(48, 92, 126);

    internal UiLayout076(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Display.MenuChanged += OnMenuChanged;
        Mod.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        Mod.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        ApplyLayout(e.NewMenu);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(20))
            return;

        ApplyLayout(Game1.activeClickableMenu);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || e.Button != SButton.MouseLeft)
            return;

        if (Game1.activeClickableMenu is CompanyMenu company)
        {
            if (CompanyTabsField?.GetValue(company) is not List<(string Name, Rectangle Bounds)> tabs || tabs.Count < 2)
                return;

            Rectangle productionTab = tabs[1].Bounds;
            if (!productionTab.Contains(Game1.getMouseX(), Game1.getMouseY()))
                return;

            Mod.Helper.Input.Suppress(e.Button);
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new Production2Menu(Mod);
            ApplyLayout(Game1.activeClickableMenu);
            return;
        }

        if (Game1.activeClickableMenu is Production2Menu production)
        {
            Rectangle catalog = CatalogButton(production);
            if (!catalog.Contains(Game1.getMouseX(), Game1.getMouseY()))
                return;

            Mod.Helper.Input.Suppress(e.Button);
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new ProductCatalogMenu(Mod);
            ApplyLayout(Game1.activeClickableMenu);
        }
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Game1.activeClickableMenu is CompanyMenu company)
        {
            DrawCompanyVersion(e.SpriteBatch, company);
            return;
        }

        if (Game1.activeClickableMenu is Production2Menu production)
        {
            DrawCatalogButton(e.SpriteBatch, production);
            DrawProductionIcons(e.SpriteBatch, production);
            DrawQualitySummary(e.SpriteBatch, production);
        }
    }

    private static void ApplyLayout(IClickableMenu? menu)
    {
        if (menu is CompanyMenu company)
            CenterCompanyMenu(company);
        else if (menu is Production2Menu production)
            ReflowProduction(production);
        else if (menu is ProductCatalogMenu catalog)
            ReflowCatalog(catalog);
    }

    private static void CenterCompanyMenu(CompanyMenu menu)
    {
        // Match Stardew's own GameMenu convention: UI screen coordinates are based on
        // uiViewport.Width/Height. uiViewport.X/Y are not added here.
        int desiredX = (Game1.uiViewport.Width - menu.width) / 2;
        int desiredY = (Game1.uiViewport.Height - menu.height) / 2;
        int dx = desiredX - menu.xPositionOnScreen;
        int dy = desiredY - menu.yPositionOnScreen;
        if (dx == 0 && dy == 0)
            return;

        menu.xPositionOnScreen = desiredX;
        menu.yPositionOnScreen = desiredY;

        if (CompanyTabsField?.GetValue(menu) is List<(string Name, Rectangle Bounds)> tabs)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                (string name, Rectangle bounds) = tabs[i];
                tabs[i] = (name, new Rectangle(bounds.X + dx, bounds.Y + dy, bounds.Width, bounds.Height));
            }
        }

        MoveCloseButton(menu, dx, dy);
    }

    private static void ReflowProduction(Production2Menu menu)
    {
        int uiW = Math.Max(640, Game1.uiViewport.Width);
        int uiH = Math.Max(520, Game1.uiViewport.Height);
        int w = Math.Min(1440, Math.Max(620, uiW - 28));
        int h = Math.Min(930, Math.Max(500, uiH - 28));
        w = Math.Min(w, Math.Max(600, uiW - 8));
        h = Math.Min(h, Math.Max(480, uiH - 8));
        int x = (uiW - w) / 2;
        int y = (uiH - h) / 2;

        Rectangle panel = new(x, y, w, h);
        Rectangle header = new(x + 8, y + 8, w - 16, 66);
        Rectangle status = new(x + 8, y + 78, w - 16, 50);
        int bodyTop = y + 136;
        int bodyHeight = Math.Max(220, h - 366);
        int gap = 10;
        int available = Math.Max(520, w - 16 - gap * 2);
        int leftW = (int)(available * 0.30f);
        int centerW = (int)(available * 0.41f);
        int rightW = Math.Max(140, available - leftW - centerW);
        Rectangle left = new(x + 8, bodyTop, leftW, bodyHeight);
        Rectangle center = new(left.Right + gap, bodyTop, centerW, bodyHeight);
        Rectangle right = new(center.Right + gap, bodyTop, rightW, bodyHeight);

        int bottomTop = bodyTop + bodyHeight + 10;
        int bottomHeight = Math.Max(78, h - (bottomTop - y) - 48);
        int bottomW = Math.Max(250, (w - 26) / 2);
        Rectangle intermediate = new(x + 8, bottomTop, bottomW, bottomHeight);
        Rectangle finished = new(intermediate.Right + 10, bottomTop, Math.Max(250, w - 18 - bottomW), bottomHeight);
        Rectangle footer = new(x + 8, y + h - 39, w - 16, 31);

        SetRectangle(menu, "Panel", panel);
        SetRectangle(menu, "Header", header);
        SetRectangle(menu, "StatusBar", status);
        SetRectangle(menu, "LeftPanel", left);
        SetRectangle(menu, "CenterPanel", center);
        SetRectangle(menu, "RightPanel", right);
        SetRectangle(menu, "IntermediatePanel", intermediate);
        SetRectangle(menu, "FinishedPanel", finished);
        SetRectangle(menu, "Footer", footer);

        menu.xPositionOnScreen = 0;
        menu.yPositionOnScreen = 0;
        menu.width = Game1.uiViewport.Width;
        menu.height = Game1.uiViewport.Height;
        PlaceCloseButton(menu, panel.Right - 14, panel.Y + 14);
    }

    private static void ReflowCatalog(ProductCatalogMenu menu)
    {
        int uiW = Math.Max(640, Game1.uiViewport.Width);
        int uiH = Math.Max(520, Game1.uiViewport.Height);
        int w = Math.Min(1180, Math.Max(620, uiW - 60));
        int h = Math.Min(860, Math.Max(500, uiH - 54));
        w = Math.Min(w, Math.Max(600, uiW - 8));
        h = Math.Min(h, Math.Max(480, uiH - 8));
        Rectangle panel = new((uiW - w) / 2, (uiH - h) / 2, w, h);

        SetRectangle(menu, "Panel", panel);
        menu.xPositionOnScreen = 0;
        menu.yPositionOnScreen = 0;
        menu.width = Game1.uiViewport.Width;
        menu.height = Game1.uiViewport.Height;
        PlaceCloseButton(menu, panel.Right - 10, panel.Y + 10);
    }

    private static void DrawCompanyVersion(SpriteBatch b, CompanyMenu menu)
    {
        Rectangle versionArea = new(menu.xPositionOnScreen + 20, menu.yPositionOnScreen + 57, 185, 34);
        b.Draw(Game1.fadeToBlackRect, versionArea, SidebarGreen);
        b.DrawString(Game1.smallFont, $"COMPANY {VersionText}", new Vector2(menu.xPositionOnScreen + 27, menu.yPositionOnScreen + 67), new Color(215, 228, 210));

        int selected = CompanySelectedTabField?.GetValue(menu) as int? ?? -1;
        if (selected != 0)
            return;

        int x = menu.xPositionOnScreen + 250;
        int noteY = menu.yPositionOnScreen + 496;
        Rectangle notePatch = new(x + 12, noteY + 8, Math.Max(200, menu.width - 325), 27);
        b.Draw(Game1.fadeToBlackRect, notePatch, Color.White);
        b.DrawString(Game1.smallFont, $"Agricultural Company {VersionText} · Production 2.x", new Vector2(x + 18, noteY + 14), new Color(90, 128, 76));
    }

    private static Rectangle CatalogButton(Production2Menu menu)
    {
        Rectangle panel = GetRectangle(menu, "Panel");
        return new Rectangle(panel.Right - 405, panel.Y + 24, 165, 34);
    }

    private static void DrawCatalogButton(SpriteBatch b, Production2Menu menu)
    {
        Rectangle rect = CatalogButton(menu);
        IClickableMenu.drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, new Color(51, 97, 73));
        CenterText(b, "제품 카탈로그", rect, Color.White);
    }

    private void DrawProductionIcons(SpriteBatch b, Production2Menu menu)
    {
        Rectangle left = GetRectangle(menu, "LeftPanel");
        Rectangle center = GetRectangle(menu, "CenterPanel");
        Rectangle intermediate = GetRectangle(menu, "IntermediatePanel");
        Rectangle finished = GetRectangle(menu, "FinishedPanel");

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < lines.Count && i < 3; i++)
        {
            ProductionJob? job = Mod.Production.GetLineJob(lines[i].Id);
            ProductionRecipeDefinition? recipe = job is null ? null : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is null)
                continue;
            Rectangle card = LineCard(left, i);
            Mod.Icons.DrawRecipeIcon(b, recipe, new Rectangle(card.Right - 130, card.Y + 39, 38, 38));
        }

        string key = SelectedRecipeField?.GetValue(menu) as string ?? "";
        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(key);
        if (selected is not null)
            Mod.Icons.DrawRecipeIcon(b, selected, new Rectangle(center.Right - 70, center.Y + 48, 52, 52));

        int y = intermediate.Y + 39;
        foreach (IntermediateStockEntry stock in Mod.Production.GetIntermediateStock().Take(4))
        {
            Mod.Icons.DrawProductIcon(b, stock.Key, new Rectangle(intermediate.Right - 184, y + 3, 26, 26));
            y += 34;
        }

        y = finished.Y + 39;
        foreach (ProductStockEntry stock in Mod.State.FinishedGoods.Values
                     .Where(p => p is not null && p.Quantity > 0)
                     .OrderByDescending(p => p.Quality)
                     .ThenByDescending(p => p.Quantity)
                     .Take(4))
        {
            Mod.Icons.DrawProductIcon(b, stock.ProductKey, new Rectangle(finished.Right - 184, y + 3, 26, 26));
            y += 34;
        }
    }

    private void DrawQualitySummary(SpriteBatch b, Production2Menu menu)
    {
        Rectangle center = GetRectangle(menu, "CenterPanel");
        if (center.Height < 300)
            return;

        string key = SelectedRecipeField?.GetValue(menu) as string ?? "";
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(key);
        if (recipe is null)
            return;

        ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, recipe.Key, StringComparison.OrdinalIgnoreCase));
        ProductionForecast forecast = active is null ? Mod.Quality.GetForecast(recipe, 1) : Mod.Quality.GetForecast(active);
        Rectangle box = new(center.X + 15, center.Bottom - 174, center.Width - 30, 102);
        b.Draw(Game1.fadeToBlackRect, box, WoodDark);
        Rectangle inner = new(box.X + 3, box.Y + 3, box.Width - 6, box.Height - 6);
        b.Draw(Game1.fadeToBlackRect, inner, Paper);
        b.DrawString(Game1.smallFont, "생산 분석", new Vector2(inner.X + 10, inner.Y + 7), Green);
        b.DrawString(Game1.smallFont, $"품질 {forecast.FinalQualityScore}/100 · 수율 {forecast.ExpectedYieldPercent}% · 예상 {forecast.MostLikelyGrade}급", new Vector2(inner.X + 10, inner.Y + 32), Wood);
        b.DrawString(Game1.smallFont, $"S {forecast.SChance}%  A {forecast.AChance}%  B {forecast.BChance}%  C {forecast.CChance}%", new Vector2(inner.X + 10, inner.Y + 57), Blue);
        b.DrawString(Game1.smallFont, $"병목 공정: {forecast.BottleneckStage}", new Vector2(inner.X + 10, inner.Y + 78), Muted);
    }

    private static Rectangle LineCard(Rectangle leftPanel, int index)
    {
        int top = leftPanel.Y + 49;
        int h = Math.Max(88, (leftPanel.Height - 62) / 3 - 7);
        return new Rectangle(leftPanel.X + 10, top + index * (h + 7), leftPanel.Width - 20, h);
    }

    private static Rectangle GetRectangle(object target, string name)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) is Rectangle value ? value : Rectangle.Empty;

    private static void SetRectangle(object target, string name, Rectangle value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
    }

    private static void MoveCloseButton(IClickableMenu menu, int dx, int dy)
    {
        if (CloseButtonField?.GetValue(menu) is ClickableTextureComponent close)
        {
            close.bounds.X += dx;
            close.bounds.Y += dy;
        }
    }

    private static void PlaceCloseButton(IClickableMenu menu, int right, int y)
    {
        if (CloseButtonField?.GetValue(menu) is ClickableTextureComponent close)
        {
            close.bounds.X = right - close.bounds.Width;
            close.bounds.Y = y;
        }
    }

    private static void CenterText(SpriteBatch b, string text, Rectangle rect, Color color)
    {
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(rect.X + rect.Width / 2f - size.X / 2f, rect.Y + rect.Height / 2f - size.Y / 2f), color);
    }
}
