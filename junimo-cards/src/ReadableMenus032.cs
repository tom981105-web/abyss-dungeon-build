using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

// v0.3.4 bigger-text + "wow" reveal pass.
// The goal is to spend screen space on the things the player actually reads,
// then make pack reveals feel like a reward instead of a static flip.
internal static class ReadableUi034
{
    internal static Rectangle FullCenter(int width, int height)
    {
        width = Math.Min(width, Math.Max(320, Game1.uiViewport.Width - 16));
        height = Math.Min(height, Math.Max(260, Game1.uiViewport.Height - 16));
        return new Rectangle(
            Math.Max(0, (Game1.uiViewport.Width - width) / 2),
            Math.Max(0, (Game1.uiViewport.Height - height) / 2),
            width,
            height);
    }

    internal static void Begin(SpriteBatch b, IClickableMenu menu, string title, string subtitle = "")
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.60f);
        IClickableMenu.drawTextureBox(b, menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height, Color.White);

        int headerH = Math.Min(88, Math.Max(72, menu.height / 5));
        Rectangle header = new(menu.xPositionOnScreen + 14, menu.yPositionOnScreen + 12, menu.width - 28, headerH);
        IClickableMenu.drawTextureBox(b, header.X, header.Y, header.Width, header.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(header.X + 7, header.Y + 7, header.Width - 14, header.Height - 14), CardUi.GreenDark);

        CardUi.CenterText(b, Game1.dialogueFont, title,
            new Rectangle(header.X + 12, header.Y + 5, header.Width - 24, headerH - 34),
            Color.White, 1.12f);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            CardUi.CenterText(b, Game1.smallFont, subtitle,
                new Rectangle(header.X + 12, header.Bottom - 31, header.Width - 24, 24),
                new Color(236, 242, 224), 1.38f);
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

        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14), fill * 0.90f);
        CardUi.CenterText(b, Game1.dialogueFont, text,
            new Rectangle(r.X + 8, r.Y + 6, r.Width - 16, r.Height - 12),
            enabled ? Color.White : new Color(224, 220, 207), 0.96f);
    }

    internal static void DrawSimpleCardFront(SpriteBatch b, Rectangle r, CardDefinition card, CardPull pull, bool strongGlow)
    {
        Color rarity = CardUi.RarityColor(card.Rarity);
        b.Draw(Game1.fadeToBlackRect, r, new Color(250, 235, 198));
        CardUi.Border(b, r, strongGlow ? rarity : new Color(95, 69, 43), strongGlow ? 6 : 3);

        int bandH = Math.Max(28, r.Height / 9);
        Rectangle band = new(r.X + 7, r.Y + 7, r.Width - 14, bandH);
        b.Draw(Game1.fadeToBlackRect, band, rarity);
        CardUi.CenterText(b, Game1.smallFont, ModEntry.RarityName(card.Rarity), band, Color.White, 1.28f);

        int emblemH = Math.Max(48, r.Height / 3);
        Rectangle emblem = new(r.X + 14, band.Bottom + 9, r.Width - 28, emblemH);
        b.Draw(Game1.fadeToBlackRect, emblem, rarity * 0.22f);
        string initial = string.IsNullOrWhiteSpace(card.Name) ? "?" : card.Name[..1];
        CardUi.CenterText(b, Game1.dialogueFont, initial, emblem, rarity, 1.40f);

        int nameY = emblem.Bottom + 4;
        CardUi.CenterText(b, Game1.dialogueFont, card.Name,
            new Rectangle(r.X + 8, nameY, r.Width - 16, 36), CardUi.Ink, 0.92f);

        int metaY = nameY + 37;
        CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(pull.Variant)} · {pull.Condition}",
            new Rectangle(r.X + 8, metaY, r.Width - 16, 28), CardUi.Ink, 1.12f);

        CardUi.CenterText(b, Game1.dialogueFont, $"{pull.MarketValue:N0}G",
            new Rectangle(r.X + 8, r.Bottom - 37, r.Width - 16, 30), CardUi.GreenDark, 0.88f);
    }
}

