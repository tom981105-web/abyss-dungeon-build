using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

/// <summary>
/// v0.3.7 visual direction:
/// readable medium/large type plus original illustrated TCG-style cards.
/// </summary>
internal static class TcgVisuals037
{
    private static Texture2D? ArtAtlas;
    private static Texture2D? BoosterPack;

    private static readonly string[] ArtKeys =
    {
        "parsnip","potato","green_slime","joja_cola","sardine",
        "dandelion","stone","copper_ore","field_snack","chicken",
        "strawberry","blue_jazz","cave_fly","dust_sprite","purple_shorts",
        "junimo","minecart","ancient_seed","skull_cavern","shadow_brute",
        "krobus","galaxy_sword","dinosaur","witch","mermaid",
        "golden_pumpkin","mr_qi","stardrop","prismatic_shard","grandpas_shrine"
    };

    private static readonly Dictionary<string, int> ArtIndex =
        ArtKeys.Select((key, i) => (key, i))
            .ToDictionary(p => p.key, p => p.i, StringComparer.OrdinalIgnoreCase);

    private static readonly Color Paper = new(248, 232, 194);
    private static readonly Color Paper2 = new(237, 214, 167);
    private static readonly Color Deep = new(61, 41, 27);

    internal static void Initialize(IModHelper helper, IMonitor monitor)
    {
        try
        {
            ArtAtlas = helper.ModContent.Load<Texture2D>("assets/tcg_art_atlas.png");
            BoosterPack = helper.ModContent.Load<Texture2D>("assets/tcg_booster_pack.png");
            monitor.Log("Junimo Cards 0.3.7 TCG illustration assets loaded (30-card atlas + booster pack).", LogLevel.Info);
        }
        catch (Exception ex)
        {
            monitor.Log($"TCG art assets could not be loaded; generated fallback panels will be used. {ex.Message}", LogLevel.Warn);
            ArtAtlas = null;
            BoosterPack = null;
        }
    }

    internal static void DrawBoosterPack(SpriteBatch b, Rectangle dest)
    {
        if (BoosterPack is not null)
        {
            b.Draw(BoosterPack, dest, Color.White);
            CardUi.Border(b, dest, new Color(226, 178, 68), 3);
            return;
        }

        CardUi.DrawCardBack(b, dest);
    }

    internal static void DrawCardBack(SpriteBatch b, Rectangle r, bool pulse = false)
    {
        Color outer = new(224, 171, 62);
        Color inner = new(245, 222, 147);
        Rectangle shadow = new(r.X + 4, r.Y + 5, r.Width, r.Height);
        b.Draw(Game1.fadeToBlackRect, shadow, Color.Black * 0.18f);
        b.Draw(Game1.fadeToBlackRect, r, new Color(22, 58, 42));
        CardUi.Border(b, r, outer, pulse ? 6 : 4);
        Rectangle innerRect = new(r.X + 9, r.Y + 9, r.Width - 18, r.Height - 18);
        CardUi.Border(b, innerRect, inner, 2);

        int artH = Math.Max(70, (int)(r.Height * 0.52f));
        Rectangle emblem = new(r.X + 16, r.Y + 28, r.Width - 32, artH);
        b.Draw(Game1.fadeToBlackRect, emblem, new Color(38, 93, 58));
        CardUi.Border(b, emblem, new Color(84, 143, 89), 2);
        CardUi.CenterText(b, Game1.dialogueFont, "✦", emblem, new Color(244, 209, 107), 1.18f);
        CardUi.CenterText(b, Game1.smallFont, "JUNIMO CARDS",
            new Rectangle(r.X + 9, r.Bottom - 42, r.Width - 18, 28), Color.White, 0.95f);
    }

    internal static void DrawCard(
        SpriteBatch b,
        Rectangle r,
        CardDefinition card,
        string variant,
        string condition,
        int value,
        int count = 0,
        bool selected = false,
        bool dramatic = false)
    {
        Color rarity = CardUi.RarityColor(card.Rarity);
        int rank = ModEntry.GetRarityRank(card.Rarity);

        int shadowPad = dramatic ? 7 : 4;
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(r.X + shadowPad, r.Y + shadowPad, r.Width, r.Height),
            Color.Black * (dramatic ? 0.26f : 0.17f));

        Color body = rank >= 4 ? new Color(255, 239, 191) : Paper;
        b.Draw(Game1.fadeToBlackRect, r, body);

        int border = dramatic ? 6 : selected ? 5 : 3;
        Color frame = variant switch
        {
            "Gold" => new Color(230, 170, 45),
            "Rainbow" => new Color(233, 108, 183),
            _ => rarity
        };
        CardUi.Border(b, r, frame, border);

        if (selected)
        {
            Rectangle sel = new(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6);
            CardUi.Border(b, sel, new Color(255, 214, 87), 2);
        }

        int pad = Math.Max(6, r.Width / 24);
        int headerH = Math.Clamp(r.Height / 9, 20, 34);
        Rectangle header = new(r.X + pad, r.Y + pad, r.Width - pad * 2, headerH);
        b.Draw(Game1.fadeToBlackRect, header, rarity);
        CardUi.CenterText(b, Game1.smallFont,
            $"{ModEntry.RarityName(card.Rarity)}   {card.SetNo}",
            header, Color.White, Math.Min(1.28f, 0.92f + r.Width / 650f));

