global using static AgriculturalCompany.Visual090Decor;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

internal static class Visual090Decor
{
    internal static void Leaf(SpriteBatch b, Rectangle r)
    {
        if (r.Width <= 0 || r.Height <= 0) return;
        Color dark = new(31, 82, 34);
        Color mid = new(69, 137, 54);
        Color light = new(112, 170, 70);
        int stem = Math.Max(2, r.Width / 8);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + r.Width / 2 - stem / 2, r.Y + r.Height / 5, stem, r.Height * 4 / 5), dark);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Y + r.Height / 4, r.Width / 2, r.Height / 3), mid);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + r.Width / 2, r.Y + r.Height / 12, r.Width / 2, r.Height / 3), light);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + r.Width / 5, r.Y + r.Height * 3 / 5, r.Width / 2, r.Height / 3), light);
    }
}
