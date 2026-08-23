using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductionQualityUi
{
    private readonly ModEntry Mod;
    private readonly FieldInfo? SelectedRecipeField = typeof(Production2Menu).GetField("SelectedRecipeKey", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Color Paper = new(242, 226, 187);
    private static readonly Color PaperDark = new(224, 205, 164);
    private static readonly Color Wood = new(83, 53, 30);
    private static readonly Color Green = new(50, 91, 49);
    private static readonly Color Green2 = new(73, 118, 65);
    private static readonly Color Gold = new(190, 139, 43);
    private static readonly Color Muted = new(104, 84, 59);
    private static readonly Color Purple = new(132, 92, 153);
    private static readonly Color Blue = new(48, 92, 126);
    private static readonly Color Red = new(143, 61, 51);

    internal ProductionQualityUi(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Game1.activeClickableMenu is not Production2Menu menu || Game1.viewport.Height < 760)
            return;

        string selectedKey = SelectedRecipeField?.GetValue(menu) as string ?? "";
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(selectedKey) ?? Mod.Recipes.FirstOrDefault();
        if (recipe is null)
            return;

        ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, recipe.Key, StringComparison.OrdinalIgnoreCase));
        ProductionForecast forecast = active is not null ? Mod.Quality.GetForecast(active) : Mod.Quality.GetForecast(recipe, 1);
        DrawAnalysis(e.SpriteBatch, recipe, forecast, active);
    }

    private void DrawAnalysis(SpriteBatch b, ProductionRecipeDefinition recipe, ProductionForecast forecast, ProductionJob? active)
    {
        Rectangle center = CalculateCenterPanel();
        int contentTop = center.Y + 45;
        int infoY = contentTop + 180;
        int infoBottom = center.Bottom - 72;
        int infoHeight = Math.Max(156, infoBottom - infoY);
        Rectangle info = new(center.X + 15, infoY, center.Width - 30, infoHeight);

        b.Draw(Game1.fadeToBlackRect, info, Wood);
        Rectangle inner = new(info.X + 3, info.Y + 3, info.Width - 6, info.Height - 6);
        b.Draw(Game1.fadeToBlackRect, inner, Paper);

        b.DrawString(Game1.smallFont, "생산 분석 2.1", new Vector2(inner.X + 12, inner.Y + 9), Green);
        string status = active is null ? "계획 전 예상" : $"가동 중 · {Mod.Production.GetCurrentStageName(active)}";
        Vector2 statusSize = Game1.smallFont.MeasureString(status);
        b.DrawString(Game1.smallFont, status, new Vector2(inner.Right - statusSize.X - 12, inner.Y + 9), Muted);

        int dividerX = inner.X + (int)(inner.Width * 0.53f);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(dividerX, inner.Y + 35, 2, inner.Height - 47), PaperDark);

        int leftX = inner.X + 12;
        int rowY = inner.Y + 39;
        int leftValueX = dividerX - 12;
        DrawMetric(b, leftX, leftValueX, rowY, "종합 품질점수", $"{forecast.FinalQualityScore}/100", Gold); rowY += 25;
        DrawMetric(b, leftX, leftValueX, rowY, "원재료 품질", $"{forecast.InputQualityScore}/100", Wood); rowY += 25;
        DrawMetric(b, leftX, leftValueX, rowY, "라인 효율", $"{forecast.LineEfficiency}%", Green); rowY += 25;
        DrawMetric(b, leftX, leftValueX, rowY, "공정 안정성", $"{forecast.ProcessQualityScore}/100", Green); rowY += 25;
        DrawMetric(b, leftX, leftValueX, rowY, "예상 수율", $"{forecast.ExpectedYieldPercent}%", Blue); rowY += 25;
        string outputText = forecast.MinOutput == forecast.MaxOutput
            ? $"{forecast.MinOutput}{recipe.OutputUnit}"
            : $"{forecast.MinOutput}~{forecast.MaxOutput}{recipe.OutputUnit}";
        DrawMetric(b, leftX, leftValueX, rowY, "예상 생산량", outputText, Wood); rowY += 25;
        DrawMetric(b, leftX, leftValueX, rowY, "병목 공정", forecast.BottleneckStage, Red);

        int rightX = dividerX + 13;
        int rightW = inner.Right - rightX - 12;
        b.DrawString(Game1.smallFont, $"등급 확률 · 예상 {forecast.MostLikelyGrade}급", new Vector2(rightX, inner.Y + 39), Wood);
        int barY = inner.Y + 68;
        DrawChance(b, rightX, barY, rightW, "S", forecast.SChance, Purple); barY += 28;
        DrawChance(b, rightX, barY, rightW, "A", forecast.AChance, Green2); barY += 28;
        DrawChance(b, rightX, barY, rightW, "B", forecast.BChance, Blue); barY += 28;
        DrawChance(b, rightX, barY, rightW, "C", forecast.CChance, Muted); barY += 31;

        ProductionResultReport? latest = Mod.Quality.GetLatestReport();
        if (latest is not null && barY + 40 < inner.Bottom)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(rightX, barY, rightW, 1), PaperDark);
            b.DrawString(Game1.smallFont, "최근 생산 결과", new Vector2(rightX, barY + 6), Green);
            string result = $"{latest.ProductName} {latest.Grade}급 · 수율 {latest.ActualYieldPercent}% · 품질 {latest.FinalQualityScore}";
            b.DrawString(Game1.smallFont, TrimToWidth(result, rightW), new Vector2(rightX, barY + 28), Muted);
        }
    }

    private static void DrawMetric(SpriteBatch b, int labelX, int valueRight, int y, string label, string value, Color valueColor)
    {
        b.DrawString(Game1.smallFont, label, new Vector2(labelX, y), Muted);
        Vector2 size = Game1.smallFont.MeasureString(value);
        b.DrawString(Game1.smallFont, value, new Vector2(valueRight - size.X, y), valueColor);
    }

    private static void DrawChance(SpriteBatch b, int x, int y, int width, string grade, int chance, Color fill)
    {
        b.DrawString(Game1.smallFont, grade, new Vector2(x, y), Wood);
        Rectangle back = new(x + 28, y + 5, Math.Max(30, width - 82), 12);
        b.Draw(Game1.fadeToBlackRect, back, new Color(170, 157, 126));
        int fillW = (int)((back.Width - 2) * Math.Clamp(chance / 100f, 0f, 1f));
        b.Draw(Game1.fadeToBlackRect, new Rectangle(back.X + 1, back.Y + 1, fillW, back.Height - 2), fill);
        string text = $"{chance}%";
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(x + width - size.X, y), Wood);
    }

    private static string TrimToWidth(string text, int width)
    {
        if (Game1.smallFont.MeasureString(text).X <= width)
            return text;
        string result = text;
        while (result.Length > 3 && Game1.smallFont.MeasureString(result + "…").X > width)
            result = result[..^1];
        return result + "…";
    }

    private static Rectangle CalculateCenterPanel()
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
        int leftX = x + 8;
        return new Rectangle(leftX + leftW + gap, bodyTop, centerW, bodyHeight);
    }
}