        int artTop = header.Bottom + Math.Max(5, r.Height / 55);
        int artH = Math.Clamp((int)(r.Height * 0.42f), 55, 118);
        Rectangle art = new(r.X + pad + 3, artTop, r.Width - pad * 2 - 6, artH);
        DrawArt(b, card.Key, art, rarity);
        CardUi.Border(b, art, new Color(255, 245, 215), 2);

        DrawVariantOverlay(b, art, variant, frame);

        int nameY = art.Bottom + Math.Max(3, r.Height / 70);
        int nameH = Math.Clamp(r.Height / 7, 24, 42);
        CardUi.CenterText(b, Game1.dialogueFont, card.Name,
            new Rectangle(r.X + pad, nameY, r.Width - pad * 2, nameH),
            Deep, Math.Min(0.96f, 0.72f + r.Width / 950f));

        int metaY = nameY + nameH;
        int metaH = Math.Clamp(r.Height / 10, 18, 30);
        CardUi.CenterText(b, Game1.smallFont,
            $"{card.Category} · {ModEntry.VariantName(variant)}",
            new Rectangle(r.X + pad, metaY, r.Width - pad * 2, metaH),
            rarity, Math.Min(1.16f, 0.90f + r.Width / 800f));

        int conditionY = metaY + metaH;
        int bottomReserve = Math.Clamp(r.Height / 7, 26, 40);
        if (conditionY + 18 < r.Bottom - bottomReserve)
        {
            CardUi.CenterText(b, Game1.smallFont, condition,
                new Rectangle(r.X + pad, conditionY, r.Width - pad * 2, 22),
                CardUi.Muted, Math.Min(1.12f, 0.88f + r.Width / 850f));
        }

        string bottom = count > 0 ? $"{value:N0}G  ·  보유 {count}장" : $"{value:N0}G";
        Rectangle priceRect = new(r.X + pad, r.Bottom - bottomReserve, r.Width - pad * 2, bottomReserve - 4);
        b.Draw(Game1.fadeToBlackRect, priceRect, Paper2 * 0.70f);
        CardUi.CenterText(b, Game1.smallFont, bottom, priceRect,
            rank >= 4 ? new Color(132, 79, 18) : CardUi.GreenDark,
            Math.Min(1.18f, 0.92f + r.Width / 850f));

        if (rank >= 4)
            DrawCornerSparkles(b, r, variant, dramatic);
    }

    private static void DrawArt(SpriteBatch b, string key, Rectangle dest, Color fallback)
    {
        if (ArtAtlas is not null && ArtIndex.TryGetValue(key, out int index))
        {
            int col = index % 5;
            int row = index / 5;
            Rectangle src = new(col * 256, row * 160, 256, 160);
            b.Draw(ArtAtlas, dest, src, Color.White);
            return;
        }

        b.Draw(Game1.fadeToBlackRect, dest, fallback * 0.36f);
        Rectangle glow = new(dest.X + dest.Width / 6, dest.Y + dest.Height / 5,
            dest.Width * 2 / 3, dest.Height * 3 / 5);
        b.Draw(Game1.fadeToBlackRect, glow, Color.White * 0.13f);
        CardUi.CenterText(b, Game1.dialogueFont, "✦", dest, fallback, 1.25f);
    }

    private static void DrawVariantOverlay(SpriteBatch b, Rectangle art, string variant, Color frame)
    {
        if (variant == "Normal")
            return;

        float time = (float)Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (variant == "Holo")
        {
            int x = art.X + (int)((time * 90f) % Math.Max(1, art.Width + 70)) - 35;
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x, art.Y, 24, art.Height), Color.White * 0.16f);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x + 18, art.Y, 8, art.Height), new Color(150, 220, 255) * 0.16f);
        }
        else if (variant == "Gold")
        {
            CardUi.Border(b, new Rectangle(art.X - 3, art.Y - 3, art.Width + 6, art.Height + 6),
                new Color(242, 196, 62) * 0.90f, 3);
            b.Draw(Game1.fadeToBlackRect, art, new Color(255, 215, 95) * 0.08f);
        }
        else if (variant == "Rainbow")
        {
            Color[] colors =
            {
                new(255, 86, 92), new(255, 174, 61), new(244, 227, 75),
                new(72, 201, 112), new(76, 163, 246), new(171, 93, 225)
            };
            int t = Math.Max(2, Math.Min(5, art.Width / 40));
            int seg = Math.Max(1, art.Width / colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                b.Draw(Game1.fadeToBlackRect,
                    new Rectangle(art.X + i * seg, art.Y, i == colors.Length - 1 ? art.Right - (art.X + i * seg) : seg, t),
                    colors[i]);
            }
            b.Draw(Game1.fadeToBlackRect, art, frame * 0.07f);
        }
    }

    private static void DrawCornerSparkles(SpriteBatch b, Rectangle r, string variant, bool dramatic)
    {
        float time = (float)Game1.currentGameTime.TotalGameTime.TotalSeconds;
        int count = dramatic ? 8 : 4;
        for (int i = 0; i < count; i++)
        {
            float phase = time * 1.4f + i * 1.71f;
            int x = i % 2 == 0 ? r.X + 10 + (i * 17) % Math.Max(12, r.Width / 3) : r.Right - 12 - (i * 13) % Math.Max(12, r.Width / 3);
            int y = r.Y + 18 + (i * 29) % Math.Max(30, r.Height - 45);
            int s = 2 + (int)((MathF.Sin(phase) + 1f) * 1.5f);
            Color c = variant == "Rainbow"
                ? BalancedUi036.RainbowColor(i, time)
                : new Color(255, 236, 162);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x - s * 2, y - 1, s * 4, 2), c * 0.65f);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x - 1, y - s * 2, 2, s * 4), c * 0.65f);
        }
    }
}

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
}

