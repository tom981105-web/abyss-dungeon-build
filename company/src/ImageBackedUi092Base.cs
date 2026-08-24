using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

/// <summary>
/// Image-backed UI base. The authored reference PNG remains the visible UI.
/// 0.9.3 renders it at 88% of the available viewport and supplies lightweight
/// hover/selection/toast feedback so invisible hit boxes feel responsive.
/// </summary>
internal abstract class ImageBackedUi092Base : IClickableMenu
{
    protected const int ImageW = 1672;
    protected const int ImageH = 941;
    private const float ViewportUsage = 0.88f;

    protected readonly ModEntry Mod;
    protected Texture2D? Background;
    protected float UiScale = 1f;
    protected int UiX;
    protected int UiY;

    protected ImageBackedUi092Base(ModEntry mod, string assetPath)
        : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height, false)
    {
        Mod = mod;
        try { Background = Mod.Helper.ModContent.Load<Texture2D>(assetPath); }
        catch (Exception ex)
        {
            Background = null;
            Mod.Monitor.Log($"Image-backed UI asset failed to load: {assetPath}. {ex.Message}", StardewModdingAPI.LogLevel.Error);
        }
        Recalc();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = Game1.uiViewport.Width;
        height = Game1.uiViewport.Height;
        Recalc();
    }

    protected void Recalc()
    {
        int vw = Math.Max(640, Game1.uiViewport.Width);
        int vh = Math.Max(360, Game1.uiViewport.Height);
        float targetW = vw * ViewportUsage;
        float targetH = vh * ViewportUsage;
        UiScale = Math.Min(targetW / ImageW, targetH / ImageH);
        UiScale = Math.Max(0.20f, UiScale);

        int dw = Math.Max(1, (int)MathF.Round(ImageW * UiScale));
        int dh = Math.Max(1, (int)MathF.Round(ImageH * UiScale));
        UiX = (vw - dw) / 2;
        UiY = (vh - dh) / 2;
    }

    protected Rectangle H(int x, int y, int w, int h)
        => new(
            UiX + (int)MathF.Round(x * UiScale),
            UiY + (int)MathF.Round(y * UiScale),
            Math.Max(1, (int)MathF.Round(w * UiScale)),
            Math.Max(1, (int)MathF.Round(h * UiScale))
        );

    protected void DrawImage(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.68f);
        if (Background is null)
        {
            b.DrawString(Game1.dialogueFont, "UI 이미지 자산을 불러오지 못했습니다.", new Vector2(48, 48), Color.White);
            return;
        }

        Rectangle dest = new(UiX, UiY, Math.Max(1, (int)MathF.Round(ImageW * UiScale)), Math.Max(1, (int)MathF.Round(ImageH * UiScale)));
        b.Draw(Background, dest, Color.White);
    }

    protected bool Hover(Rectangle r)
        => r.Contains(Game1.getMouseX(), Game1.getMouseY());

    protected void DrawHover(SpriteBatch b, Rectangle r)
    {
        if (!Hover(r))
            return;
        Fill(b, r, new Color(255, 214, 91) * 0.10f);
        Outline(b, r, new Color(255, 218, 90) * 0.95f, Math.Max(2, (int)MathF.Round(3 * UiScale)));
    }

    protected void DrawSelected(SpriteBatch b, Rectangle r)
        => Outline(b, r, new Color(255, 191, 52) * 0.95f, Math.Max(2, (int)MathF.Round(4 * UiScale)));

    protected void DrawToast(SpriteBatch b, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        float scale = Math.Clamp(UiScale * 1.10f, 0.55f, 1.05f);
        Vector2 size = Game1.smallFont.MeasureString(text) * scale;
        int padX = Math.Max(12, (int)MathF.Round(18 * UiScale));
        int padY = Math.Max(7, (int)MathF.Round(10 * UiScale));
        int w = Math.Min((int)(Game1.uiViewport.Width * 0.70f), (int)MathF.Ceiling(size.X) + padX * 2);
        int h = (int)MathF.Ceiling(size.Y) + padY * 2;
        int x = (Game1.uiViewport.Width - w) / 2;
        int y = Math.Min(Game1.uiViewport.Height - h - 18, UiY + Math.Max(1, (int)MathF.Round(ImageH * UiScale)) - h - 10);
        Rectangle box = new(x, y, w, h);
        Fill(b, box, new Color(44, 29, 15) * 0.93f);
        Outline(b, box, new Color(232, 174, 61), 2);
        Vector2 pos = new(box.X + (box.Width - size.X) / 2f, box.Y + (box.Height - size.Y) / 2f);
        b.DrawString(Game1.smallFont, text, pos, new Color(255, 238, 190), 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    private static void Fill(SpriteBatch b, Rectangle r, Color c)
        => b.Draw(Game1.fadeToBlackRect, r, c);

    private static void Outline(SpriteBatch b, Rectangle r, Color c, int t)
    {
        t = Math.Max(1, t);
        Fill(b, new Rectangle(r.X, r.Y, r.Width, t), c);
        Fill(b, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
        Fill(b, new Rectangle(r.X, r.Y, t, r.Height), c);
        Fill(b, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
    }
}
