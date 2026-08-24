using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

/// <summary>
/// 0.9.2 image-backed UI base. The authored reference PNG is the UI; code only scales it
/// and maps image-space hit boxes to the current viewport.
/// </summary>
internal abstract class ImageBackedUi092Base : IClickableMenu
{
    protected const int ImageW = 1672;
    protected const int ImageH = 941;
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
            Mod.Monitor.Log($"0.9.2 image-backed UI asset failed to load: {assetPath}. {ex.Message}", StardewModdingAPI.LogLevel.Error);
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
        UiScale = Math.Min(vw / (float)ImageW, vh / (float)ImageH);
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
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.72f);
        if (Background is null)
        {
            b.DrawString(Game1.dialogueFont, "UI 이미지 자산을 불러오지 못했습니다.", new Vector2(48, 48), Color.White);
            return;
        }
        Rectangle dest = new(UiX, UiY, Math.Max(1, (int)MathF.Round(ImageW * UiScale)), Math.Max(1, (int)MathF.Round(ImageH * UiScale)));
        b.Draw(Background, dest, Color.White);
    }
}