internal sealed class ReadableCardShopMenu032 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly Rectangle Pack;
    private readonly Rectangle Collection;
    private readonly Rectangle Shelf;
    private readonly Rectangle Close;

    internal ReadableCardShopMenu032(ModEntry mod)
    {
        Mod = mod;
        Rectangle r = ReadableUi034.FullCenter(1280, 760);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int contentTop = r.Y + Math.Min(104, Math.Max(92, r.Height / 4));
        int statsH = Math.Min(86, Math.Max(72, r.Height / 5));
        int statsBottom = contentTop + statsH;
        int gap = Math.Max(10, r.Width / 60);
        int tileTop = statsBottom + 10;
        int tileH = Math.Min(128, Math.Max(100, r.Height / 3));
        int tileW = (r.Width - 40 - gap * 2) / 3;

        Pack = new Rectangle(r.X + 20, tileTop, tileW, tileH);
        Collection = new Rectangle(Pack.Right + gap, tileTop, tileW, tileH);
        Shelf = new Rectangle(Collection.Right + gap, tileTop, tileW, tileH);
        Close = new Rectangle(r.Right - 130, r.Bottom - 50, 105, 38);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pack.Contains(x, y))
        {
            Game1.activeClickableMenu = new ReadablePackMenu032(Mod, this);
            Game1.playSound("bigSelect");
            return;
        }
        if (Collection.Contains(x, y))
        {
            Game1.activeClickableMenu = new ReadableCollectionMenu032(Mod, this);
            Game1.playSound("bigSelect");
            return;
        }
        if (Shelf.Contains(x, y))
        {
            Game1.activeClickableMenu = new ReadableShelfMenu032(Mod, this);
            Game1.playSound("bigSelect");
            return;
        }
        if (Close.Contains(x, y))
            exitThisMenu();
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        ReadableUi034.Begin(b, this, "주니모 카드샵", "팩 · 컬렉션 · 판매");

        int unique = Mod.Core.UniqueCardCount();
        int contentTop = yPositionOnScreen + Math.Min(104, Math.Max(92, height / 4));
        int statsH = Math.Min(86, Math.Max(72, height / 5));
        Rectangle stats = new(xPositionOnScreen + 20, contentTop, width - 40, statsH);
        CardUi.Panel(b, stats);

        int cell = stats.Width / 4;
        DrawStat(b, new Rectangle(stats.X, stats.Y, cell, stats.Height), "골드", $"{Game1.player.Money:N0}G");
        DrawStat(b, new Rectangle(stats.X + cell, stats.Y, cell, stats.Height), "미개봉", $"{Mod.State.UnopenedPacks}팩");
        DrawStat(b, new Rectangle(stats.X + cell * 2, stats.Y, cell, stats.Height), "컬렉션", $"{unique}/{Mod.Cards.Count}");
        DrawStat(b, new Rectangle(stats.X + cell * 3, stats.Y, stats.Width - cell * 3, stats.Height), "매출", $"{Mod.State.LifetimeCardRevenue:N0}G");

        DrawHomeTile(b, Pack, "팩 구매", $"1팩 {Mod.Config.PackPrice:N0}G");
        DrawHomeTile(b, Collection, "컬렉션", $"{unique}/{Mod.Cards.Count}");
        DrawHomeTile(b, Shelf, "판매 진열대", $"{Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots}");

        int summaryY = Math.Min(Close.Y - 64, Pack.Bottom + 12);
        Rectangle summary = new(xPositionOnScreen + 20, summaryY, width - 160, 52);
        CardUi.Panel(b, summary);
        string today = $"오늘  손님 {Mod.State.LastCustomerCount}명  ·  판매 {Mod.State.LastCardsSold}장  ·  +{Mod.State.LastDailyRevenue:N0}G";
        CardUi.CenterText(b, Game1.dialogueFont, today, new Rectangle(summary.X + 12, summary.Y + 7, summary.Width - 24, summary.Height - 14), CardUi.GreenDark, 0.98f);

        ReadableUi034.Button(b, Close, "닫기");
        drawMouse(b);
    }

    private static void DrawStat(SpriteBatch b, Rectangle r, string label, string value)
    {
        CardUi.CenterText(b, Game1.smallFont, label,
            new Rectangle(r.X + 4, r.Y + 7, r.Width - 8, 26), CardUi.Muted, 1.40f);
        CardUi.CenterText(b, Game1.dialogueFont, value,
            new Rectangle(r.X + 4, r.Y + 30, r.Width - 8, r.Height - 34), CardUi.Ink, 1.02f);
    }

    private static void DrawHomeTile(SpriteBatch b, Rectangle r, string title, string value)
    {
        CardUi.Panel(b, r);
        CardUi.CenterText(b, Game1.dialogueFont, title,
            new Rectangle(r.X + 10, r.Y + 16, r.Width - 20, r.Height / 2), CardUi.GreenDark, 1.08f);
        CardUi.CenterText(b, Game1.smallFont, value,
            new Rectangle(r.X + 10, r.Y + r.Height / 2 + 15, r.Width - 20, r.Height / 3), CardUi.Ink, 1.42f);
    }
}

