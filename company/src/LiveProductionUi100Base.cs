using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal abstract class LiveProductionUi100Base : IClickableMenu
{
    protected const int ImageW = 1672;
    protected const int ImageH = 941;
    protected readonly ModEntry Mod;
    private readonly Texture2D? Background;
    private readonly Texture2D? Atlas;
    protected float UiScale = 1f;
    protected int UiX;
    protected int UiY;

    protected static readonly Color Ink = new(67, 43, 25);
    protected static readonly Color Muted = new(116, 92, 65);
    protected static readonly Color Green = new(45, 104, 47);
    protected static readonly Color DeepGreen = new(25, 73, 29);
    protected static readonly Color Gold = new(220, 159, 48);
    protected static readonly Color Orange = new(231, 130, 42);
    protected static readonly Color Blue = new(48, 112, 169);
    protected static readonly Color Red = new(177, 61, 42);
    protected static readonly Color Parchment = new(248, 220, 165);

    protected LiveProductionUi100Base(ModEntry mod, string backgroundPath)
        : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height, false)
    {
        Mod = mod;
        try { Background = Mod.Helper.ModContent.Load<Texture2D>(backgroundPath); }
        catch (Exception ex) { Mod.Monitor.Log($"0.10.0 live UI background load failed: {ex.Message}", StardewModdingAPI.LogLevel.Error); }
        try { Atlas = Mod.Helper.ModContent.Load<Texture2D>("assets/production_visuals_091.png"); }
        catch (Exception ex) { Mod.Monitor.Log($"0.10.0 production art atlas load failed: {ex.Message}", StardewModdingAPI.LogLevel.Warn); }
        Recalc();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = Game1.uiViewport.Width;
        height = Game1.uiViewport.Height;
        Recalc();
    }

    private void Recalc()
    {
        int vw = Math.Max(640, Game1.uiViewport.Width);
        int vh = Math.Max(360, Game1.uiViewport.Height);
        float fit = Math.Min(vw / (float)ImageW, vh / (float)ImageH);
        UiScale = Math.Max(0.20f, fit * 0.90f);
        int dw = Math.Max(1, (int)MathF.Round(ImageW * UiScale));
        int dh = Math.Max(1, (int)MathF.Round(ImageH * UiScale));
        UiX = (vw - dw) / 2;
        UiY = (vh - dh) / 2;
    }

    protected Rectangle H(int x, int y, int w, int h)
        => new(UiX + (int)MathF.Round(x * UiScale), UiY + (int)MathF.Round(y * UiScale), Math.Max(1, (int)MathF.Round(w * UiScale)), Math.Max(1, (int)MathF.Round(h * UiScale)));

    protected Vector2 P(int x, int y) => new(UiX + x * UiScale, UiY + y * UiScale);

    protected void DrawBackground(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.70f);
        if (Background is null)
        {
            b.DrawString(Game1.dialogueFont, "Live UI 배경 이미지를 불러오지 못했습니다.", new Vector2(40, 40), Color.White);
            return;
        }
        b.Draw(Background, H(0, 0, ImageW, ImageH), Color.White);
    }

    protected void Fill(SpriteBatch b, Rectangle r, Color c) => b.Draw(Game1.staminaRect, r, c);

    protected void Outline(SpriteBatch b, Rectangle r, Color c, int px = 3)
    {
        int p = Math.Max(1, (int)MathF.Round(px * UiScale));
        Fill(b, new Rectangle(r.X, r.Y, r.Width, p), c);
        Fill(b, new Rectangle(r.X, r.Bottom - p, r.Width, p), c);
        Fill(b, new Rectangle(r.X, r.Y, p, r.Height), c);
        Fill(b, new Rectangle(r.Right - p, r.Y, p, r.Height), c);
    }

    protected void Text(SpriteBatch b, SpriteFont font, string text, int x, int y, float scale = 1f, Color? color = null)
    {
        b.DrawString(font, text ?? "", P(x, y), color ?? Ink, 0f, Vector2.Zero, Math.Max(0.25f, scale * UiScale), SpriteEffects.None, 1f);
    }

    protected void TextCentered(SpriteBatch b, SpriteFont font, string text, Rectangle imageRect, float scale = 1f, Color? color = null)
    {
        Rectangle r = H(imageRect.X, imageRect.Y, imageRect.Width, imageRect.Height);
        float s = Math.Max(0.25f, scale * UiScale);
        Vector2 size = font.MeasureString(text ?? "") * s;
        Vector2 pos = new(r.X + (r.Width - size.X) / 2f, r.Y + (r.Height - size.Y) / 2f);
        b.DrawString(font, text ?? "", pos, color ?? Ink, 0f, Vector2.Zero, s, SpriteEffects.None, 1f);
    }

    protected void Progress(SpriteBatch b, Rectangle imageRect, float progress, Color? fill = null)
    {
        Rectangle r = H(imageRect.X, imageRect.Y, imageRect.Width, imageRect.Height);
        Fill(b, r, new Color(95, 75, 52));
        Rectangle inner = new(r.X + Math.Max(1, (int)(2 * UiScale)), r.Y + Math.Max(1, (int)(2 * UiScale)), Math.Max(1, r.Width - Math.Max(2, (int)(4 * UiScale))), Math.Max(1, r.Height - Math.Max(2, (int)(4 * UiScale))));
        Fill(b, inner, new Color(214, 195, 151));
        int width = (int)MathF.Round(inner.Width * Math.Clamp(progress, 0f, 1f));
        if (width > 0) Fill(b, new Rectangle(inner.X, inner.Y, width, inner.Height), fill ?? new Color(70, 145, 63));
        if (progress > 0f)
        {
            double t = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 1000d;
            int shine = inner.X + (int)((t * 70) % Math.Max(1, inner.Width));
            Fill(b, new Rectangle(shine, inner.Y, Math.Max(1, (int)(3 * UiScale)), inner.Height), Color.White * 0.45f);
        }
    }

    protected int MachineSprite(string lineType)
        => lineType.Equals("Fermentation", StringComparison.OrdinalIgnoreCase) ? 1 : lineType.Equals("Packaging", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

    protected int ProcessSprite(string stageName)
    {
        string s = stageName ?? "";
        if (s.Contains("세척")) return 4;
        if (s.Contains("착즙") || s.Contains("파쇄") || s.Contains("압착") || s.Contains("절단")) return 5;
        if (s.Contains("살균") || s.Contains("가열") || s.Contains("숙성") || s.Contains("염장") || s.Contains("발효")) return 6;
        if (s.Contains("병입")) return 7;
        if (s.Contains("포장") || s.Contains("세트")) return 2;
        return 5;
    }

    protected int ProductSprite(ProductionRecipeDefinition recipe)
    {
        string key = recipe.Key ?? "";
        string name = recipe.DisplayName ?? "";
        string family = recipe.ProductFamily ?? "";
        if (key.Contains("TomatoJuice", StringComparison.OrdinalIgnoreCase) || name.Contains("토마토주스")) return 9;
        if (key.Contains("Watermelon", StringComparison.OrdinalIgnoreCase) && (name.Contains("주스") || name.Contains("원액"))) return 10;
        if (name.Contains("선물") || name.Contains("세트")) return 11;
        if (name.Contains("펄프") || name.Contains("베이스")) return 12;
        if (name.Contains("절임") || name.Contains("배추")) return 13;
        if (name.Contains("밀가루") || name.Contains("분말") || name.Contains("가루")) return 14;
        if (name.Contains("잼")) return 15;
        if (recipe.OutputKind.Equals("Intermediate", StringComparison.OrdinalIgnoreCase)) return 8;
        if (family.Equals("Tomato", StringComparison.OrdinalIgnoreCase)) return 9;
        if (family.Equals("Watermelon", StringComparison.OrdinalIgnoreCase)) return 10;
        return 8;
    }

    protected void DrawAtlas(SpriteBatch b, int index, Rectangle imageRect, float alpha = 1f, int yOffset = 0)
    {
        if (Atlas is null) return;
        Rectangle dest = H(imageRect.X, imageRect.Y + yOffset, imageRect.Width, imageRect.Height);
        int cell = 192;
        Rectangle src = new((index % 4) * cell, (index / 4) * cell, cell, cell);
        b.Draw(Atlas, dest, src, Color.White * Math.Clamp(alpha, 0f, 1f));
    }

    protected void DrawProduct(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle imageRect, float alpha = 1f)
        => DrawAtlas(b, ProductSprite(recipe), imageRect, alpha);

    protected void DrawMachineAnimated(SpriteBatch b, string lineType, Rectangle imageRect, bool active)
    {
        double t = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 1000d;
        int bob = active ? (int)Math.Round(Math.Sin(t * 8d) * 2d) : 0;
        DrawAtlas(b, MachineSprite(lineType), imageRect, active ? 1f : 0.88f, bob);
        if (!active) return;

        for (int i = 0; i < 4; i++)
        {
            float phase = (float)((t * 0.45 + i * 0.23) % 1d);
            int x = imageRect.X + 22 + (int)((imageRect.Width - 44) * phase);
            Rectangle light = H(x, imageRect.Y + imageRect.Height - 12, 7, 7);
            Fill(b, light, i % 2 == 0 ? new Color(100, 220, 91) : new Color(246, 193, 64));
        }
        for (int i = 0; i < 3; i++)
        {
            float phase = (float)((t * 0.32 + i * 0.31) % 1d);
            int sx = imageRect.X + imageRect.Width / 2 + i * 13 - 13;
            int sy = imageRect.Y + 12 - (int)(phase * 24);
            Rectangle steam = H(sx, sy, 8, 8);
            Fill(b, steam, Color.White * (0.42f * (1f - phase)));
        }
    }

    protected string LineName(string lineType) => lineType switch
    {
        "Fermentation" => "발효",
        "Packaging" => "포장",
        _ => "음료"
    };

    protected string KindName(ProductionRecipeDefinition recipe)
        => recipe.OutputKind.Equals("Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품";

    protected int IntermediateQuantity(ProductionRecipeDefinition recipe)
    {
        string key = string.IsNullOrWhiteSpace(recipe.OutputIntermediateKey) ? recipe.Key : recipe.OutputIntermediateKey;
        return Mod.State.IntermediateStock.Values.Where(p => p is not null && p.Quantity > 0 && string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)).Sum(p => p.Quantity);
    }

    protected int ProductQuantity(ProductionRecipeDefinition recipe)
        => recipe.OutputKind.Equals("Intermediate", StringComparison.OrdinalIgnoreCase) ? IntermediateQuantity(recipe) : Mod.Production.GetFinishedQuantity(recipe.Key);

    protected void DrawSmallButton(SpriteBatch b, Rectangle imageRect, string label, Color color)
    {
        Rectangle r = H(imageRect.X, imageRect.Y, imageRect.Width, imageRect.Height);
        Fill(b, r, color);
        Outline(b, r, new Color(86, 52, 24), 2);
        TextCentered(b, Game1.smallFont, label, imageRect, 0.62f, Color.White);
    }

    protected bool MouseOver(Rectangle imageRect)
    {
        Point p = Game1.getMousePosition();
        return H(imageRect.X, imageRect.Y, imageRect.Width, imageRect.Height).Contains(p.X, p.Y);
    }
}
