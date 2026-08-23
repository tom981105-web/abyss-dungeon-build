using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

/// <summary>
/// 0.8.4 visual rebuild of the company production experience.
/// The layout follows the approved wood/cream/green reference and scales as one composition.
/// </summary>
internal abstract class Company084MenuBase : IClickableMenu
{
    protected const int DesignW = 1400;
    protected const int DesignH = 820;
    protected readonly ModEntry Mod;
    protected float Scale = 1f;
    protected int Ox;
    protected int Oy;

    protected static readonly Color WoodDeep = new(62, 36, 18);
    protected static readonly Color Wood = new(111, 65, 29);
    protected static readonly Color WoodMid = new(151, 96, 43);
    protected static readonly Color WoodLight = new(194, 137, 68);
    protected static readonly Color Cream = new(250, 235, 197);
    protected static readonly Color Cream2 = new(244, 223, 177);
    protected static readonly Color Cream3 = new(233, 204, 147);
    protected static readonly Color GreenDeep = new(27, 70, 28);
    protected static readonly Color Green = new(45, 104, 43);
    protected static readonly Color GreenBright = new(92, 151, 61);
    protected static readonly Color Gold = new(224, 171, 58);
    protected static readonly Color GoldLight = new(250, 211, 107);
    protected static readonly Color Ink = new(70, 49, 29);
    protected static readonly Color Muted = new(119, 91, 58);
    protected static readonly Color Blue = new(42, 91, 143);
    protected static readonly Color Orange = new(217, 119, 38);
    protected static readonly Color Red = new(165, 58, 43);
    protected static readonly Color Purple = new(131, 75, 150);