internal sealed class ReadablePackMenu032 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle BuyOne;
    private readonly Rectangle BuyFive;
    private readonly Rectangle Open;
    private readonly Rectangle Back;
    private string Message = "5번째 언커먼+ · 10팩 Rare+ 천장";

    internal ReadablePackMenu032(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = ReadableUi034.FullCenter(1180, 720);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int buttonY = r.Bottom - 108;
        int gap = 12;
        int buttonW = (r.Width - 52 - gap * 2) / 3;
        BuyOne = new Rectangle(r.X + 20, buttonY, buttonW, 52);
        BuyFive = new Rectangle(BuyOne.Right + gap, buttonY, buttonW, 52);
        Open = new Rectangle(BuyFive.Right + gap, buttonY, buttonW, 52);
        Back = new Rectangle(r.Right - 120, r.Bottom - 48, 95, 36);
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
            Game1.activeClickableMenu = new ReadablePackOpeningMenu032(Mod, this);
            Game1.playSound("bigSelect");
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        ReadableUi034.Begin(b, this, "팩 구매", "Pelican Origins · 5장");

        int top = yPositionOnScreen + Math.Min(108, Math.Max(94, height / 4));
        int bottom = BuyOne.Y - 12;
        int infoH = Math.Max(120, bottom - top);

        int packW = Math.Min(180, Math.Max(130, width / 5));
        Rectangle pack = new(xPositionOnScreen + 30, top, packW, infoH);
        Rectangle packCard = new(pack.X + 10, pack.Y + 8, pack.Width - 20, pack.Height - 16);
        CardUi.DrawCardBack(b, packCard);

        Rectangle info = new(pack.Right + 16, top, xPositionOnScreen + width - 30 - (pack.Right + 16), infoH);
        CardUi.Panel(b, info);
        CardUi.CenterText(b, Game1.dialogueFont, $"보유 {Mod.State.UnopenedPacks}팩",
            new Rectangle(info.X + 15, info.Y + 12, info.Width - 30, info.Height / 3), CardUi.Ink, 1.10f);
        CardUi.CenterText(b, Game1.dialogueFont, "5번째 · 언커먼 이상",
            new Rectangle(info.X + 15, info.Y + info.Height / 3, info.Width - 30, info.Height / 3), CardUi.GreenDark, 0.98f);
        CardUi.CenterText(b, Game1.dialogueFont, $"Rare+ 천장 · {Mod.State.PacksSinceRare}/10",
            new Rectangle(info.X + 15, info.Y + info.Height * 2 / 3, info.Width - 30, info.Height / 3 - 8), CardUi.GreenDark, 0.94f);

        ReadableUi034.Button(b, BuyOne, $"1팩 {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        ReadableUi034.Button(b, BuyFive, $"5팩 {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        ReadableUi034.Button(b, Open, $"개봉 {Mod.State.UnopenedPacks}팩", Mod.State.UnopenedPacks > 0, true);

        CardUi.CenterText(b, Game1.smallFont, Message,
            new Rectangle(xPositionOnScreen + 22, BuyOne.Bottom + 3, width - 155, 36), CardUi.Muted, 1.28f);
        ReadableUi034.Button(b, Back, "뒤로");
        drawMouse(b);
    }
}

internal sealed class ReadablePackOpeningMenu032 : IClickableMenu
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
    private bool CompletionCelebrated;
    private float CompletionFx = -1f;

    internal ReadablePackOpeningMenu032(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = ReadableUi034.FullCenter(1360, 760);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        if (!Mod.TryOpenPack(out Pulls, out OpeningMessage))
            Pulls = new List<CardPull>();

        Revealed = new bool[Pulls.Count];
        RevealFx = Enumerable.Repeat(-1f, Pulls.Count).ToArray();

        int gap = Math.Max(6, Math.Min(14, r.Width / 70));
        int available = r.Width - 28 - gap * 4;
        int cardW = Math.Max(72, available / 5);
        int top = r.Y + Math.Min(106, Math.Max(92, r.Height / 4));
        int choicesReserve = 88;
        int cardH = Math.Max(170, Math.Min(330, r.Bottom - choicesReserve - top - 38));
        int total = cardW * 5 + gap * 4;
        int startX = r.X + (r.Width - total) / 2;

        for (int i = 0; i < 5; i++)
            Cards.Add(new Rectangle(startX + i * (cardW + gap), top, cardW, cardH));

        int choiceW = Math.Min(250, Math.Max(150, (r.Width - 70) / 2));
        PackChoice = new Rectangle(r.Center.X - choiceW - 8, r.Bottom - 62, choiceW, 46);
        CollectionChoice = new Rectangle(r.Center.X + 8, r.Bottom - 62, choiceW, 46);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pulls.Count == 0)
        {
            Game1.activeClickableMenu = ReturnMenu;
            return;
        }

        bool complete = Revealed.Length > 0 && Revealed.All(p => p);
        if (complete && CompletionFx >= 0.45f)
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
                Game1.activeClickableMenu = new ReadableCollectionMenu032(Mod, ReturnMenu);
                return;
            }
        }

        if (InputLock > 0f)
            return;

        for (int i = 0; i < Pulls.Count && i < Cards.Count; i++)
        {
            if (!Revealed[i] && Cards[i].Contains(x, y))
            {
                Revealed[i] = true;
                RevealFx[i] = 0f;
                ActiveReveal = i;

                CardDefinition? def = Mod.FindCard(Pulls[i].CardKey);
                ActiveRank = def is null ? 0 : ModEntry.GetRarityRank(def.Rarity);
                InputLock = ActiveRank >= 4 ? 0.72f : ActiveRank >= 2 ? 0.52f : 0.30f;

                if (ActiveRank >= 4)
                    Game1.playSound("reward");
                else if (ActiveRank >= 2)
                    Game1.playSound("newArtifact");
                else
                    Game1.playSound("coin");
                return;
            }
        }
    }

    public override void update(GameTime time)
    {
        base.update(time);
        float dt = Math.Min(0.05f, (float)time.ElapsedGameTime.TotalSeconds);
        InputLock = Math.Max(0f, InputLock - dt);

        for (int i = 0; i < RevealFx.Length; i++)
        {
            if (RevealFx[i] >= 0f && RevealFx[i] < 2.2f)
                RevealFx[i] += dt;
        }

        if (ActiveReveal >= 0 && ActiveReveal < RevealFx.Length && RevealFx[ActiveReveal] > 1.55f)
            ActiveReveal = -1;

        if (!CompletionCelebrated && Revealed.Length > 0 && Revealed.All(p => p) && RevealFx.All(p => p >= 0.72f))
        {
            CompletionCelebrated = true;
            CompletionFx = 0f;
            Game1.playSound("reward");
        }

        if (CompletionFx >= 0f && CompletionFx < 3f)
            CompletionFx += dt;
    }

    public override void draw(SpriteBatch b)
    {
        ReadableUi034.Begin(b, this, "팩 개봉", OpeningMessage.Contains("천장") ? OpeningMessage : "원하는 카드를 눌러 공개하세요");

        DrawDramaBackdrop(b);

        for (int i = 0; i < Cards.Count; i++)
        {
            Rectangle r = Cards[i];
            if (i >= Pulls.Count)
            {
                CardUi.DrawCardBack(b, r);
                continue;
            }

            if (!Revealed[i])
            {
                float pulse = 1f + MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalSeconds * 4f + i) * 0.018f;
                Rectangle hover = ScaleAroundCenter(r, pulse, pulse);
                CardUi.DrawCardBack(b, hover);
                CardUi.CenterText(b, Game1.smallFont, "클릭",
                    new Rectangle(r.X, r.Bottom + 3, r.Width, 27), CardUi.GreenDark, 1.28f);
                continue;
            }

            CardPull pull = Pulls[i];
            CardDefinition? def = Mod.FindCard(pull.CardKey);
            if (def is null)
                continue;

            int rank = ModEntry.GetRarityRank(def.Rarity);
            DrawAnimatedCard(b, r, def, pull, rank, Math.Max(0f, RevealFx[i]), i == ActiveReveal);
        }

        if (CompletionFx >= 0f)
            DrawCompletionCelebration(b);

        if (Revealed.Length > 0 && Revealed.All(p => p) && CompletionFx >= 0.45f)
        {
            ReadableUi034.Button(b, PackChoice, "팩 구매로", true, false);
            ReadableUi034.Button(b, CollectionChoice, "컬렉션으로", true, true);
        }
        else
        {
            int opened = Revealed.Count(p => p);
            CardUi.CenterText(b, Game1.dialogueFont, $"{opened}/5",
                new Rectangle(xPositionOnScreen + width / 2 - 65, yPositionOnScreen + height - 67, 130, 42),
                CardUi.Ink, 1.02f);
        }

        drawMouse(b);
    }

    private void DrawDramaBackdrop(SpriteBatch b)
    {
        if (ActiveReveal < 0 || ActiveReveal >= Pulls.Count || ActiveReveal >= RevealFx.Length)
            return;

        float t = RevealFx[ActiveReveal];
        if (t < 0f || t > 1.55f)
            return;

        CardDefinition? def = Mod.FindCard(Pulls[ActiveReveal].CardKey);
        if (def is null)
            return;

        int rank = ModEntry.GetRarityRank(def.Rarity);
        if (rank < 2)
            return;

        Color accent = CardUi.RarityColor(def.Rarity);
        float dim = rank >= 4 ? 0.40f : 0.24f;
        float fade = t < 0.18f ? t / 0.18f : Math.Clamp(1f - (t - 0.75f) / 0.80f, 0f, 1f);
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
            Color.Black * (dim * fade));

        Rectangle target = Cards[ActiveReveal];
        Vector2 center = new(target.Center.X, target.Center.Y);

        if (rank >= 4 && t > 0.18f && t < 1.20f)
        {
            float rayLife = Math.Clamp(1f - Math.Abs(t - 0.62f) / 0.58f, 0f, 1f);
            int rays = rank >= 5 ? 20 : 14;
            for (int i = 0; i < rays; i++)
            {
                float a = i * MathF.PI * 2f / rays + t * 0.65f;
                Color rayColor = rank >= 5 ? RainbowColor(i, rays, t) : accent;
                Vector2 scale = new(target.Width * (rank >= 5 ? 1.9f : 1.55f), rank >= 5 ? 5f : 4f);
                b.Draw(Game1.fadeToBlackRect, center, null, rayColor * (0.18f * rayLife), a, new Vector2(0f, 0.5f), scale, SpriteEffects.None, 0f);
            }
        }

        if (t > 0.24f && t < 0.52f)
        {
            float flash = 1f - Math.Abs(t - 0.36f) / 0.16f;
            flash = Math.Clamp(flash, 0f, 1f);
            Color flashColor = rank >= 4 ? new Color(255, 239, 177) : accent;
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                flashColor * (flash * (rank >= 4 ? 0.22f : 0.10f)));
        }

        string banner = rank >= 5
            ? "✦✦ 시크릿!!! ✦✦"
            : rank >= 4
                ? "★ 레전더리!! ★"
                : rank == 3
                    ? "에픽!"
                    : "레어!";

        if (t > 0.34f && t < 1.32f)
        {
            float bannerLife = Math.Clamp(Math.Min((t - 0.34f) / 0.18f, (1.32f - t) / 0.30f), 0f, 1f);
            Color bannerColor = rank >= 5 ? RainbowColor((int)(t * 10f), 12, t) : accent;
            Rectangle bannerRect = new(xPositionOnScreen + width / 2 - Math.Min(260, width / 3), yPositionOnScreen + 86, Math.Min(520, width * 2 / 3), 54);
            CardUi.CenterText(b, Game1.dialogueFont, banner, bannerRect, bannerColor * bannerLife, rank >= 4 ? 1.18f : 1.02f);
        }
    }

    private void DrawAnimatedCard(SpriteBatch b, Rectangle baseRect, CardDefinition def, CardPull pull, int rank, float t, bool active)
    {
        Color accent = rank >= 1 ? CardUi.RarityColor(def.Rarity) : CardUi.Gold;

        if (t < 0.13f)
        {
            float press = 1f - MathF.Sin(t / 0.13f * MathF.PI) * 0.035f;
            CardUi.DrawCardBack(b, ScaleAroundCenter(baseRect, press, press));
            return;
        }

        if (t < 0.30f)
        {
            float p = SmoothStep((t - 0.13f) / 0.17f);
            float widthScale = 1f - p * 0.95f;
            float heightScale = 1f + MathF.Sin(p * MathF.PI) * 0.06f;
            CardUi.DrawCardBack(b, ScaleAroundCenter(baseRect, widthScale, heightScale));
            return;
        }

        float revealP = Math.Clamp((t - 0.30f) / 0.34f, 0f, 1f);
        float sx = 0.05f + 0.95f * EaseOutBack(revealP);

        float rarityBoost = rank >= 5 ? 0.22f : rank >= 4 ? 0.17f : rank >= 2 ? 0.11f : 0.07f;
        float bouncePhase = Math.Clamp((t - 0.30f) / 0.62f, 0f, 1f);
        float zoom = 1f + MathF.Sin(bouncePhase * MathF.PI) * rarityBoost;
        float lift = MathF.Sin(bouncePhase * MathF.PI) * (rank >= 4 ? 24f : rank >= 2 ? 17f : 10f);
        float shake = active && rank >= 2 && t < 0.78f
            ? MathF.Sin(t * (rank >= 4 ? 76f : 58f)) * (rank >= 4 ? 5f : 2.5f)
            : 0f;

        Rectangle drawRect = ScaleAroundCenter(baseRect, Math.Max(0.05f, sx) * zoom, zoom);
        drawRect.Offset((int)Math.Round(shake), -(int)Math.Round(lift));

        if (t < 1.45f)
            DrawBurst(b, baseRect, accent, rank, t);

        ReadableUi034.DrawSimpleCardFront(b, drawRect, def, pull, rank >= 2);

        if (t < 1.30f)
        {
            float pulse = Math.Clamp(1f - (t - 0.30f) / 1.0f, 0f, 1f);
            int pad = 6 + (int)(pulse * (rank >= 5 ? 30 : rank >= 4 ? 24 : rank >= 2 ? 15 : 8));
            Rectangle aura = new(drawRect.X - pad, drawRect.Y - pad, drawRect.Width + pad * 2, drawRect.Height + pad * 2);
            CardUi.Border(b, aura, accent * (0.30f + pulse * 0.65f), rank >= 4 ? 6 : rank >= 2 ? 4 : 2);
            if (rank >= 5)
                CardUi.Border(b, new Rectangle(aura.X - 8, aura.Y - 8, aura.Width + 16, aura.Height + 16), RainbowColor((int)(t * 12f), 12, t) * 0.70f, 3);
        }
    }

    private static void DrawBurst(SpriteBatch b, Rectangle r, Color color, int rank, float t)
    {
        float local = Math.Max(0f, t - 0.24f);
        float life = Math.Clamp(1f - local / 1.22f, 0f, 1f);
        if (life <= 0f)
            return;

        int count = rank >= 5 ? 28 : rank >= 4 ? 22 : rank >= 2 ? 15 : 8;
        float radius = 18f + local * (rank >= 4 ? 145f : rank >= 2 ? 105f : 70f);

        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.PI * 2f / count + local * (rank >= 4 ? 2.4f : 1.5f);
            int px = r.Center.X + (int)(MathF.Cos(a) * radius);
            int py = r.Center.Y + (int)(MathF.Sin(a) * radius * 0.68f);
            int size = rank >= 4 ? 8 : rank >= 2 ? 6 : 4;
            Color particle = rank >= 5 ? RainbowColor(i, count, t) : color;
            b.Draw(Game1.fadeToBlackRect, new Rectangle(px - size / 2, py - size / 2, size, size), particle * (0.22f + life * 0.72f));

            if (rank >= 4 && i % 3 == 0)
            {
                int cross = size + 5;
                b.Draw(Game1.fadeToBlackRect, new Rectangle(px - cross / 2, py - 1, cross, 2), Color.White * (life * 0.70f));
                b.Draw(Game1.fadeToBlackRect, new Rectangle(px - 1, py - cross / 2, 2, cross), Color.White * (life * 0.70f));
            }
        }
    }

    private void DrawCompletionCelebration(SpriteBatch b)
    {
        if (CompletionFx < 0f || CompletionFx > 2.2f)
            return;

        float t = CompletionFx;
        float life = Math.Clamp(1f - t / 2.2f, 0f, 1f);
        int pieces = 30;

        for (int i = 0; i < pieces; i++)
        {
            float lane = (i + 0.5f) / pieces;
            int x = xPositionOnScreen + 12 + (int)(lane * (width - 24));
            float fall = (t * (120f + (i % 5) * 22f) + (i * 17) % Math.Max(1, height)) % Math.Max(1, height - 20);
            int y = yPositionOnScreen + (int)fall;
            Color c = RainbowColor(i, pieces, t);
            int w = 4 + i % 4;
            int h = 8 + i % 6;
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, w, h), c * (0.30f + life * 0.55f));
        }

        if (t < 1.45f)
        {
            float pop = t < 0.28f ? EaseOutBack(t / 0.28f) : 1f;
            string text = "팩 오픈 완료!";
            Rectangle banner = new(xPositionOnScreen + width / 2 - Math.Min(220, width / 3), yPositionOnScreen + 88, Math.Min(440, width * 2 / 3), 52);
            CardUi.CenterText(b, Game1.dialogueFont, text, banner, CardUi.Gold, 1.12f * Math.Max(0.1f, pop));
        }
    }

    private static Color RainbowColor(int index, int count, float t)
    {
        float h = ((index / (float)Math.Max(1, count)) + t * 0.22f) % 1f;
        return HsvToRgb(h, 0.78f, 1f);
    }

    private static Color HsvToRgb(float h, float s, float v)
    {
        h = (h % 1f + 1f) % 1f;
        float c = v * s;
        float x = c * (1f - MathF.Abs((h * 6f) % 2f - 1f));
        float m = v - c;
        float r, g, b;

        if (h < 1f / 6f) { r = c; g = x; b = 0f; }
        else if (h < 2f / 6f) { r = x; g = c; b = 0f; }
        else if (h < 3f / 6f) { r = 0f; g = c; b = x; }
        else if (h < 4f / 6f) { r = 0f; g = x; b = c; }
        else if (h < 5f / 6f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }

        return new Color(r + m, g + m, b + m);
    }

    private static Rectangle ScaleAroundCenter(Rectangle r, float sx, float sy)
    {
        int w = Math.Max(2, (int)Math.Round(r.Width * sx));
        int h = Math.Max(2, (int)Math.Round(r.Height * sy));
        return new Rectangle(r.Center.X - w / 2, r.Center.Y - h / 2, w, h);
    }

    private static float SmoothStep(float p)
    {
        p = Math.Clamp(p, 0f, 1f);
        return p * p * (3f - 2f * p);
    }

    private static float EaseOutBack(float p)
    {
        p = Math.Clamp(p, 0f, 1f);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * MathF.Pow(p - 1f, 3f) + c1 * MathF.Pow(p - 1f, 2f);
    }
}

