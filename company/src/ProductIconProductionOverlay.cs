using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductIconProductionOverlay
{
    private readonly ModEntry Mod;
    private readonly FieldInfo? SelectedRecipeField = typeof(Production2Menu).GetField("SelectedRecipeKey", BindingFlags.Instance | BindingFlags.NonPublic);

    internal ProductIconProductionOverlay(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Game1.activeClickableMenu is not Production2Menu menu)
            return;

        Layout layout = BuildLayout();
        DrawLineIcons(e.SpriteBatch, layout);
        DrawSelectedRecipeIcon(e.SpriteBatch, menu, layout);
        DrawStockIcons(e.SpriteBatch, layout);
    }

    private void DrawLineIcons(SpriteBatch b, Layout layout)
    {
        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < lines.Count && i < 3; i++)
        {
            ProductionJob? job = Mod.Production.GetLineJob(lines[i].Id);
            if (job is null)
                continue;
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is null)
                continue;

            Rectangle card = LineCard(layout.LeftPanel, i);
            Rectangle icon = new(card.Right - 130, card.Y + 39, 38, 38);
            Mod.Icons.DrawRecipeIcon(b, recipe, icon);
        }
    }

    private void DrawSelectedRecipeIcon(SpriteBatch b, Production2Menu menu, Layout layout)
    {
        string? key = SelectedRecipeField?.GetValue(menu) as string;
        if (string.IsNullOrWhiteSpace(key))
            return;
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(key);
        if (recipe is null)
            return;

        Rectangle icon = new(layout.CenterPanel.Right - 70, layout.CenterPanel.Y + 48, 52, 52);
        Mod.Icons.DrawRecipeIcon(b, recipe, icon);
    }

    private void DrawStockIcons(SpriteBatch b, Layout layout)
    {
        IReadOnlyList<IntermediateStockEntry> intermediates = Mod.Production.GetIntermediateStock();
        int y = layout.IntermediatePanel.Y + 39;
        foreach (IntermediateStockEntry stock in intermediates.Take(4))
        {
            Rectangle icon = new(layout.IntermediatePanel.Right - 184, y + 3, 26, 26);
            Mod.Icons.DrawProductIcon(b, stock.Key, icon);
            y += 34;
        }

        List<ProductStockEntry> finished = Mod.State.FinishedGoods.Values
            .Where(p => p is not null && p.Quantity > 0)
            .OrderByDescending(p => p.Quality)
            .ThenByDescending(p => p.Quantity)
            .Take(4)
            .ToList();
        y = layout.FinishedPanel.Y + 39;
        foreach (ProductStockEntry stock in finished)
        {
            Rectangle icon = new(layout.FinishedPanel.Right - 184, y + 3, 26, 26);
            Mod.Icons.DrawProductIcon(b, stock.ProductKey, icon);
            y += 34;
        }
    }

    private static Layout BuildLayout()
    {
        int w = Math.Min(1440, Math.Max(960, Game1.viewport.Width - 28));
        int h = Math.Min(930, Math.Max(690, Game1.viewport.Height - 28));
        int x = Game1.viewport.Width / 2 - w / 2;
        int y = Game1.viewport.Height / 2 - h / 2;
        int bodyTop = y + 136;
        int bodyHeight = h - 136 - 230;
        int gap = 10;
        int leftW = (int)((w - 16 - gap * 2) * 0.30f);
        int centerW = (int)((w - 16 - gap * 2) * 0.41f);
        int rightW = w - 16 - gap * 2 - leftW - centerW;
        Rectangle left = new(x + 8, bodyTop, leftW, bodyHeight);
        Rectangle center = new(left.Right + gap, bodyTop, centerW, bodyHeight);
        Rectangle right = new(center.Right + gap, bodyTop, rightW, bodyHeight);
        int bottomTop = bodyTop + bodyHeight + 10;
        int bottomHeight = h - (bottomTop - y) - 48;
        int bottomW = (w - 26) / 2;
        Rectangle intermediate = new(x + 8, bottomTop, bottomW, bottomHeight);
        Rectangle finished = new(intermediate.Right + 10, bottomTop, w - 18 - bottomW, bottomHeight);
        return new Layout(left, center, right, intermediate, finished);
    }

    private static Rectangle LineCard(Rectangle leftPanel, int index)
    {
        int top = leftPanel.Y + 49;
        int h = Math.Max(112, (leftPanel.Height - 62) / 3 - 7);
        return new Rectangle(leftPanel.X + 10, top + index * (h + 7), leftPanel.Width - 20, h);
    }

    private readonly record struct Layout(
        Rectangle LeftPanel,
        Rectangle CenterPanel,
        Rectangle RightPanel,
        Rectangle IntermediatePanel,
        Rectangle FinishedPanel
    );
}
