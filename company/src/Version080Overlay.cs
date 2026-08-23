using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class Version080Overlay
{
    private readonly ModEntry Mod;

    internal Version080Overlay(ModEntry mod)
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

        Color sidebar = new(48, 78, 58);
        Color light = new(215, 228, 210);
        Color accent = new(90, 128, 76);
        Rectangle versionArea = new(menu.xPositionOnScreen + 20, menu.yPositionOnScreen + 57, 185, 34);
        e.SpriteBatch.Draw(Game1.fadeToBlackRect, versionArea, sidebar);
        e.SpriteBatch.DrawString(Game1.smallFont, "COMPANY 0.8.4", new Vector2(menu.xPositionOnScreen + 27, menu.yPositionOnScreen + 67), light);

        int x = menu.xPositionOnScreen + 250;
        int noteY = menu.yPositionOnScreen + 496;
        Rectangle notePatch = new(x + 12, noteY + 8, Math.Max(200, menu.width - 325), 27);
        e.SpriteBatch.Draw(Game1.fadeToBlackRect, notePatch, Color.White);
        e.SpriteBatch.DrawString(Game1.smallFont, "Agricultural Company 0.8.4 · Production UI Visual Upgrade", new Vector2(x + 18, noteY + 14), accent);
    }
}