internal sealed class TcgCardShopMenu037 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly Rectangle Pack;
    private readonly Rectangle Collection;
    private readonly Rectangle Shelf;
    private readonly Rectangle Close;

    internal TcgCardShopMenu037(ModEntry mod)
    {
        Mod = mod;
        Rectangle r = CardUi.Center(900, 520);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int margin = 34;
        int gap = 14;
        int tileW = (width - margin * 2 - gap * 2) / 3;
        int tileY = yPositionOnScreen + 216;
        Pack = new Rectangle(xPositionOnScreen + margin, tileY, tileW, 134);
        Collection = new Rectangle(Pack.Right + gap, tileY, tileW, 134);
        Shelf = new Rectangle(Collection.Right + gap, tileY, tileW, 134);
        Close = new Rectangle(xPositionOnScreen + width - 132, yPositionOnScreen + height - 48, 96, 36);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pack.Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new TcgPackMenu037(Mod, this);
            return;
        }
        if (Collection.Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new TcgCollectionMenu037(Mod, this);
            return;
        }
        if (Shelf.Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new TcgShelfMenu037(Mod, this);
            return;
        }
        if (Close.Contains(x, y))
            exitThisMenu();
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        TcgUi037.Begin(b, this, "주니모 카드샵", "TEST · 모든 등급 16.67%");

        int unique = Mod.Core.UniqueCardCount();
        Rectangle stats = new(xPositionOnScreen + 34, yPositionOnScreen + 108, width - 68, 90);
        CardUi.Panel(b, stats);
        int cell = stats.Width / 4;
        DrawStat(b, new Rectangle(stats.X, stats.Y, cell, stats.Height), "골드", $"{Game1.player.Money:N0}G");
        DrawStat(b, new Rectangle(stats.X + cell, stats.Y, cell, stats.Height), "미개봉", $"{Mod.State.UnopenedPacks}팩");
        DrawStat(b, new Rectangle(stats.X + cell * 2, stats.Y, cell, stats.Height), "컬렉션", $"{unique}/{Mod.Cards.Count}");
        DrawStat(b, new Rectangle(stats.X + cell * 3, stats.Y, stats.Width - cell * 3, stats.Height), "매출", $"{Mod.State.LifetimeCardRevenue:N0}G");

        DrawTile(b, Pack, "팩 구매", $"1팩 {Mod.Config.PackPrice:N0}G");
        DrawTile(b, Collection, "컬렉션", $"수집 {unique}/{Mod.Cards.Count}");
        DrawTile(b, Shelf, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots}");

        Rectangle today = new(xPositionOnScreen + 34, yPositionOnScreen + 366, width - 68, 64);
        CardUi.Panel(b, today);
        CardUi.CenterText(b, Game1.smallFont, "오늘",
            new Rectangle(today.X + 10, today.Y + 10, 82, 44), CardUi.Ink, 1.30f);
        string summary = $"손님 {Mod.State.LastCustomerCount}명   판매 {Mod.State.LastCardsSold}장   +{Mod.State.LastDailyRevenue:N0}G";
        CardUi.CenterText(b, Game1.dialogueFont, summary,
            new Rectangle(today.X + 94, today.Y + 8, today.Width - 106, 48), CardUi.GreenDark, 0.88f);

        TcgUi037.MiniButton(b, Close, "닫기");
        drawMouse(b);
    }

    private static void DrawStat(SpriteBatch b, Rectangle r, string label, string value)
    {
        CardUi.CenterText(b, Game1.smallFont, label,
            new Rectangle(r.X + 5, r.Y + 7, r.Width - 10, 29), CardUi.Muted, 1.38f);
        CardUi.CenterText(b, Game1.dialogueFont, value,
            new Rectangle(r.X + 5, r.Y + 36, r.Width - 10, 47), CardUi.Ink, 0.88f);
    }

    private static void DrawTile(SpriteBatch b, Rectangle r, string title, string sub)
    {
        CardUi.Panel(b, r);
        CardUi.CenterText(b, Game1.dialogueFont, title,
            new Rectangle(r.X + 12, r.Y + 18, r.Width - 24, 52), CardUi.GreenDark, 0.94f);
        CardUi.CenterText(b, Game1.smallFont, sub,
            new Rectangle(r.X + 12, r.Y + 82, r.Width - 24, 32), CardUi.Ink, 1.28f);
    }
}