    protected Company084MenuBase(ModEntry mod) : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height, false)
    {
        Mod = mod;
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
        int uiW = Math.Max(720, Game1.uiViewport.Width);
        int uiH = Math.Max(520, Game1.uiViewport.Height);
        Scale = Math.Min((uiW - 18f) / DesignW, (uiH - 18f) / DesignH);
        Scale = Math.Clamp(Scale, 0.56f, 1.16f);
        int actualW = S(DesignW);
        int actualH = S(DesignH);
        Ox = (uiW - actualW) / 2;
        Oy = (uiH - actualH) / 2;
    }

    protected Rectangle D(int x, int y, int w, int h) => new(Ox + S(x), Oy + S(y), Math.Max(1, S(w)), Math.Max(1, S(h)));
    protected int S(int v) => (int)MathF.Round(v * Scale);
    protected static Rectangle Inset(Rectangle r, int n) => new(r.X + n, r.Y + n, Math.Max(1, r.Width - n * 2), Math.Max(1, r.Height - n * 2));
    protected static void Fill(SpriteBatch b, Rectangle r, Color c) => b.Draw(Game1.fadeToBlackRect, r, c);

    protected void Frame(SpriteBatch b)
    {
        Rectangle r = D(0, 0, DesignW, DesignH);
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(5)), Wood);
        Fill(b, Inset(r, S(11)), WoodMid);
        Fill(b, Inset(r, S(17)), Cream3);
        Fill(b, D(20, 86, 1360, 6), WoodDeep);
        Fill(b, D(20, 664, 1360, 6), WoodDeep);
        for (int x = 30; x < 1370; x += 72)
        {
            Fill(b, D(x, 6, 28, 5), new Color(82, 47, 21));
            Fill(b, D(x + 12, 811, 28, 4), new Color(82, 47, 21));
        }
    }

    protected void Paper(SpriteBatch b, Rectangle r, Color? fill = null)
    {
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(3)), WoodLight);
        Fill(b, Inset(r, S(7)), fill ?? Cream);
        Fill(b, new Rectangle(r.X + S(12), r.Y + S(10), Math.Max(1, r.Width - S(24)), Math.Max(1, S(2))), Color.White * 0.35f);
    }

    protected void Plaque(SpriteBatch b, Rectangle r, string text, float size = 0.82f)
    {
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(3)), Gold);
        Fill(b, Inset(r, S(7)), GreenDeep);
        Fill(b, new Rectangle(r.X + S(14), r.Y + S(8), Math.Max(1, r.Width - S(28)), S(3)), GreenBright * 0.5f);
        Stud(b, r.X + S(10), r.Y + S(10)); Stud(b, r.Right - S(15), r.Y + S(10));
        Stud(b, r.X + S(10), r.Bottom - S(15)); Stud(b, r.Right - S(15), r.Bottom - S(15));
        Text(b, Game1.dialogueFont, text, r, new Color(251, 221, 143), size, true);
    }

    protected void WoodButton(SpriteBatch b, Rectangle r, string text, bool primary = false, Color? fill = null)
    {
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(3)), primary ? GreenDeep : WoodMid);
        Fill(b, Inset(r, S(7)), fill ?? (primary ? Green : new Color(207, 151, 79)));
        Text(b, Game1.smallFont, text, r, primary ? Color.White : Ink, 0.95f, true);
    }

    protected void Text(SpriteBatch b, SpriteFont font, string value, Rectangle r, Color color, float rel, bool center = false)
    {
        value ??= "";
        float sc = Math.Max(0.08f, Scale * rel);
        string fitted = value;
        while (fitted.Length > 1 && font.MeasureString(fitted).X * sc > r.Width)
            fitted = fitted[..^1];
        if (fitted != value && fitted.Length > 1)
            fitted = fitted[..^1] + "…";
        Vector2 size = font.MeasureString(fitted) * sc;
        float x = center ? r.X + (r.Width - size.X) / 2f : r.X;
        float y = r.Y + (r.Height - size.Y) / 2f;
        b.DrawString(font, fitted, new Vector2(x, y), color, 0f, Vector2.Zero, sc, SpriteEffects.None, 1f);
    }

    protected void Progress(SpriteBatch b, Rectangle r, float value, Color? color = null)
    {
        value = Math.Clamp(value, 0f, 1f);
        Fill(b, r, new Color(93, 75, 49));
        Rectangle inner = Inset(r, S(2));
        Fill(b, inner, new Color(211, 198, 153));
        int w = Math.Max(0, (int)(inner.Width * value));
        if (w > 0)
        {
            Fill(b, new Rectangle(inner.X, inner.Y, w, inner.Height), color ?? Green);
            Fill(b, new Rectangle(inner.X, inner.Y, w, Math.Max(1, S(3))), GreenBright);
        }
    }

    protected void Dots(SpriteBatch b, Rectangle r)
    {
        int step = Math.Max(3, S(8));
        int dot = Math.Max(1, S(2));
        for (int x = r.X; x < r.Right; x += step)
            Fill(b, new Rectangle(x, r.Y, dot, Math.Max(1, r.Height)), new Color(188, 148, 92));
    }

    protected void Grade(SpriteBatch b, Rectangle r, string grade)
    {
        string g = string.IsNullOrWhiteSpace(grade) ? "C" : grade.Trim().ToUpperInvariant();
        Color c = g switch { "S" => Purple, "A" => new Color(116, 159, 65), "B" => new Color(73, 127, 173), _ => new Color(189, 124, 61) };
        Fill(b, r, WoodDeep); Fill(b, Inset(r, S(2)), c); Fill(b, Inset(r, S(5)), Cream);
        Text(b, Game1.smallFont, $"{g}급", r, Ink, 0.72f, true);
    }

    protected void StatusPill(SpriteBatch b, Rectangle r, string text, bool active)
    {
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(2)), active ? Green : new Color(125, 103, 72));
        Text(b, Game1.smallFont, text, r, Color.White, 0.72f, true);
    }

    protected void Coin(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(131, 80, 18)); Fill(b, Inset(r, S(4)), Gold); Fill(b, Inset(r, S(9)), GoldLight);
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y + S(8), S(6), r.Height - S(16)), new Color(169, 101, 20));
        Fill(b, new Rectangle(r.X + S(9), r.Y + r.Height / 2 - S(3), r.Width - S(18), S(6)), new Color(169, 101, 20));
    }

    protected void Shield(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X + S(4), r.Y, r.Width - S(8), r.Height - S(9)), Gold);
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(4), r.Width - S(16), r.Height - S(16)), Green);
        Star(b, new Rectangle(r.X + r.Width / 2 - S(7), r.Y + S(10), S(14), S(14)), GoldLight);
        TriangleDown(b, new Rectangle(r.X + S(8), r.Bottom - S(16), r.Width - S(16), S(14)), Gold);
    }

    protected void Scroll(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X + S(7), r.Y + S(5), r.Width - S(14), r.Height - S(10)), new Color(244, 218, 161));
        Fill(b, new Rectangle(r.X + S(3), r.Y + S(2), r.Width - S(6), S(8)), Wood);
        Fill(b, new Rectangle(r.X + S(3), r.Bottom - S(10), r.Width - S(6), S(8)), Wood);
        Fill(b, new Rectangle(r.X + S(13), r.Y + S(18), r.Width - S(26), S(3)), Muted);
        Fill(b, new Rectangle(r.X + S(13), r.Y + S(29), r.Width - S(22), S(3)), Muted);
    }

    protected void Heart(SpriteBatch b, Rectangle r)
    {
        Color p = new(214, 64, 103);
        Fill(b, new Rectangle(r.X + S(4), r.Y + S(8), S(15), S(15)), p);
        Fill(b, new Rectangle(r.Right - S(19), r.Y + S(8), S(15), S(15)), p);
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(15), r.Width - S(16), S(16)), p);
        TriangleDown(b, new Rectangle(r.X + S(8), r.Y + S(30), r.Width - S(16), S(13)), p);
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(8), S(5), S(5)), new Color(255, 170, 188));
    }

    protected void Machine(SpriteBatch b, Rectangle r, string type, bool active)
    {
        Color metal = active ? new Color(91, 103, 88) : new Color(121, 112, 92);
        Color dark = new(55, 51, 43);
        Color glow = active ? new Color(83, 157, 70) : new Color(158, 139, 96);
        Fill(b, new Rectangle(r.X + S(4), r.Bottom - S(12), r.Width - S(8), S(8)), dark);
        if (type == "Fermentation")
        {
            Tank(b, new Rectangle(r.X + S(5), r.Y + S(10), S(52), r.Height - S(24)), active);
            Tank(b, new Rectangle(r.X + S(58), r.Y + S(4), S(52), r.Height - S(18)), active);
            Fill(b, new Rectangle(r.X + S(25), r.Y + S(3), S(62), S(6)), dark);
        }
        else if (type == "Packaging")
        {
            Fill(b, new Rectangle(r.X + S(8), r.Y + S(30), r.Width - S(16), S(15)), dark);
            Fill(b, new Rectangle(r.X + S(13), r.Y + S(18), S(17), S(48)), metal);
            Fill(b, new Rectangle(r.Right - S(30), r.Y + S(18), S(17), S(48)), metal);
            Box(b, new Rectangle(r.X + r.Width / 2 - S(20), r.Y + S(10), S(40), S(40)));
            Fill(b, new Rectangle(r.X + S(15), r.Bottom - S(22), r.Width - S(30), S(5)), glow);
        }
        else
        {
            Fill(b, new Rectangle(r.X + S(6), r.Y + S(26), S(34), S(43)), metal);
            Fill(b, new Rectangle(r.X + S(12), r.Y + S(34), S(22), S(10)), glow);
            Tank(b, new Rectangle(r.X + S(42), r.Y + S(11), S(53), S(61)), active);
            Fill(b, new Rectangle(r.X + S(95), r.Y + S(37), S(13), S(30)), dark);
            Bottle(b, new Rectangle(r.X + S(99), r.Y + S(20), S(14), S(34)), Green);
        }
    }

    protected void ProcessIcon(SpriteBatch b, Rectangle r, string name, bool active)
    {
        string n = name ?? "";
        if (n.Contains("세척"))
        {
            Fill(b, new Rectangle(r.X + S(7), r.Y + S(27), r.Width - S(14), S(19)), new Color(79, 113, 126));
            Fill(b, new Rectangle(r.X + S(11), r.Y + S(31), r.Width - S(22), S(5)), new Color(128, 191, 207));
            Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y + S(5), S(6), S(22)), new Color(88, 95, 92));
            Drop(b, new Rectangle(r.X + S(14), r.Y + S(13), S(8), S(12)));
            Drop(b, new Rectangle(r.Right - S(22), r.Y + S(13), S(8), S(12)));
        }
        else if (n.Contains("병입") || n.Contains("음료"))
        {
            Bottle(b, new Rectangle(r.X + r.Width / 2 - S(11), r.Y + S(4), S(22), r.Height - S(8)), active ? new Color(68, 133, 65) : Green);
            Fill(b, new Rectangle(r.X + S(5), r.Y + S(10), S(11), S(7)), Wood);
        }
        else if (n.Contains("포장") || n.Contains("세트"))
            Box(b, Inset(r, S(8)));
        else if (n.Contains("살균") || n.Contains("가열"))
        {
            Tank(b, Inset(r, S(5)), active);
            Flame(b, new Rectangle(r.X + r.Width / 2 - S(9), r.Bottom - S(16), S(18), S(15)));
        }
        else if (n.Contains("숙성") || n.Contains("발효") || n.Contains("염장"))
        {
            Tank(b, Inset(r, S(5)), active);
            Fill(b, new Rectangle(r.X + S(10), r.Y + S(13), r.Width - S(20), S(5)), Wood);
        }
        else
        {
            Fill(b, new Rectangle(r.X + S(8), r.Y + S(18), r.Width - S(16), r.Height - S(23)), new Color(79, 85, 75));
            Fill(b, new Rectangle(r.X + r.Width / 2 - S(4), r.Y + S(4), S(8), S(25)), new Color(58, 61, 56));
            Fill(b, new Rectangle(r.X + S(13), r.Y + S(31), r.Width - S(26), S(8)), active ? GreenBright : new Color(139, 128, 94));
        }
    }

    protected void Tank(SpriteBatch b, Rectangle r, bool active)
    {
        Fill(b, new Rectangle(r.X + S(3), r.Y + S(5), r.Width - S(6), r.Height - S(7)), new Color(54, 54, 48));
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(10), r.Width - S(16), r.Height - S(16)), active ? new Color(96, 109, 91) : new Color(119, 110, 91));
        Fill(b, new Rectangle(r.X + S(2), r.Y + S(4), r.Width - S(4), S(7)), new Color(45, 45, 41));
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(18), r.Width - S(16), S(4)), active ? GreenBright : new Color(161, 138, 86));
        Fill(b, new Rectangle(r.X + S(10), r.Bottom - S(6), S(5), S(8)), WoodDeep);
        Fill(b, new Rectangle(r.Right - S(15), r.Bottom - S(6), S(5), S(8)), WoodDeep);
    }

    protected void Bottle(SpriteBatch b, Rectangle r, Color liquid)
    {
        Fill(b, new Rectangle(r.X + r.Width / 3, r.Y, r.Width / 3, Math.Max(S(4), r.Height / 5)), Wood);
        Fill(b, new Rectangle(r.X + S(2), r.Y + r.Height / 5, r.Width - S(4), r.Height - r.Height / 5), new Color(223, 231, 211));
        Fill(b, new Rectangle(r.X + S(4), r.Y + r.Height / 2, r.Width - S(8), r.Height / 2 - S(4)), liquid);
        Fill(b, new Rectangle(r.X + S(3), r.Y + r.Height / 2 - S(4), r.Width - S(6), S(7)), new Color(244, 224, 174));
    }

    protected void Box(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(113, 70, 30)); Fill(b, Inset(r, S(3)), new Color(222, 157, 52));
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y, S(6), r.Height), new Color(244, 196, 70));
        Fill(b, new Rectangle(r.X, r.Y + r.Height / 2 - S(3), r.Width, S(6)), new Color(244, 196, 70));
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(10), r.Y - S(4), S(20), S(10)), Red);
    }

    protected void Star(SpriteBatch b, Rectangle r, Color c)
    {
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y, S(6), r.Height), c);
        Fill(b, new Rectangle(r.X, r.Y + r.Height / 2 - S(3), r.Width, S(6)), c);
        Fill(b, Inset(r, S(5)), c);
    }

    protected void TriangleDown(SpriteBatch b, Rectangle r, Color c)
    {
        int steps = Math.Max(1, r.Height);
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)Math.Max(1, steps - 1);
            int w = Math.Max(1, (int)(r.Width * (1f - t)));
            Fill(b, new Rectangle(r.X + (r.Width - w) / 2, r.Y + i, w, 1), c);
        }
    }

    protected void TriangleRight(SpriteBatch b, Rectangle r, Color c)
    {
        int steps = Math.Max(1, r.Width);
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)Math.Max(1, steps - 1);
            int h = Math.Max(1, (int)(r.Height * (1f - Math.Abs(t * 2f - 1f))));
            Fill(b, new Rectangle(r.X + i, r.Y + (r.Height - h) / 2, 1, h), c);
        }
    }

    protected void Arrow(SpriteBatch b, Rectangle r)
    {
        int cy = r.Y + r.Height / 2;
        Fill(b, new Rectangle(r.X, cy - S(2), Math.Max(1, r.Width - S(8)), S(4)), Green);
        TriangleRight(b, new Rectangle(r.Right - S(10), cy - S(8), S(10), S(16)), Green);
    }

    protected void Flame(SpriteBatch b, Rectangle r)
    {
        TriangleDown(b, r, Orange);
        Fill(b, Inset(r, S(5)), GoldLight);
    }

    protected void Drop(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X + S(2), r.Y + r.Height / 3, r.Width - S(4), r.Height * 2 / 3), new Color(78, 157, 194));
        TriangleDown(b, new Rectangle(r.X + S(2), r.Y, r.Width - S(4), r.Height / 2), new Color(116, 196, 225));
    }

    protected void Stud(SpriteBatch b, int x, int y) => Fill(b, new Rectangle(x, y, Math.Max(2, S(5)), Math.Max(2, S(5))), Gold);

    protected string CompanyName() => string.IsNullOrWhiteSpace(Mod.State.CompanyName) ? "새별 농업" : Mod.State.CompanyName;
    protected static string LineName(string type) => type switch { "Beverage" => "음료", "Packaging" => "포장", "Fermentation" => "발효", _ => type };
}