internal sealed class ReadableCollectionMenu032 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly int TargetSlot;
    private readonly Dictionary<string, Rectangle> Filters = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Key, Rectangle Bounds)> Hits = new();
    private readonly Rectangle Prev;
    private readonly Rectangle Next;
    private readonly Rectangle List;
    private readonly Rectangle Bonus;
    private readonly Rectangle Shelf;
    private readonly Rectangle Back;

    private string Filter = "All";
    private string SelectedKey = "";
    private string Message = "카드를 선택하세요";
    private int Page;

    internal ReadableCollectionMenu032(ModEntry mod, IClickableMenu returnMenu, int targetSlot = -1)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        TargetSlot = targetSlot;

        Rectangle r = ReadableUi034.FullCenter(1360, 780);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        string[] names = { "All", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Secret" };
        int gap = 5;
        int fw = (r.Width - 30 - gap * 6) / 7;
        int startX = r.X + 15;
        int filterY = r.Y + Math.Min(100, Math.Max(88, r.Height / 4));
        for (int i = 0; i < names.Length; i++)
            Filters[names[i]] = new Rectangle(startX + i * (fw + gap), filterY, fw, 42);

        int controlsY = r.Bottom - 50;
        Prev = new Rectangle(r.X + 18, controlsY, 90, 36);
        Next = new Rectangle(r.X + 115, controlsY, 90, 36);
        Bonus = new Rectangle(r.X + 215, controlsY, Math.Min(180, r.Width / 4), 36);
        Shelf = new Rectangle(r.Right - 205, controlsY, 95, 36);
        Back = new Rectangle(r.Right - 103, controlsY, 85, 36);

        int detailW = Math.Max(210, Math.Min(320, r.Width / 3));
        int detailX = r.Right - detailW - 18;
        int gridTop = filterY + 50;
        int gridBottom = controlsY - 46;
        List = new Rectangle(detailX + 10, gridBottom - 48, detailW - 20, 42);
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
            if (hit.Bounds.Contains(x, y))
            {
                SelectedKey = hit.Key;
                Game1.playSound("smallSelect");
                return;
            }
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 4);

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
            Game1.activeClickableMenu = new ReadableShelfMenu032(Mod, this);
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        ReadableUi034.Begin(b, this, "컬렉션", $"수집 {Mod.Core.UniqueCardCount()}/{Mod.Cards.Count}");

        foreach (var pair in Filters)
        {
            int count = pair.Key == "All" ? Mod.Core.UniqueCardCount() : Mod.Core.UniqueCountForRarity(pair.Key);
            ReadableUi034.Button(b, pair.Value, $"{FilterName(pair.Key)} {count}", true, false,
                string.Equals(Filter, pair.Key, StringComparison.OrdinalIgnoreCase));
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 4);
        Page = Math.Clamp(Page, 0, maxPage);
        int start = Page * 4;
        Hits.Clear();

        int filterY = Filters["All"].Y;
        int gridTop = filterY + 50;
        int controlsY = Prev.Y;
        int detailW = Math.Max(210, Math.Min(320, width / 3));
        int detailX = xPositionOnScreen + width - detailW - 18;
        int gridLeft = xPositionOnScreen + 18;
        int gridRight = detailX - 12;
        int gridBottom = controlsY - 8;
        int gapX = 10;
        int gapY = 8;
        int cardW = Math.Max(120, (gridRight - gridLeft - gapX) / 2);
        int cardH = Math.Max(78, (gridBottom - gridTop - gapY) / 2);

        for (int i = 0; i < 4 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            int col = i % 2;
            int rr = i / 2;
            Rectangle card = new(gridLeft + col * (cardW + gapX), gridTop + rr * (cardH + gapY), cardW, cardH);
            CardUi.Panel(b, card, row.CollectionKey == SelectedKey);

            Rectangle band = new(card.X + 7, card.Y + 7, card.Width - 14, Math.Min(30, card.Height / 4));
            b.Draw(Game1.fadeToBlackRect, band, CardUi.RarityColor(row.Card.Rarity));
            CardUi.CenterText(b, Game1.smallFont, ModEntry.RarityName(row.Card.Rarity), band, Color.White, 1.28f);

            CardUi.CenterText(b, Game1.dialogueFont, row.Card.Name,
                new Rectangle(card.X + 8, band.Bottom + 2, card.Width - 16, Math.Max(30, card.Height / 2 - 10)),
                CardUi.Ink, 0.98f);

            CardUi.CenterText(b, Game1.smallFont, $"보유 {row.Count}장",
                new Rectangle(card.X + 8, card.Bottom - Math.Min(34, card.Height / 3), card.Width - 16, Math.Min(30, card.Height / 3)),
                CardUi.Ink, 1.32f);

            Hits.Add((row.CollectionKey, card));
        }

        Rectangle detail = new(detailX, gridTop, detailW, gridBottom - gridTop);
        CardUi.Panel(b, detail, true);

        var selected = rows.FirstOrDefault(p => p.CollectionKey == SelectedKey);
        if (selected.Card is not null)
        {
            CardUi.CenterText(b, Game1.dialogueFont, selected.Card.Name,
                new Rectangle(detail.X + 10, detail.Y + 10, detail.Width - 20, 44), CardUi.Ink, 1.05f);
            CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.RarityName(selected.Card.Rarity)} · {ModEntry.VariantName(selected.Variant)}",
                new Rectangle(detail.X + 10, detail.Y + 57, detail.Width - 20, 30), CardUi.RarityColor(selected.Card.Rarity), 1.30f);
            CardUi.CenterText(b, Game1.smallFont, selected.Condition,
                new Rectangle(detail.X + 10, detail.Y + 88, detail.Width - 20, 28), CardUi.Ink, 1.26f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{selected.Value:N0}G",
                new Rectangle(detail.X + 10, detail.Y + 118, detail.Width - 20, 42), CardUi.GreenDark, 0.96f);

            int available = CardShopRules.GetListableCount(Mod, SelectedKey);
            CardUi.CenterText(b, Game1.smallFont, $"판매 가능 {available}장",
                new Rectangle(detail.X + 10, List.Y - 34, detail.Width - 20, 28), CardUi.Muted, 1.24f);
            ReadableUi034.Button(b, List, TargetSlot >= 0 ? $"{TargetSlot + 1}번에 진열" : "판매 진열", available > 0, true);
        }
        else
        {
            CardUi.CenterText(b, Game1.dialogueFont, "카드를 선택하세요",
                new Rectangle(detail.X + 12, detail.Y + 45, detail.Width - 24, 70), CardUi.Muted, 0.96f);
        }

        var bonus = Mod.Core.GetNextCollectionBonus();
        ReadableUi034.Button(b, Bonus,
            bonus.Complete ? "보너스 완료" : $"{bonus.Required}종 → 팩 {bonus.Reward}",
            !bonus.Complete, bonus.CanClaim);

        ReadableUi034.Button(b, Prev, "이전", Page > 0);
        ReadableUi034.Button(b, Next, "다음", Page < maxPage);
        ReadableUi034.Button(b, Shelf, "판매대");
        ReadableUi034.Button(b, Back, "뒤로");

        CardUi.CenterText(b, Game1.smallFont, $"{Page + 1}/{maxPage + 1} · {Message}",
            new Rectangle(Bonus.Right + 8, controlsY, Math.Max(80, Shelf.X - Bonus.Right - 16), 36),
            CardUi.Muted, 1.16f);

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