internal sealed class TcgPackMenu037 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle BuyOne;
    private readonly Rectangle BuyFive;
    private readonly Rectangle Open;
    private readonly Rectangle Back;
    private string Message = "TEST · 모든 등급 16.67%";

    internal TcgPackMenu037(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(900, 530);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int buttonY = r.Y + 365;
        int gap = 12;
        int buttonW = (r.Width - 76 - gap * 2) / 3;
        BuyOne = new Rectangle(r.X + 38, buttonY, buttonW, 54);
        BuyFive = new Rectangle(BuyOne.Right + gap, buttonY, buttonW, 54);
        Open = new Rectangle(BuyFive.Right + gap, buttonY, buttonW, 54);
        Back = new Rectangle(r.Right - 132, r.Bottom - 48, 98, 36);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (BuyOne.Contains(x, y))
        {
            Mod.TryBuyPacks(1, out Message);
            return;
        }
        if (BuyFive.Contains(x, y))
        {
            Mod.TryBuyPacks(5, out Message);
            return;
        }
        if (Open.Contains(x, y) && Mod.State.UnopenedPacks > 0)
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new TcgPackOpeningMenu037(Mod, this);
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        TcgUi037.Begin(b, this, "팩 구매", "Junimo Cards · 부스터 팩");

        Rectangle pack = new(xPositionOnScreen + 54, yPositionOnScreen + 114, 178, 226);
        TcgVisuals037.DrawBoosterPack(b, pack);

        Rectangle info = new(pack.Right + 22, yPositionOnScreen + 114,
            width - 54 - (pack.Right + 22 - xPositionOnScreen), 226);
        CardUi.Panel(b, info);

        CardUi.CenterText(b, Game1.dialogueFont, $"보유 {Mod.State.UnopenedPacks}팩",
            new Rectangle(info.X + 16, info.Y + 14, info.Width - 32, 50), CardUi.Ink, 0.96f);
        CardUi.CenterText(b, Game1.smallFont, "커먼 · 언커먼 · 레어",
            new Rectangle(info.X + 16, info.Y + 76, info.Width - 32, 32), CardUi.GreenDark, 1.30f);
        CardUi.CenterText(b, Game1.smallFont, "에픽 · 레전더리 · 시크릿",
            new Rectangle(info.X + 16, info.Y + 112, info.Width - 32, 32), CardUi.GreenDark, 1.30f);
        CardUi.CenterText(b, Game1.dialogueFont, "각 16.67%",
            new Rectangle(info.X + 16, info.Y + 158, info.Width - 32, 44), TcgUi037.TestGold, 0.90f);

        TcgUi037.Button(b, BuyOne, $"1팩 {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        TcgUi037.Button(b, BuyFive, $"5팩 {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        TcgUi037.Button(b, Open, $"개봉 {Mod.State.UnopenedPacks}팩", Mod.State.UnopenedPacks > 0, true);
        CardUi.CenterText(b, Game1.smallFont, Message,
            new Rectangle(xPositionOnScreen + 40, BuyOne.Bottom + 5, width - 185, 32), CardUi.Muted, 1.28f);
        TcgUi037.MiniButton(b, Back, "뒤로");
        drawMouse(b);
    }
}

internal sealed class TcgPackOpeningMenu037 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly List<CardPull> Pulls;
    private readonly bool[] Revealed;
    private readonly float[] RevealFx;
    private readonly List<Rectangle> Cards = new();
    private readonly Rectangle PackChoice;
    private readonly Rectangle CollectionChoice;
    private readonly string OpeningMessage;

    private int ActiveReveal = -1;
    private int ActiveRank;
    private float InputLock;
    private float CompletionFx = -1f;
    private bool CompletionCelebrated;

    internal TcgPackOpeningMenu037(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(950, 590);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        if (!Mod.TryOpenPack(out Pulls, out OpeningMessage))
            Pulls = new List<CardPull>();

        Revealed = new bool[Pulls.Count];
        RevealFx = Enumerable.Repeat(-1f, Pulls.Count).ToArray();

        int gap = 12;
        int margin = 30;
        int cardW = (r.Width - margin * 2 - gap * 4) / 5;
        int cardH = 270;
        int top = r.Y + 114;
        for (int i = 0; i < 5; i++)
            Cards.Add(new Rectangle(r.X + margin + i * (cardW + gap), top, cardW, cardH));

        int choiceW = 220;
        int choiceY = r.Bottom - 54;
        PackChoice = new Rectangle(r.Center.X - choiceW - 8, choiceY, choiceW, 40);
        CollectionChoice = new Rectangle(r.Center.X + 8, choiceY, choiceW, 40);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pulls.Count == 0)
        {
            Game1.activeClickableMenu = ReturnMenu;
            return;
        }

        bool complete = Revealed.Length > 0 && Revealed.All(p => p);
        if (complete && CompletionFx >= 0.35f)
        {
            if (PackChoice.Contains(x, y))
            {
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = ReturnMenu;
                return;
            }
            if (CollectionChoice.Contains(x, y))
            {
                Game1.playSound("bigSelect");
                Game1.activeClickableMenu = new TcgCollectionMenu037(Mod, ReturnMenu);
                return;
            }
        }

        if (InputLock > 0f)
            return;

        for (int i = 0; i < Pulls.Count && i < Cards.Count; i++)
        {
            if (Revealed[i] || !Cards[i].Contains(x, y))
                continue;

            Revealed[i] = true;
            RevealFx[i] = 0f;
            ActiveReveal = i;
            CardDefinition? def = Mod.FindCard(Pulls[i].CardKey);
            ActiveRank = def is null ? 0 : ModEntry.GetRarityRank(def.Rarity);
            InputLock = ActiveRank >= 4 ? 0.70f : ActiveRank >= 2 ? 0.50f : 0.28f;
            Game1.playSound(ActiveRank >= 4 ? "reward" : ActiveRank >= 2 ? "newArtifact" : "coin");
            return;
        }
    }

    public override void update(GameTime time)
    {
        base.update(time);
        float dt = Math.Min(0.05f, (float)time.ElapsedGameTime.TotalSeconds);
        InputLock = Math.Max(0f, InputLock - dt);
        for (int i = 0; i < RevealFx.Length; i++)
        {
            if (RevealFx[i] >= 0f && RevealFx[i] < 2f)
                RevealFx[i] += dt;
        }

        if (ActiveReveal >= 0 && ActiveReveal < RevealFx.Length && RevealFx[ActiveReveal] > 1.50f)
            ActiveReveal = -1;

        if (!CompletionCelebrated && Revealed.Length > 0 && Revealed.All(p => p) && RevealFx.All(p => p >= 0.66f))
        {
            CompletionCelebrated = true;
            CompletionFx = 0f;
            Game1.playSound("reward");
        }
        if (CompletionFx >= 0f && CompletionFx < 2.5f)
            CompletionFx += dt;
    }

    public override void draw(SpriteBatch b)
    {
        TcgUi037.Begin(b, this, "팩 개봉",
            OpeningMessage.StartsWith("TEST", StringComparison.OrdinalIgnoreCase)
                ? "카드를 눌러 공개하세요 · TEST 16.67%"
                : "카드를 눌러 공개하세요");

        DrawDrama(b);

        for (int i = 0; i < Cards.Count; i++)
        {
            Rectangle baseRect = Cards[i];
            if (i >= Pulls.Count)
            {
                TcgVisuals037.DrawCardBack(b, baseRect);
                continue;
            }

            if (!Revealed[i])
            {
                float pulse = 1f + MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalSeconds * 4f + i) * 0.012f;
                TcgVisuals037.DrawCardBack(b, TcgUi037.ScaleAroundCenter(baseRect, pulse, pulse), true);
                CardUi.CenterText(b, Game1.smallFont, "클릭",
                    new Rectangle(baseRect.X, baseRect.Bottom + 4, baseRect.Width, 27),
                    CardUi.GreenDark, 1.30f);
                continue;
            }

            CardDefinition? def = Mod.FindCard(Pulls[i].CardKey);
            if (def is null)
                continue;
            DrawAnimatedCard(b, baseRect, def, Pulls[i], ModEntry.GetRarityRank(def.Rarity),
                Math.Max(0f, RevealFx[i]), i == ActiveReveal);
        }

        if (CompletionFx >= 0f)
            DrawCompletion(b);

        bool complete = Revealed.Length > 0 && Revealed.All(p => p);
        if (complete && CompletionFx >= 0.35f)
        {
            TcgUi037.Button(b, PackChoice, "팩 구매로");
            TcgUi037.Button(b, CollectionChoice, "컬렉션으로", true, true);
        }
        else
        {
            int opened = Revealed.Count(p => p);
            CardUi.CenterText(b, Game1.dialogueFont, $"{opened}/5",
                new Rectangle(xPositionOnScreen + width / 2 - 58, yPositionOnScreen + 430, 116, 40),
                CardUi.Ink, 0.90f);
        }

        drawMouse(b);
    }

    private void DrawDrama(SpriteBatch b)
    {
        if (ActiveReveal < 0 || ActiveReveal >= Pulls.Count || ActiveReveal >= RevealFx.Length)
            return;

        float t = RevealFx[ActiveReveal];
        if (t < 0f || t > 1.50f)
            return;

        CardDefinition? def = Mod.FindCard(Pulls[ActiveReveal].CardKey);
        if (def is null)
            return;
        int rank = ModEntry.GetRarityRank(def.Rarity);
        if (rank < 2)
            return;

        Color accent = CardUi.RarityColor(def.Rarity);
        float fade = t < 0.16f ? t / 0.16f : Math.Clamp(1f - (t - 0.72f) / 0.78f, 0f, 1f);
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Black * ((rank >= 4 ? 0.38f : 0.22f) * fade));

        Rectangle target = Cards[ActiveReveal];
        Vector2 center = new(target.Center.X, target.Center.Y);

        if (rank >= 4 && t > 0.16f && t < 1.18f)
        {
            float life = Math.Clamp(1f - Math.Abs(t - 0.60f) / 0.58f, 0f, 1f);
            int rays = rank >= 5 ? 20 : 14;
            for (int i = 0; i < rays; i++)
            {
                float a = i * MathF.PI * 2f / rays + t * 0.8f;
                Color ray = rank >= 5
                    ? BalancedUi036.RainbowColor(i, t)
                    : accent;
                b.Draw(Game1.fadeToBlackRect, center, null,
                    ray * (0.18f * life), a, new Vector2(0f, 0.5f),
                    new Vector2(target.Width * 1.45f, 4.5f),
                    SpriteEffects.None, 0f);
            }
        }

        if (t > 0.24f && t < 0.50f)
        {
            float flash = 1f - Math.Abs(t - 0.37f) / 0.13f;
            Color flashColor = rank >= 4 ? new Color(255, 240, 180) : accent;
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                flashColor * (Math.Clamp(flash, 0f, 1f) * (rank >= 4 ? 0.21f : 0.10f)));
        }

        if (t > 0.34f && t < 1.22f)
        {
            string banner = rank >= 5
                ? "✦ 시크릿!!! ✦"
                : rank >= 4
                    ? "★ 레전더리!! ★"
                    : rank == 3 ? "에픽!" : "레어!";
            Color bannerColor = rank >= 5
                ? BalancedUi036.RainbowColor((int)(t * 10f), t)
                : accent;
            CardUi.CenterText(b, Game1.dialogueFont, banner,
                new Rectangle(xPositionOnScreen + width / 2 - 220, yPositionOnScreen + 88, 440, 44),
                bannerColor, rank >= 4 ? 1.05f : 0.92f);
        }
    }

    private void DrawAnimatedCard(SpriteBatch b, Rectangle baseRect, CardDefinition def, CardPull pull, int rank, float t, bool active)
    {
        Color accent = rank >= 1 ? CardUi.RarityColor(def.Rarity) : CardUi.Gold;

        if (t < 0.12f)
        {
            float press = 1f - MathF.Sin(t / 0.12f * MathF.PI) * 0.03f;
            TcgVisuals037.DrawCardBack(b, TcgUi037.ScaleAroundCenter(baseRect, press, press));
            return;
        }
        if (t < 0.28f)
        {
            float p = TcgUi037.SmoothStep((t - 0.12f) / 0.16f);
            TcgVisuals037.DrawCardBack(b,
                TcgUi037.ScaleAroundCenter(baseRect, 1f - p * 0.95f, 1f + MathF.Sin(p * MathF.PI) * 0.05f));
            return;
        }

        float revealP = Math.Clamp((t - 0.28f) / 0.32f, 0f, 1f);
        float sx = 0.05f + 0.95f * TcgUi037.EaseOutBack(revealP);
        float boost = rank >= 5 ? 0.19f : rank >= 4 ? 0.15f : rank >= 2 ? 0.10f : 0.055f;
        float bounceP = Math.Clamp((t - 0.28f) / 0.60f, 0f, 1f);
        float zoom = 1f + MathF.Sin(bounceP * MathF.PI) * boost;
        float lift = MathF.Sin(bounceP * MathF.PI) * (rank >= 4 ? 20f : rank >= 2 ? 13f : 8f);
        float shake = active && rank >= 2 && t < 0.74f
            ? MathF.Sin(t * 64f) * (rank >= 4 ? 3.8f : 2f)
            : 0f;

        Rectangle drawRect = TcgUi037.ScaleAroundCenter(baseRect, Math.Max(0.05f, sx) * zoom, zoom);
        drawRect.Offset((int)Math.Round(shake), -(int)Math.Round(lift));

        if (t < 1.38f)
            DrawBurst(b, baseRect, accent, rank, t);

        TcgVisuals037.DrawCard(b, drawRect, def, pull.Variant, pull.Condition, pull.MarketValue, 0, false, rank >= 2);

        if (t < 1.18f)
        {
            float pulse = Math.Clamp(1f - (t - 0.28f) / 0.90f, 0f, 1f);
            int pad = 5 + (int)(pulse * (rank >= 5 ? 23 : rank >= 4 ? 18 : rank >= 2 ? 12 : 6));
            Rectangle aura = new(drawRect.X - pad, drawRect.Y - pad,
                drawRect.Width + pad * 2, drawRect.Height + pad * 2);
            CardUi.Border(b, aura, accent * (0.30f + pulse * 0.62f),
                rank >= 4 ? 5 : rank >= 2 ? 3 : 2);
        }
    }

    private static void DrawBurst(SpriteBatch b, Rectangle r, Color color, int rank, float t)
    {
        float local = Math.Max(0f, t - 0.20f);
        float life = Math.Clamp(1f - local / 1.16f, 0f, 1f);
        if (life <= 0f)
            return;

        int count = rank >= 5 ? 26 : rank >= 4 ? 20 : rank >= 2 ? 14 : 8;
        float radius = 15f + local * (rank >= 4 ? 115f : rank >= 2 ? 86f : 58f);
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.PI * 2f / count + local * 2f;
            int px = r.Center.X + (int)(MathF.Cos(a) * radius);
            int py = r.Center.Y + (int)(MathF.Sin(a) * radius * 0.65f);
            int size = rank >= 4 ? 7 : rank >= 2 ? 5 : 4;
            Color particle = rank >= 5 ? BalancedUi036.RainbowColor(i, t) : color;
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(px - size / 2, py - size / 2, size, size),
                particle * (0.22f + life * 0.72f));
        }
    }

    private void DrawCompletion(SpriteBatch b)
    {
        if (CompletionFx < 0f || CompletionFx > 2.0f)
            return;
        float life = Math.Clamp(1f - CompletionFx / 2f, 0f, 1f);

        for (int i = 0; i < 34; i++)
        {
            float seed = i * 0.618f;
            int x = xPositionOnScreen + 30 + (int)((seed % 1f) * (width - 60));
            int y = yPositionOnScreen + 100 + (int)((CompletionFx * 120f + i * 23) % Math.Max(120, height - 180));
            Color c = BalancedUi036.RainbowColor(i, CompletionFx);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, 5, 9), c * (0.38f + life * 0.55f));
        }

        if (CompletionFx < 1.2f)
            CardUi.CenterText(b, Game1.dialogueFont, "팩 오픈 완료!",
                new Rectangle(xPositionOnScreen + width / 2 - 180, yPositionOnScreen + 415, 360, 45),
                TcgUi037.TestGold, 0.96f);
    }
}

