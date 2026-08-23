using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class VersionLabelOverlay
{
    private const string VersionText = "0.7.5";
    private readonly ModEntry Mod;

    internal VersionLabelOverlay(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Game1.activeClickableMenu is not CompanyMenu menu)
            return;

        DrawVersion(e.SpriteBatch, menu);
    }

    private static void DrawVersion(SpriteBatch b, CompanyMenu menu)
    {
        Color sidebar = new(48, 78, 58);
        Color light = new(215, 228, 210);
        Color accent = new(90, 128, 76);

        Rectangle versionArea = new(menu.xPositionOnScreen + 20, menu.yPositionOnScreen + 57, 185, 34);
        b.Draw(Game1.fadeToBlackRect, versionArea, sidebar);
        b.DrawString(Game1.smallFont, $"COMPANY {VersionText}", new Vector2(menu.xPositionOnScreen + 27, menu.yPositionOnScreen + 67), light);

        int x = menu.xPositionOnScreen + 250;
        int noteY = menu.yPositionOnScreen + 496;
        Rectangle notePatch = new(x + 12, noteY + 8, Math.Max(200, menu.width - 325), 27);
        b.Draw(Game1.fadeToBlackRect, notePatch, Color.White);
        b.DrawString(Game1.smallFont, $"Agricultural Company {VersionText} · Production 2.x", new Vector2(x + 18, noteY + 14), accent);
    }
}
