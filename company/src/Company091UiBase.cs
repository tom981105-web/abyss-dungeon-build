using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

/// <summary>
/// 0.9.1 reference-fidelity UI base.
/// Designed natively around a 1280x720 canvas so the production UI doesn't become tiny
/// on the user's common window size. All visual assets are standalone PNGs with safe fallbacks.
/// </summary>
internal abstract class Company091UiBase : IClickableMenu
{
    protected const int DesignW = 1280;
    protected const int DesignH = 720;
    protected readonly ModEntry Mod;
    protected float Scale = 1f;
    protected int Ox;
    protected int Oy;
    protected Texture2D? Skin;
    protected Texture2D? Atlas;

    protected static readonly Color WoodDeep = new(58, 31, 15);
    protected static readonly Color Wood = new(111, 64, 28);
    protected static readonly Color WoodLight = new(190, 132, 65);
    protected static readonly Color Cream = new(247, 229, 188);
    protected static readonly Color Cream2 = new(241, 217, 169);
    protected static readonly Color GreenDeep = new(24, 67, 28);
    protected static readonly Color Green = new(46, 108, 47);
    protected static readonly Color GreenBright = new(100, 164, 75);
    protected static readonly Color Gold = new(226, 173, 58);
    protected static readonly Color GoldLight = new(250, 214, 111);
    protected static readonly Color Ink = new(67, 46, 28);
    protected static readonly Color Muted = new(116, 88, 57);
    protected static readonly Color Blue = new(42, 94, 147);
    protected static readonly Color Orange = new(218, 122, 40);
    protected static readonly Color Red = new(166, 57, 42);
    protected static readonly Color Purple = new(132, 76, 152);