internal sealed class TcgCollectionMenu037 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly int TargetSlot;
    private readonly Dictionary<string, Rectangle> Filters = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Key, Rectangle Bounds)> Hits = new();
    private readonly Rectangle Prev;
    private readonly Rectangle Next;
    private readonly Rectangle Bonus;
    private readonly Rectangle List;
    private readonly Rectangle Shelf;
    private readonly Rectangle Back;

    private string Filter = "All";
    private string SelectedKey = "";
    private string Message = "카드를 선택하세요";
    private int Page;

    internal TcgCollectionMenu037(ModEntry mod, IClickableMenu returnMenu, int targetSlot = -1)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        TargetSlot = targetSlot;
        Rectangle r = CardUi.Center(960, 620);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        string[] names = { "All", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Secret" };
        int gap = 5;
        int fw = (r.Width - 36 - gap * 6) / 7;
        int filterY = r.Y + 102;
        for (int i = 0; i < names.Length; i++)
            Filters[names[i]] = new Rectangle(r.X + 18 + i * (fw + gap), filterY, fw, 38);

        int controlsY = r.Bottom - 47;
        Prev = new Rectangle(r.X + 18, controlsY, 82, 35);
        Next = new Rectangle(r.X + 106, controlsY, 82, 35);
        Bonus = new Rectangle(r.X + 196, controlsY, 150, 35);
        Shelf = new Rectangle(r.Right - 190, controlsY, 82, 35);
        Back = new Rectangle(r.Right - 102, controlsY, 84, 35);

        int detailW = 230;
        int detailX = r.Right - detailW - 18;
        List = new Rectangle(detailX + 10, r.Y + 494, detailW - 20, 42);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (var pair in Filters)
        {
            if (!pair.Value.Contains(x, y))
                continue;
            Filter = pair.Key;
            Page = 0;
            SelectedKey = "";
            Game1.playSound("smallSelect");
            return;
        }

        foreach (var hit in Hits)
        {
            if (!hit.Bounds.Contains(x, y))
                continue;
            SelectedKey = hit.Key;
            Game1.playSound("smallSelect");
            return;
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 8);

        if (Prev.Contains(x, y) && Page > 0)
        {
            Page--;
            SelectedKey = "";
            return;
        }
        if (Next.Contains(x, y) && Page < maxPage)
        {
            Page++;
            SelectedKey = "";
            return;
        }
        if (Bonus.Contains(x, y))
        {
            Mod.Core.TryClaimCollectionBonus(out Message);
            return;
        }
        if (List.Contains(x, y) && !string.IsNullOrWhiteSpace(SelectedKey))
        {
            bool ok = CardShopRules.TryListForSale(Mod, SelectedKey, TargetSlot, out Message);
            if (ok && TargetSlot >= 0)
                Game1.activeClickableMenu = ReturnMenu;
            return;
        }
        if (Shelf.Contains(x, y))
        {
            Game1.activeClickableMenu = new TcgShelfMenu037(Mod, this);
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        TcgUi037.Begin(b, this, "컬렉션", $"수집 {Mod.Core.UniqueCardCount()}/{Mod.Cards.Count}");

        foreach (var pair in Filters)
        {
            int count = pair.Key == "All"
                ? Mod.Core.UniqueCardCount()
                : Mod.Core.UniqueCountForRarity(pair.Key);
            TcgUi037.MiniButton(b, pair.Value, $"{FilterName(pair.Key)} {count}", true,
                string.Equals(Filter, pair.Key, StringComparison.OrdinalIgnoreCase));
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 8);
        Page = Math.Clamp(Page, 0, maxPage);
        int start = Page * 8;
        Hits.Clear();

        int detailW = 230;
        int detailX = xPositionOnScreen + width - detailW - 18;
        int gridLeft = xPositionOnScreen + 18;
        int gridRight = detailX - 10;
        int gridTop = yPositionOnScreen + 150;
        int gapX = 7;
        int gapY = 9;
        int cardW = (gridRight - gridLeft - gapX * 3) / 4;
        int cardH = 188;

        for (int i = 0; i < 8 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            int col = i % 4;
            int rr = i / 4;
            Rectangle card = new(gridLeft + col * (cardW + gapX),
                gridTop + rr * (cardH + gapY), cardW, cardH);

            TcgVisuals037.DrawCard(b, card, row.Card, row.Variant, row.Condition,
                row.Value, row.Count, row.CollectionKey == SelectedKey, false);
            Hits.Add((row.CollectionKey, card));
        }

        Rectangle detail = new(detailX, gridTop, detailW, 382);
        CardUi.Panel(b, detail, true);
        var selected = rows.FirstOrDefault(p => p.CollectionKey == SelectedKey);

        if (selected.Card is not null)
        {
            Rectangle preview = new(detail.X + 25, detail.Y + 12, detail.Width - 50, 265);
            TcgVisuals037.DrawCard(b, preview, selected.Card, selected.Variant, selected.Condition,
                selected.Value, selected.Count, true, false);

            int available = CardShopRules.GetListableCount(Mod, SelectedKey);
            CardUi.CenterText(b, Game1.smallFont,
                $"판매 가능 {available}장",
                new Rectangle(detail.X + 12, preview.Bottom + 6, detail.Width - 24, 30),
                CardUi.Muted, 1.24f);
            TcgUi037.Button(b, List,
                TargetSlot >= 0 ? $"{TargetSlot + 1}번에 진열" : "판매 진열",
                available > 0, true);
        }
        else
        {
            CardUi.CenterText(b, Game1.dialogueFont, "카드를 선택하세요",
                new Rectangle(detail.X + 12, detail.Y + 105, detail.Width - 24, 70),
                CardUi.Muted, 0.88f);
        }

        var bonus = Mod.Core.GetNextCollectionBonus();
        TcgUi037.MiniButton(b, Prev, "이전", Page > 0);
        TcgUi037.MiniButton(b, Next, "다음", Page < maxPage);
        TcgUi037.MiniButton(b, Bonus,
            bonus.Complete ? "보너스 완료" : $"{bonus.Required}종 → 팩 {bonus.Reward}",
            !bonus.Complete, bonus.CanClaim);
        TcgUi037.MiniButton(b, Shelf, "판매대");
        TcgUi037.MiniButton(b, Back, "뒤로");

        CardUi.CenterText(b, Game1.smallFont,
            $"{Page + 1}/{maxPage + 1} · {Message}",
            new Rectangle(Bonus.Right + 8, Prev.Y,
                Math.Max(90, Shelf.X - Bonus.Right - 16), 35),
            CardUi.Muted, 1.20f);

        drawMouse(b);
    }

    private static string FilterName(string filter) => filter switch
    {
        "All" => "전체",
        "Common" => "커먼",
        "Uncommon" => "언커먼",
        "Rare" => "레어",
        "Epic" => "에픽",
        "Legendary" => "전설",
        "Secret" => "시크릿",
        _ => filter
    };
}

