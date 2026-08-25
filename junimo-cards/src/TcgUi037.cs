using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

internal static class TcgUi037
{
    internal static readonly Color TestGold = new(230, 177, 61);

    internal static void Begin(SpriteBatch b, IClickableMenu menu, string title, string subtitle = "")
    {
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Black * 0.52f);
        IClickableMenu.drawTextureBox(b, menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height, Color.White);

        Rectangle header = new(menu.xPositionOnScreen + 18, menu.yPositionOnScreen + 14, menu.width - 36, 82);
        IClickableMenu.drawTextureBox(b, header.X, header.Y, header.Width, header.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(header.X + 7, header.Y + 7, header.Width - 14, header.Height - 14),
            CardUi.GreenDark);

        CardUi.CenterText(b, Game1.dialogueFont, title,
            new Rectangle(header.X + 14, header.Y + 3, header.Width - 28, 46),
            Color.White, 1.05f);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            CardUi.CenterText(b, Game1.smallFont, subtitle,
                new Rectangle(header.X + 14, header.Y + 49, header.Width - 28, 27),
                new Color(245, 235, 181), 1.36f);
        }
    }

    internal static void Button(SpriteBatch b, Rectangle r, string text, bool enabled = true, bool green = false, bool selected = false)
    {
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
        Color fill = !enabled
            ? new Color(139, 132, 117)
            : green
                ? CardUi.Green
                : selected
                    ? new Color(194, 132, 45)
                    : new Color(172, 118, 50);
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14),
            fill * 0.90f);
        CardUi.CenterText(b, Game1.dialogueFont, text,
            new Rectangle(r.X + 8, r.Y + 6, r.Width - 16, r.Height - 12),
            enabled ? Color.White : new Color(224, 220, 207), 0.90f);
    }

    internal static void MiniButton(SpriteBatch b, Rectangle r, string text, bool enabled = true, bool selected = false)
    {
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
        Color fill = !enabled
            ? new Color(139, 132, 117)
            : selected ? CardUi.Green : new Color(172, 118, 50);
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(r.X + 6, r.Y + 6, r.Width - 12, r.Height - 12),
            fill * 0.90f);
        CardUi.CenterText(b, Game1.smallFont, text,
            new Rectangle(r.X + 7, r.Y + 5, r.Width - 14, r.Height - 10),
            enabled ? Color.White : new Color(224, 220, 207), 1.30f);
    }

    internal static Rectangle ScaleAroundCenter(Rectangle r, float sx, float sy)
    {
        int w = Math.Max(2, (int)Math.Round(r.Width * sx));
        int h = Math.Max(2, (int)Math.Round(r.Height * sy));
        return new Rectangle(r.Center.X - w / 2, r.Center.Y - h / 2, w, h);
    }

    internal static float SmoothStep(float p)
    {
        p = Math.Clamp(p, 0f, 1f);
        return p * p * (3f - 2f * p);
    }

    internal static float EaseOutBack(float p)
    {
        p = Math.Clamp(p, 0f, 1f);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * MathF.Pow(p - 1f, 3f) + c1 * MathF.Pow(p - 1f, 2f);
    }

    internal static Color RainbowColor(int seed, float time)
    {
        float h = (seed * 0.137f + time * 0.18f) % 1f;
        if (h < 0f)
            h += 1f;

        float s = 0.72f;
        float v = 1f;
        float c = v * s;
        float hp = h * 6f;
        float x = c * (1f - MathF.Abs(hp % 2f - 1f));
        float r = 0f, g = 0f, b = 0f;

        if (hp < 1f) { r = c; g = x; }
        else if (hp < 2f) { r = x; g = c; }
        else if (hp < 3f) { g = c; b = x; }
        else if (hp < 4f) { g = x; b = c; }
        else if (hp < 5f) { r = x; b = c; }
        else { r = c; b = x; }

        float m = v - c;
        return new Color(r + m, g + m, b + m);
    }
}