    protected Company091UiBase(ModEntry mod)
        : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height, false)
    {
        Mod = mod;
        try { Skin = Mod.Helper.ModContent.Load<Texture2D>("assets/ui_skin_091.png"); }
        catch
        {
            try { Skin = Mod.Helper.ModContent.Load<Texture2D>("assets/ui_skin_090.png"); }
            catch { Skin = null; }
        }
        try { Atlas = Mod.Helper.ModContent.Load<Texture2D>("assets/production_visuals_091.png"); }
        catch
        {
            try { Atlas = Mod.Helper.ModContent.Load<Texture2D>("assets/production_visuals_087.png"); }
            catch { Atlas = null; }
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
        int uiW = Math.Max(800, Game1.uiViewport.Width);
        int uiH = Math.Max(540, Game1.uiViewport.Height);
        Scale = Math.Min((uiW - 8f) / DesignW, (uiH - 8f) / DesignH);
        Scale = Math.Clamp(Scale, 0.72f, 1.18f);
        int actualW = S(DesignW);
        int actualH = S(DesignH);
        Ox = (uiW - actualW) / 2;
        Oy = (uiH - actualH) / 2;
    }

    protected int S(int v) => (int)MathF.Round(v * Scale);
    protected Rectangle D(int x, int y, int w, int h)
        => new(Ox + S(x), Oy + S(y), Math.Max(1, S(w)), Math.Max(1, S(h)));
    protected static Rectangle Inset(Rectangle r, int n)
        => new(r.X + n, r.Y + n, Math.Max(1, r.Width - n * 2), Math.Max(1, r.Height - n * 2));
    protected static void Fill(SpriteBatch b, Rectangle r, Color c) => b.Draw(Game1.fadeToBlackRect, r, c);

    protected void Frame(SpriteBatch b)
    {
        Rectangle all = D(0, 0, DesignW, DesignH);
        Fill(b, all, new Color(45, 25, 12));
        Tile(b, D(5, 5, 1270, 710), 0, Color.White);
        Fill(b, D(14, 72, 1252, 5), WoodDeep);
        Fill(b, D(14, 590, 1252, 5), WoodDeep);
        for (int x = 16; x < 1270; x += 250)
        {
            Stud(b, D(x, 7, 8, 8));
            Stud(b, D(x, 706, 8, 8));
        }
    }

    protected void Panel(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, WoodDeep);
        Tile(b, Inset(r, S(3)), 0, Color.White * 0.90f);
        Tile(b, Inset(r, S(8)), 1, Color.White);
    }

    protected void Card(SpriteBatch b, Rectangle r, Color? tint = null)
    {
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(3)), Gold * 0.88f);
        Tile(b, Inset(r, S(6)), 1, Color.White);
        if (tint.HasValue)
            Fill(b, Inset(r, S(7)), tint.Value * 0.20f);
    }

    protected void Plaque(SpriteBatch b, Rectangle r, string text, float rel = 0.95f)
    {
        Fill(b, r, WoodDeep);
        Tile(b, Inset(r, S(3)), 3, Color.White);
        Tile(b, Inset(r, S(7)), 2, Color.White);
        Stud(b, new Rectangle(r.X + S(8), r.Y + S(8), S(7), S(7)));
        Stud(b, new Rectangle(r.Right - S(15), r.Y + S(8), S(7), S(7)));
        Stud(b, new Rectangle(r.X + S(8), r.Bottom - S(15), S(7), S(7)));
        Stud(b, new Rectangle(r.Right - S(15), r.Bottom - S(15), S(7), S(7)));
        Text(b, Game1.dialogueFont, text, r, new Color(252, 222, 145), rel, true);
    }

    protected void Button(SpriteBatch b, Rectangle r, string text, bool primary = false, Color? fill = null)
    {
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(3)), Gold * 0.86f);
        Fill(b, Inset(r, S(6)), fill ?? (primary ? Green : new Color(205, 148, 76)));
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(7), Math.Max(1, r.Width - S(16)), S(3)), Color.White * 0.25f);
        Text(b, Game1.smallFont, text, r, primary || fill.HasValue ? Color.White : Ink, 0.94f, true);
    }

    protected void Text(SpriteBatch b, SpriteFont font, string value, Rectangle r, Color color, float rel, bool center = false)
    {
        value ??= "";
        float sc = Math.Max(0.10f, Scale * rel * 1.08f);
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
        Fill(b, r, new Color(77, 60, 39));
        Rectangle inner = Inset(r, S(2));
        Fill(b, inner, new Color(213, 199, 151));
        int w = Math.Max(0, (int)MathF.Round(inner.Width * value));
        if (w > 0)
        {
            Fill(b, new Rectangle(inner.X, inner.Y, w, inner.Height), color ?? Green);
            Fill(b, new Rectangle(inner.X, inner.Y, w, Math.Max(1, S(3))), GreenBright);
        }
    }

    protected void Dots(SpriteBatch b, Rectangle r)
    {
        int step = Math.Max(4, S(8));
        int dot = Math.Max(1, S(2));
        for (int x = r.X; x < r.Right; x += step)
            Fill(b, new Rectangle(x, r.Y, dot, Math.Max(1, r.Height)), new Color(186, 145, 87));
    }

    protected void Grade(SpriteBatch b, Rectangle r, string grade)
    {
        string g = string.IsNullOrWhiteSpace(grade) ? "C" : grade.Trim().ToUpperInvariant();
        Color c = g switch
        {
            "S" => Purple,
            "A" => new Color(116, 159, 65),
            "B" => new Color(73, 127, 173),
            _ => new Color(189, 124, 61)
        };
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(2)), c);
        Fill(b, Inset(r, S(5)), Cream);
        Text(b, Game1.smallFont, $"{g}급", r, Ink, 0.78f, true);
    }

    protected void Status(SpriteBatch b, Rectangle r, string text, bool active)
    {
        Fill(b, r, WoodDeep);
        Fill(b, Inset(r, S(2)), active ? Green : new Color(124, 102, 72));
        Text(b, Game1.smallFont, text, r, Color.White, 0.76f, true);
    }

    protected void Tile(SpriteBatch b, Rectangle dest, int tile, Color tint)
    {
        if (Skin is null)
        {
            Fill(b, dest, tile == 0 ? Wood : tile == 2 ? GreenDeep : tile == 3 ? Gold : Cream);
            return;
        }
        int sourceTile = Skin.Width >= 512 ? 128 : 64;
        int maxTile = Math.Max(1, Skin.Width / sourceTile);
        tile = Math.Clamp(tile, 0, maxTile - 1);
        Rectangle srcBase = new(tile * sourceTile, 0, sourceTile, Math.Min(sourceTile, Skin.Height));
        int step = Math.Max(1, S(sourceTile));
        for (int y = dest.Y; y < dest.Bottom; y += step)
        {
            for (int x = dest.X; x < dest.Right; x += step)
            {
                int w = Math.Min(step, dest.Right - x);
                int h = Math.Min(step, dest.Bottom - y);
                int sw = Math.Min(sourceTile, Math.Max(1, (int)MathF.Round(w / Scale)));
                int sh = Math.Min(srcBase.Height, Math.Max(1, (int)MathF.Round(h / Scale)));
                b.Draw(Skin, new Rectangle(x, y, w, h), new Rectangle(srcBase.X, srcBase.Y, sw, sh), tint);
            }
        }
    }

    protected void DrawAtlas(SpriteBatch b, int index, Rectangle dest, float alpha = 1f)
    {
        if (Atlas is null || index < 0 || index >= 16)
            return;
        int cell = Atlas.Width >= 768 ? 192 : 128;
        Rectangle src = new((index % 4) * cell, (index / 4) * cell, cell, cell);
        b.Draw(Atlas, dest, src, Color.White * alpha);
    }

    protected void DrawProduct(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle dest, float alpha = 1f)
    {
        if (Atlas is null)
        {
            Mod.Icons.DrawRecipeIcon(b, recipe, dest, alpha);
            return;
        }
        DrawAtlas(b, ProductSprite(recipe), dest, alpha);
    }

    protected static int MachineSprite(string type) => type switch
    {
        "Fermentation" => 1,
        "Packaging" => 2,
        _ => 0
    };

    protected static int ProcessSprite(string? stage)
    {
        string s = stage ?? "";
        if (s.Contains("세척")) return 4;
        if (s.Contains("착즙") || s.Contains("압착") || s.Contains("파쇄") || s.Contains("분쇄") || s.Contains("절단")) return 5;
        if (s.Contains("살균") || s.Contains("가열") || s.Contains("숙성") || s.Contains("발효") || s.Contains("염장")) return 6;
        if (s.Contains("병입")) return 7;
        if (s.Contains("포장") || s.Contains("세트")) return 11;
        return 5;
    }

    protected static int ProductSprite(ProductionRecipeDefinition recipe)
    {
        string n = recipe.DisplayName ?? "";
        string k = recipe.Key ?? "";
        if (n.Contains("토마토주스") || k.Contains("TomatoJuice", StringComparison.OrdinalIgnoreCase)) return 9;
        if (n.Contains("수박주스") || k.Contains("WatermelonJuice", StringComparison.OrdinalIgnoreCase)) return 10;
        if (n.Contains("잼") || k.Contains("Jam", StringComparison.OrdinalIgnoreCase)) return 15;
        if (n.Contains("선물세트") || n.Contains("선물 세트")) return 11;
        if (n.Contains("펄프")) return 12;
        if (n.Contains("절임") || n.Contains("피클")) return 13;
        if (n.Contains("밀가루") || n.Contains("분말") || n.Contains("가루")) return 14;
        if (n.Contains("주스") || n.Contains("원액")) return 8;
        if (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase))
            return n.Contains("세척") ? 3 : 5;
        return 8;
    }

    protected void Arrow(SpriteBatch b, Rectangle r)
    {
        int cy = r.Y + r.Height / 2;
        Fill(b, new Rectangle(r.X, cy - S(2), Math.Max(1, r.Width - S(9)), S(4)), Green);
        for (int i = 0; i < S(10); i++)
        {
            int h = Math.Max(1, S(16) - Math.Abs(i - S(5)) * 2);
            Fill(b, new Rectangle(r.Right - S(10) + i, cy - h / 2, 1, h), Green);
        }
    }

    protected void Coin(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(129, 79, 18));
        Fill(b, Inset(r, S(4)), Gold);
        Fill(b, Inset(r, S(9)), GoldLight);
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y + S(8), S(6), r.Height - S(16)), new Color(166, 98, 19));
        Fill(b, new Rectangle(r.X + S(9), r.Y + r.Height / 2 - S(3), r.Width - S(18), S(6)), new Color(166, 98, 19));
    }

    protected void Shield(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X + S(4), r.Y, r.Width - S(8), r.Height - S(9)), Gold);
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(4), r.Width - S(16), r.Height - S(17)), Green);
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y + S(12), S(6), S(18)), GoldLight);
        Fill(b, new Rectangle(r.X + S(14), r.Y + S(18), r.Width - S(28), S(6)), GoldLight);
    }

    protected void ScrollIcon(SpriteBatch b, Rectangle r)
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
        Fill(b, new Rectangle(r.X + S(4), r.Y + S(8), S(16), S(16)), p);
        Fill(b, new Rectangle(r.Right - S(20), r.Y + S(8), S(16), S(16)), p);
        Fill(b, new Rectangle(r.X + S(9), r.Y + S(15), r.Width - S(18), S(17)), p);
        for (int i = 0; i < S(14); i++)
        {
            int w = Math.Max(1, r.Width - S(18) - i * 2);
            Fill(b, new Rectangle(r.X + (r.Width - w) / 2, r.Y + S(31) + i, w, 1), p);
        }
    }

    protected void Star(SpriteBatch b, Rectangle r, Color c)
    {
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y, S(6), r.Height), c);
        Fill(b, new Rectangle(r.X, r.Y + r.Height / 2 - S(3), r.Width, S(6)), c);
        Fill(b, Inset(r, S(5)), c);
    }

    protected void Stud(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(126, 76, 19));
        Fill(b, Inset(r, Math.Max(1, S(2))), Gold);
    }

    protected static string LineName(string type) => type switch
    {
        "Beverage" => "음료",
        "Packaging" => "포장",
        "Fermentation" => "발효",
        _ => type
    };

    protected string CompanyName() => string.IsNullOrWhiteSpace(Mod.State.CompanyName) ? "새별 농업" : Mod.State.CompanyName;
}