internal sealed class TcgShelfMenu037 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly List<Rectangle> Slots = new();
    private readonly Rectangle Add;
    private readonly Rectangle Down;
    private readonly Rectangle Up;
    private readonly Rectangle Remove;
    private readonly Rectangle Back;

    private int SelectedSlot;
    private string Message = "슬롯을 선택하세요";

    internal TcgShelfMenu037(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(960, 610);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int top = r.Y + 106;
        int gapX = 8;
        int gapY = 9;
        int slotW = (r.Width - 36 - gapX * 3) / 4;
        int slotH = 174;
        int startX = r.X + 18;
        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Slots.Add(new Rectangle(startX + col * (slotW + gapX), top + row * (slotH + gapY), slotW, slotH));
        }

        int controlsY = r.Bottom - 48;
        int gap = 7;
        int controlW = (r.Width - 36 - gap * 4) / 5;
        Add = new Rectangle(r.X + 18, controlsY, controlW, 36);
        Down = new Rectangle(Add.Right + gap, controlsY, controlW, 36);
        Up = new Rectangle(Down.Right + gap, controlsY, controlW, 36);
        Remove = new Rectangle(Up.Right + gap, controlsY, controlW, 36);
        Back = new Rectangle(Remove.Right + gap, controlsY, controlW, 36);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (!Slots[i].Contains(x, y))
                continue;
            SelectedSlot = i;
            Game1.playSound("smallSelect");
            return;
        }

        SaleListing? listing = Mod.Core.GetListingAtSlot(SelectedSlot);
        if (Add.Contains(x, y) && listing is null)
        {
            Game1.activeClickableMenu = new TcgCollectionMenu037(Mod, this, SelectedSlot);
            return;
        }
        if (Down.Contains(x, y) && listing is not null)
        {
            Mod.Core.TryAdjustListingPrice(SelectedSlot, -1, out Message);
            return;
        }
        if (Up.Contains(x, y) && listing is not null)
        {
            Mod.Core.TryAdjustListingPrice(SelectedSlot, 1, out Message);
            return;
        }
        if (Remove.Contains(x, y) && listing is not null)
        {
            Mod.Core.RemoveListingBySlot(SelectedSlot, out Message);
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        TcgUi037.Begin(b, this, "판매 진열대",
            $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots} · 하루 최대 {Mod.Config.MaxDailySales}장");

        IReadOnlyList<SaleListing?> shelf = Mod.Core.GetShelfSlots();

        for (int i = 0; i < Slots.Count; i++)
        {
            Rectangle r = Slots[i];
            CardUi.Panel(b, r, i == SelectedSlot);
            SaleListing? listing = i < shelf.Count ? shelf[i] : null;

            if (listing is null)
            {
                CardUi.CenterText(b, Game1.dialogueFont, "+",
                    new Rectangle(r.X + 8, r.Y + 20, r.Width - 16, 54),
                    CardUi.Muted, 0.96f);
                CardUi.CenterText(b, Game1.smallFont, $"{i + 1}번",
                    new Rectangle(r.X + 8, r.Y + 90, r.Width - 16, 38),
                    CardUi.Muted, 1.28f);
                continue;
            }

            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out string condition))
                continue;
            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null)
                continue;

            Rectangle mini = new(r.X + 24, r.Y + 10, r.Width - 48, r.Height - 34);
            TcgVisuals037.DrawCard(b, mini, card, variant, condition, listing.Price, 0, i == SelectedSlot, false);
            CardUi.CenterText(b, Game1.smallFont, $"{listing.Price:N0}G",
                new Rectangle(r.X + 8, r.Bottom - 25, r.Width - 16, 22),
                CardUi.GreenDark, 1.18f);
        }

        SaleListing? selected = Mod.Core.GetListingAtSlot(SelectedSlot);
        string infoText = selected is null
            ? $"{SelectedSlot + 1}번 · 빈 슬롯"
            : $"{SelectedSlot + 1}번";

        if (selected is not null &&
            CardKeys.TryParse(selected.CollectionKey, out string key, out string selectedVariant, out string selectedCondition))
        {
            CardDefinition? card = Mod.FindCard(key);
            if (card is not null)
                infoText = $"{card.Name} · {ModEntry.VariantName(selectedVariant)} · {selectedCondition} · 판매확률 {Mod.Core.GetSaleChance(selected) * 100:0}%";
        }

        if (!string.Equals(Message, "슬롯을 선택하세요", StringComparison.Ordinal))
            infoText = Message;

        Rectangle info = new(xPositionOnScreen + 20, yPositionOnScreen + 480, width - 40, 35);
        CardUi.CenterText(b, Game1.smallFont, infoText, info, CardUi.Ink, 1.20f);

        TcgUi037.MiniButton(b, Add, selected is null ? "카드 넣기" : "사용 중", selected is null);
        TcgUi037.MiniButton(b, Down, "가격 -50", selected is not null);
        TcgUi037.MiniButton(b, Up, "가격 +50", selected is not null);
        TcgUi037.MiniButton(b, Remove, "회수", selected is not null);
        TcgUi037.MiniButton(b, Back, "뒤로");
        drawMouse(b);
    }
}