internal sealed class ReadableShelfMenu032 : IClickableMenu
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

    internal ReadableShelfMenu032(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;

        Rectangle r = ReadableUi034.FullCenter(1360, 780);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int top = r.Y + Math.Min(106, Math.Max(92, r.Height / 4));
        int controlsY = r.Bottom - 48;
        int infoY = controlsY - 38;
        int gridBottom = infoY - 8;
        int gapX = 8;
        int gapY = 8;
        int slotW = (r.Width - 30 - gapX * 3) / 4;
        int slotH = Math.Max(70, (gridBottom - top - gapY) / 2);
        int startX = r.X + 15;

        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Slots.Add(new Rectangle(startX + col * (slotW + gapX), top + row * (slotH + gapY), slotW, slotH));
        }

        int gap = 6;
        int controlW = (r.Width - 30 - gap * 4) / 5;
        Add = new Rectangle(r.X + 15, controlsY, controlW, 34);
        Down = new Rectangle(Add.Right + gap, controlsY, controlW, 34);
        Up = new Rectangle(Down.Right + gap, controlsY, controlW, 34);
        Remove = new Rectangle(Up.Right + gap, controlsY, controlW, 34);
        Back = new Rectangle(Remove.Right + gap, controlsY, controlW, 34);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].Contains(x, y))
            {
                SelectedSlot = i;
                Game1.playSound("smallSelect");
                return;
            }
        }

        SaleListing? listing = Mod.Core.GetListingAtSlot(SelectedSlot);

        if (Add.Contains(x, y) && listing is null)
        {
            Game1.activeClickableMenu = new ReadableCollectionMenu032(Mod, this, SelectedSlot);
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
        ReadableUi034.Begin(b, this, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots} · 하루 최대 {Mod.Config.MaxDailySales}장");

        IReadOnlyList<SaleListing?> shelf = Mod.Core.GetShelfSlots();

        for (int i = 0; i < Slots.Count; i++)
        {
            Rectangle r = Slots[i];
            CardUi.Panel(b, r, i == SelectedSlot);
            SaleListing? listing = i < shelf.Count ? shelf[i] : null;

            if (listing is null)
            {
                CardUi.CenterText(b, Game1.dialogueFont, "+",
                    new Rectangle(r.X + 6, r.Y + 8, r.Width - 12, r.Height / 2), CardUi.Muted, 1.14f);
                CardUi.CenterText(b, Game1.smallFont, $"{i + 1}번",
                    new Rectangle(r.X + 6, r.Y + r.Height / 2, r.Width - 12, r.Height / 3), CardUi.Muted, 1.30f);
                continue;
            }

            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string slotVariant, out string slotCondition))
                continue;

            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null)
                continue;

            CardUi.CenterText(b, Game1.dialogueFont, card.Name,
                new Rectangle(r.X + 8, r.Y + 12, r.Width - 16, r.Height / 2), CardUi.Ink, 1.02f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{listing.Price:N0}G",
                new Rectangle(r.X + 8, r.Y + r.Height / 2, r.Width - 16, r.Height / 3), CardUi.GreenDark, 0.94f);
        }

        SaleListing? selected = Mod.Core.GetListingAtSlot(SelectedSlot);
        string selectedInfo = selected is null ? "빈 슬롯" : $"{SelectedSlot + 1}번";

        if (selected is not null && CardKeys.TryParse(selected.CollectionKey, out string key, out string selectedVariant, out string selectedCondition))
        {
            CardDefinition? card = Mod.FindCard(key);
            if (card is not null)
                selectedInfo = $"{card.Name} · {ModEntry.VariantName(selectedVariant)} · {selectedCondition} · 판매확률 {Mod.Core.GetSaleChance(selected) * 100:0}%";
        }

        Rectangle info = new(xPositionOnScreen + 15, Add.Y - 36, width - 30, 30);
        string infoText = string.Equals(Message, "슬롯을 선택하세요", StringComparison.Ordinal)
            ? selectedInfo
            : Message;
        CardUi.CenterText(b, Game1.smallFont, infoText, info, CardUi.Ink, 1.26f);

        ReadableUi034.Button(b, Add, selected is null ? "카드 넣기" : "사용 중", selected is null, true);
        ReadableUi034.Button(b, Down, "가격 -50", selected is not null);
        ReadableUi034.Button(b, Up, "가격 +50", selected is not null);
        ReadableUi034.Button(b, Remove, "회수", selected is not null);
        ReadableUi034.Button(b, Back, "뒤로");

        drawMouse(b);
    }
}
