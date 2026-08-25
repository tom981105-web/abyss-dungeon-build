using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

// v0.3.6 UI balance pass.
// Keeps the dramatic 0.3.4 reveal idea, but reduces oversized panels/cards and
// brings tiny/huge text toward one readable middle scale.
internal static class BalancedUi036
{
    internal static readonly Color TestGold = new(231, 181, 71);
    private static readonly Color[] Rainbow =
    {
        new(255, 92, 92), new(255, 186, 72), new(244, 229, 91),
        new(87, 199, 112), new(87, 170, 245), new(161, 109, 230)
    };

    internal static void Begin(SpriteBatch b, IClickableMenu menu, string title, string subtitle = "")
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.52f);
        IClickableMenu.drawTextureBox(b, menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height, Color.White);

        Rectangle header = new(menu.xPositionOnScreen + 18, menu.yPositionOnScreen + 14, menu.width - 36, 78);
        IClickableMenu.drawTextureBox(b, header.X, header.Y, header.Width, header.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(header.X + 7, header.Y + 7, header.Width - 14, header.Height - 14), CardUi.GreenDark);

        CardUi.CenterText(b, Game1.dialogueFont, title,
            new Rectangle(header.X + 14, header.Y + 4, header.Width - 28, 43), Color.White, 0.98f);
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            CardUi.CenterText(b, Game1.smallFont, subtitle,
                new Rectangle(header.X + 14, header.Y + 48, header.Width - 28, 23),
                new Color(242, 234, 188), 1.08f);
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
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14), fill * 0.88f);
        CardUi.CenterText(b, Game1.dialogueFont, text,
            new Rectangle(r.X + 8, r.Y + 6, r.Width - 16, r.Height - 12),
            enabled ? Color.White : new Color(224, 220, 207), 0.82f);
    }

    internal static void MiniButton(SpriteBatch b, Rectangle r, string text, bool enabled = true, bool selected = false)
    {
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
        Color fill = !enabled ? new Color(139, 132, 117) : selected ? CardUi.Green : new Color(172, 118, 50);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + 6, r.Y + 6, r.Width - 12, r.Height - 12), fill * 0.88f);
        CardUi.CenterText(b, Game1.smallFont, text,
            new Rectangle(r.X + 7, r.Y + 5, r.Width - 14, r.Height - 10),
            enabled ? Color.White : new Color(224, 220, 207), 1.08f);
    }

    internal static void DrawCardFront(SpriteBatch b, Rectangle r, CardDefinition card, CardPull pull, bool glow)
    {
        Color rarity = CardUi.RarityColor(card.Rarity);
        b.Draw(Game1.fadeToBlackRect, r, new Color(250, 235, 198));
        CardUi.Border(b, r, glow ? rarity : new Color(94, 70, 44), glow ? 5 : 3);

        Rectangle band = new(r.X + 7, r.Y + 7, r.Width - 14, 27);
        b.Draw(Game1.fadeToBlackRect, band, rarity);
        CardUi.CenterText(b, Game1.smallFont, ModEntry.RarityName(card.Rarity), band, Color.White, 1.04f);

        Rectangle emblem = new(r.X + 14, band.Bottom + 8, r.Width - 28, Math.Max(58, r.Height / 3));
        b.Draw(Game1.fadeToBlackRect, emblem, rarity * 0.22f);
        string initial = string.IsNullOrWhiteSpace(card.Name) ? "?" : card.Name[..1];
        CardUi.CenterText(b, Game1.dialogueFont, initial, emblem, rarity, 1.14f);

        int nameY = emblem.Bottom + 3;
        CardUi.CenterText(b, Game1.dialogueFont, card.Name,
            new Rectangle(r.X + 8, nameY, r.Width - 16, 34), CardUi.Ink, 0.78f);
        CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(pull.Variant)} · {pull.Condition}",
            new Rectangle(r.X + 8, nameY + 35, r.Width - 16, 25), CardUi.Ink, 0.96f);
        CardUi.CenterText(b, Game1.dialogueFont, $"{pull.MarketValue:N0}G",
            new Rectangle(r.X + 8, r.Bottom - 33, r.Width - 16, 26), CardUi.GreenDark, 0.76f);
    }

    internal static Color RainbowColor(int index, float time)
    {
        int shift = (int)(time * 4f);
        return Rainbow[Math.Abs(index + shift) % Rainbow.Length];
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

internal sealed class BalancedPackMenu036 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle BuyOne;
    private readonly Rectangle BuyFive;
    private readonly Rectangle Open;
    private readonly Rectangle Back;
    private string Message = "TEST · 모든 등급 16.67%";

    internal BalancedPackMenu036(ModEntry mod, IClickableMenu returnMenu)
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
        BuyOne = new Rectangle(r.X + 38, buttonY, buttonW, 52);
        BuyFive = new Rectangle(BuyOne.Right + gap, buttonY, buttonW, 52);
        Open = new Rectangle(BuyFive.Right + gap, buttonY, buttonW, 52);
        Back = new Rectangle(r.Right - 130, r.Bottom - 48, 96, 36);
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
            Game1.activeClickableMenu = new BalancedPackOpeningMenu036(Mod, this);
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        BalancedUi036.Begin(b, this, "팩 구매", "Pelican Origins · 테스트 확률");

        Rectangle pack = new(xPositionOnScreen + 52, yPositionOnScreen + 112, 168, 226);
        CardUi.DrawCardBack(b, pack);

        Rectangle info = new(pack.Right + 22, yPositionOnScreen + 112, width - 52 - (pack.Right + 22 - xPositionOnScreen), 226);
        CardUi.Panel(b, info);
        CardUi.CenterText(b, Game1.dialogueFont, $"보유 {Mod.State.UnopenedPacks}팩",
            new Rectangle(info.X + 16, info.Y + 16, info.Width - 32, 48), CardUi.Ink, 0.90f);
        CardUi.CenterText(b, Game1.smallFont, "커먼 · 언커먼 · 레어",
            new Rectangle(info.X + 16, info.Y + 80, info.Width - 32, 30), CardUi.GreenDark, 1.10f);
        CardUi.CenterText(b, Game1.smallFont, "에픽 · 레전더리 · 시크릿",
            new Rectangle(info.X + 16, info.Y + 114, info.Width - 32, 30), CardUi.GreenDark, 1.10f);
        CardUi.CenterText(b, Game1.dialogueFont, "각 16.67%",
            new Rectangle(info.X + 16, info.Y + 158, info.Width - 32, 42), BalancedUi036.TestGold, 0.84f);

        BalancedUi036.Button(b, BuyOne, $"1팩 {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        BalancedUi036.Button(b, BuyFive, $"5팩 {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        BalancedUi036.Button(b, Open, $"개봉 {Mod.State.UnopenedPacks}팩", Mod.State.UnopenedPacks > 0, true);
        CardUi.CenterText(b, Game1.smallFont, Message,
            new Rectangle(xPositionOnScreen + 40, BuyOne.Bottom + 6, width - 185, 30), CardUi.Muted, 1.06f);
        BalancedUi036.MiniButton(b, Back, "뒤로");
        drawMouse(b);
    }
}

internal sealed class BalancedPackOpeningMenu036 : IClickableMenu
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

    internal BalancedPackOpeningMenu036(ModEntry mod, IClickableMenu returnMenu)
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
        int cardH = 266;
        int top = r.Y + 112;
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
                Game1.activeClickableMenu = new BalancedCollectionMenu036(Mod, ReturnMenu);
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
            InputLock = ActiveRank >= 4 ? 0.68f : ActiveRank >= 2 ? 0.48f : 0.26f;
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

        if (ActiveReveal >= 0 && ActiveReveal < RevealFx.Length && RevealFx[ActiveReveal] > 1.45f)
            ActiveReveal = -1;

        if (!CompletionCelebrated && Revealed.Length > 0 && Revealed.All(p => p) && RevealFx.All(p => p >= 0.65f))
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
        BalancedUi036.Begin(b, this, "팩 개봉", OpeningMessage.StartsWith("TEST", StringComparison.OrdinalIgnoreCase) ? "TEST · 모든 등급 16.67%" : "카드를 눌러 공개하세요");
        DrawDrama(b);

        for (int i = 0; i < Cards.Count; i++)
        {
            Rectangle baseRect = Cards[i];
            if (i >= Pulls.Count)
            {
                CardUi.DrawCardBack(b, baseRect);
                continue;
            }

            if (!Revealed[i])
            {
                float pulse = 1f + MathF.Sin((float)Game1.currentGameTime.TotalGameTime.TotalSeconds * 4f + i) * 0.012f;
                CardUi.DrawCardBack(b, BalancedUi036.ScaleAroundCenter(baseRect, pulse, pulse));
                CardUi.CenterText(b, Game1.smallFont, "클릭", new Rectangle(baseRect.X, baseRect.Bottom + 4, baseRect.Width, 24), CardUi.GreenDark, 1.04f);
                continue;
            }

            CardDefinition? def = Mod.FindCard(Pulls[i].CardKey);
            if (def is null)
                continue;
            DrawAnimatedCard(b, baseRect, def, Pulls[i], ModEntry.GetRarityRank(def.Rarity), Math.Max(0f, RevealFx[i]), i == ActiveReveal);
        }

        if (CompletionFx >= 0f)
            DrawCompletion(b);

        bool complete = Revealed.Length > 0 && Revealed.All(p => p);
        if (complete && CompletionFx >= 0.35f)
        {
            BalancedUi036.Button(b, PackChoice, "팩 구매로");
            BalancedUi036.Button(b, CollectionChoice, "컬렉션으로", true, true);
        }
        else
        {
            int opened = Revealed.Count(p => p);
            CardUi.CenterText(b, Game1.dialogueFont, $"{opened}/5",
                new Rectangle(xPositionOnScreen + width / 2 - 58, yPositionOnScreen + 420, 116, 38), CardUi.Ink, 0.86f);
        }
        drawMouse(b);
    }

    private void DrawDrama(SpriteBatch b)
    {
        if (ActiveReveal < 0 || ActiveReveal >= Pulls.Count || ActiveReveal >= RevealFx.Length)
            return;
        float t = RevealFx[ActiveReveal];
        if (t < 0f || t > 1.45f)
            return;

        CardDefinition? def = Mod.FindCard(Pulls[ActiveReveal].CardKey);
        if (def is null)
            return;
        int rank = ModEntry.GetRarityRank(def.Rarity);
        if (rank < 2)
            return;

        Color accent = CardUi.RarityColor(def.Rarity);
        float fade = t < 0.16f ? t / 0.16f : Math.Clamp(1f - (t - 0.70f) / 0.75f, 0f, 1f);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * ((rank >= 4 ? 0.36f : 0.20f) * fade));

        Rectangle target = Cards[ActiveReveal];
        Vector2 center = new(target.Center.X, target.Center.Y);
        if (rank >= 4 && t > 0.16f && t < 1.15f)
        {
            float life = Math.Clamp(1f - Math.Abs(t - 0.58f) / 0.56f, 0f, 1f);
            int rays = rank >= 5 ? 18 : 12;
            for (int i = 0; i < rays; i++)
            {
                float a = i * MathF.PI * 2f / rays + t * 0.8f;
                Color ray = rank >= 5 ? BalancedUi036.RainbowColor(i, t) : accent;
                b.Draw(Game1.fadeToBlackRect, center, null, ray * (0.16f * life), a, new Vector2(0f, 0.5f), new Vector2(target.Width * 1.35f, 4f), SpriteEffects.None, 0f);
            }
        }

        if (t > 0.24f && t < 0.48f)
        {
            float flash = 1f - Math.Abs(t - 0.36f) / 0.12f;
            Color flashColor = rank >= 4 ? new Color(255, 240, 180) : accent;
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), flashColor * (Math.Clamp(flash, 0f, 1f) * (rank >= 4 ? 0.20f : 0.09f)));
        }

        if (t > 0.34f && t < 1.20f)
        {
            string banner = rank >= 5 ? "✦ 시크릿!!! ✦" : rank >= 4 ? "★ 레전더리!! ★" : rank == 3 ? "에픽!" : "레어!";
            Color bannerColor = rank >= 5 ? BalancedUi036.RainbowColor((int)(t * 10f), t) : accent;
            CardUi.CenterText(b, Game1.dialogueFont, banner,
                new Rectangle(xPositionOnScreen + width / 2 - 210, yPositionOnScreen + 86, 420, 42), bannerColor, rank >= 4 ? 1.00f : 0.88f);
        }
    }

    private void DrawAnimatedCard(SpriteBatch b, Rectangle baseRect, CardDefinition def, CardPull pull, int rank, float t, bool active)
    {
        Color accent = rank >= 1 ? CardUi.RarityColor(def.Rarity) : CardUi.Gold;
        if (t < 0.12f)
        {
            float press = 1f - MathF.Sin(t / 0.12f * MathF.PI) * 0.03f;
            CardUi.DrawCardBack(b, BalancedUi036.ScaleAroundCenter(baseRect, press, press));
            return;
        }
        if (t < 0.28f)
        {
            float p = BalancedUi036.SmoothStep((t - 0.12f) / 0.16f);
            CardUi.DrawCardBack(b, BalancedUi036.ScaleAroundCenter(baseRect, 1f - p * 0.95f, 1f + MathF.Sin(p * MathF.PI) * 0.05f));
            return;
        }

        float revealP = Math.Clamp((t - 0.28f) / 0.32f, 0f, 1f);
        float sx = 0.05f + 0.95f * BalancedUi036.EaseOutBack(revealP);
        float boost = rank >= 5 ? 0.18f : rank >= 4 ? 0.14f : rank >= 2 ? 0.09f : 0.05f;
        float bounceP = Math.Clamp((t - 0.28f) / 0.58f, 0f, 1f);
        float zoom = 1f + MathF.Sin(bounceP * MathF.PI) * boost;
        float lift = MathF.Sin(bounceP * MathF.PI) * (rank >= 4 ? 18f : rank >= 2 ? 12f : 7f);
        float shake = active && rank >= 2 && t < 0.72f ? MathF.Sin(t * 62f) * (rank >= 4 ? 3.5f : 1.8f) : 0f;

        Rectangle drawRect = BalancedUi036.ScaleAroundCenter(baseRect, Math.Max(0.05f, sx) * zoom, zoom);
        drawRect.Offset((int)Math.Round(shake), -(int)Math.Round(lift));
        if (t < 1.35f)
            DrawBurst(b, baseRect, accent, rank, t);
        BalancedUi036.DrawCardFront(b, drawRect, def, pull, rank >= 2);

        if (t < 1.15f)
        {
            float pulse = Math.Clamp(1f - (t - 0.28f) / 0.87f, 0f, 1f);
            int pad = 5 + (int)(pulse * (rank >= 5 ? 22 : rank >= 4 ? 17 : rank >= 2 ? 11 : 6));
            Rectangle aura = new(drawRect.X - pad, drawRect.Y - pad, drawRect.Width + pad * 2, drawRect.Height + pad * 2);
            CardUi.Border(b, aura, accent * (0.28f + pulse * 0.60f), rank >= 4 ? 5 : rank >= 2 ? 3 : 2);
        }
    }

    private static void DrawBurst(SpriteBatch b, Rectangle r, Color color, int rank, float t)
    {
        float local = Math.Max(0f, t - 0.20f);
        float life = Math.Clamp(1f - local / 1.15f, 0f, 1f);
        if (life <= 0f)
            return;

        int count = rank >= 5 ? 24 : rank >= 4 ? 18 : rank >= 2 ? 13 : 7;
        float radius = 15f + local * (rank >= 4 ? 110f : rank >= 2 ? 82f : 55f);
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.PI * 2f / count + local * 2f;
            int px = r.Center.X + (int)(MathF.Cos(a) * radius);
            int py = r.Center.Y + (int)(MathF.Sin(a) * radius * 0.65f);
            int size = rank >= 4 ? 7 : rank >= 2 ? 5 : 4;
            Color particle = rank >= 5 ? BalancedUi036.RainbowColor(i, t) : color;
            b.Draw(Game1.fadeToBlackRect, new Rectangle(px - size / 2, py - size / 2, size, size), particle * (0.20f + life * 0.70f));
        }
    }

    private void DrawCompletion(SpriteBatch b)
    {
        if (CompletionFx < 0f || CompletionFx > 2.0f)
            return;
        float life = Math.Clamp(1f - CompletionFx / 2f, 0f, 1f);
        for (int i = 0; i < 30; i++)
        {
            float seed = i * 0.618f;
            int x = xPositionOnScreen + 30 + (int)((seed % 1f) * (width - 60));
            int y = yPositionOnScreen + 100 + (int)((CompletionFx * 120f + i * 23) % Math.Max(120, height - 180));
            Color c = BalancedUi036.RainbowColor(i, CompletionFx);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, 5, 9), c * (0.35f + life * 0.55f));
        }
        if (CompletionFx < 1.2f)
            CardUi.CenterText(b, Game1.dialogueFont, "팩 오픈 완료!", new Rectangle(xPositionOnScreen + width / 2 - 180, yPositionOnScreen + 405, 360, 45), BalancedUi036.TestGold, 0.92f);
    }
}

internal sealed class BalancedCollectionMenu036 : IClickableMenu
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

    internal BalancedCollectionMenu036(ModEntry mod, IClickableMenu returnMenu, int targetSlot = -1)
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
        int filterY = r.Y + 100;
        for (int i = 0; i < names.Length; i++)
            Filters[names[i]] = new Rectangle(r.X + 18 + i * (fw + gap), filterY, fw, 36);

        int controlsY = r.Bottom - 46;
        Prev = new Rectangle(r.X + 18, controlsY, 82, 34);
        Next = new Rectangle(r.X + 106, controlsY, 82, 34);
        Bonus = new Rectangle(r.X + 196, controlsY, 150, 34);
        Shelf = new Rectangle(r.Right - 190, controlsY, 82, 34);
        Back = new Rectangle(r.Right - 102, controlsY, 84, 34);

        int detailW = 220;
        int detailX = r.Right - detailW - 18;
        List = new Rectangle(detailX + 10, r.Y + 493, detailW - 20, 40);
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
            Game1.activeClickableMenu = new BalancedShelfMenu036(Mod, this);
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        BalancedUi036.Begin(b, this, "컬렉션", $"수집 {Mod.Core.UniqueCardCount()}/{Mod.Cards.Count}");

        foreach (var pair in Filters)
        {
            int count = pair.Key == "All" ? Mod.Core.UniqueCardCount() : Mod.Core.UniqueCountForRarity(pair.Key);
            BalancedUi036.MiniButton(b, pair.Value, $"{FilterName(pair.Key)} {count}", true,
                string.Equals(Filter, pair.Key, StringComparison.OrdinalIgnoreCase));
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 8);
        Page = Math.Clamp(Page, 0, maxPage);
        int start = Page * 8;
        Hits.Clear();

        int detailW = 220;
        int detailX = xPositionOnScreen + width - detailW - 18;
        int gridLeft = xPositionOnScreen + 18;
        int gridRight = detailX - 10;
        int gridTop = yPositionOnScreen + 146;
        int gapX = 7;
        int gapY = 10;
        int cardW = (gridRight - gridLeft - gapX * 3) / 4;
        int cardH = 184;

        for (int i = 0; i < 8 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            int col = i % 4;
            int rr = i / 4;
            Rectangle card = new(gridLeft + col * (cardW + gapX), gridTop + rr * (cardH + gapY), cardW, cardH);
            CardUi.Panel(b, card, row.CollectionKey == SelectedKey);

            Rectangle band = new(card.X + 6, card.Y + 6, card.Width - 12, 25);
            b.Draw(Game1.fadeToBlackRect, band, CardUi.RarityColor(row.Card.Rarity));
            CardUi.CenterText(b, Game1.smallFont, ModEntry.RarityName(row.Card.Rarity), band, Color.White, 1.00f);
            CardUi.CenterText(b, Game1.dialogueFont, row.Card.Name,
                new Rectangle(card.X + 7, card.Y + 43, card.Width - 14, 72), CardUi.Ink, 0.76f);
            CardUi.CenterText(b, Game1.smallFont, $"보유 {row.Count}장",
                new Rectangle(card.X + 7, card.Bottom - 42, card.Width - 14, 30), CardUi.Ink, 1.04f);
            Hits.Add((row.CollectionKey, card));
        }

        Rectangle detail = new(detailX, gridTop, detailW, 378);
        CardUi.Panel(b, detail, true);
        var selected = rows.FirstOrDefault(p => p.CollectionKey == SelectedKey);
        if (selected.Card is not null)
        {
            CardUi.CenterText(b, Game1.dialogueFont, selected.Card.Name,
                new Rectangle(detail.X + 10, detail.Y + 12, detail.Width - 20, 54), CardUi.Ink, 0.84f);
            CardUi.CenterText(b, Game1.smallFont, ModEntry.RarityName(selected.Card.Rarity),
                new Rectangle(detail.X + 10, detail.Y + 72, detail.Width - 20, 28), CardUi.RarityColor(selected.Card.Rarity), 1.10f);
            CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(selected.Variant)} · {selected.Condition}",
                new Rectangle(detail.X + 10, detail.Y + 105, detail.Width - 20, 28), CardUi.Ink, 1.04f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{selected.Value:N0}G",
                new Rectangle(detail.X + 10, detail.Y + 145, detail.Width - 20, 42), CardUi.GreenDark, 0.80f);

            int available = CardShopRules.GetListableCount(Mod, SelectedKey);
            CardUi.CenterText(b, Game1.smallFont, $"보유 {selected.Count}장 · 판매 가능 {available}장",
                new Rectangle(detail.X + 10, detail.Y + 205, detail.Width - 20, 34), CardUi.Muted, 1.02f);
            BalancedUi036.Button(b, List, TargetSlot >= 0 ? $"{TargetSlot + 1}번에 진열" : "판매 진열", available > 0, true);
        }
        else
        {
            CardUi.CenterText(b, Game1.dialogueFont, "카드를 선택하세요",
                new Rectangle(detail.X + 12, detail.Y + 90, detail.Width - 24, 70), CardUi.Muted, 0.80f);
        }

        var bonus = Mod.Core.GetNextCollectionBonus();
        BalancedUi036.MiniButton(b, Prev, "이전", Page > 0);
        BalancedUi036.MiniButton(b, Next, "다음", Page < maxPage);
        BalancedUi036.MiniButton(b, Bonus, bonus.Complete ? "보너스 완료" : $"{bonus.Required}종 → 팩 {bonus.Reward}", !bonus.Complete, bonus.CanClaim);
        BalancedUi036.MiniButton(b, Shelf, "판매대");
        BalancedUi036.MiniButton(b, Back, "뒤로");

        CardUi.CenterText(b, Game1.smallFont, $"{Page + 1}/{maxPage + 1} · {Message}",
            new Rectangle(Bonus.Right + 8, Prev.Y, Math.Max(90, Shelf.X - Bonus.Right - 16), 34), CardUi.Muted, 1.00f);
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

internal sealed class BalancedShelfMenu036 : IClickableMenu
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

    internal BalancedShelfMenu036(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(960, 610);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int top = r.Y + 104;
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
            Game1.activeClickableMenu = new BalancedCollectionMenu036(Mod, this, SelectedSlot);
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
        BalancedUi036.Begin(b, this, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots} · 하루 최대 {Mod.Config.MaxDailySales}장");
        IReadOnlyList<SaleListing?> shelf = Mod.Core.GetShelfSlots();

        for (int i = 0; i < Slots.Count; i++)
        {
            Rectangle r = Slots[i];
            CardUi.Panel(b, r, i == SelectedSlot);
            SaleListing? listing = i < shelf.Count ? shelf[i] : null;
            if (listing is null)
            {
                CardUi.CenterText(b, Game1.dialogueFont, "+", new Rectangle(r.X + 8, r.Y + 24, r.Width - 16, 48), CardUi.Muted, 0.86f);
                CardUi.CenterText(b, Game1.smallFont, $"{i + 1}번", new Rectangle(r.X + 8, r.Y + 92, r.Width - 16, 34), CardUi.Muted, 1.08f);
                continue;
            }

            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out string condition))
                continue;
            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null)
                continue;

            CardUi.CenterText(b, Game1.dialogueFont, card.Name,
                new Rectangle(r.X + 10, r.Y + 20, r.Width - 20, 60), CardUi.Ink, 0.82f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{listing.Price:N0}G",
                new Rectangle(r.X + 10, r.Y + 92, r.Width - 20, 42), CardUi.GreenDark, 0.78f);
            CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(variant)} · {condition}",
                new Rectangle(r.X + 10, r.Bottom - 34, r.Width - 20, 26), CardUi.Muted, 0.98f);
        }

        SaleListing? selected = Mod.Core.GetListingAtSlot(SelectedSlot);
        string infoText = selected is null ? $"{SelectedSlot + 1}번 · 빈 슬롯" : $"{SelectedSlot + 1}번";
        if (selected is not null && CardKeys.TryParse(selected.CollectionKey, out string key, out string selectedVariant, out string selectedCondition))
        {
            CardDefinition? card = Mod.FindCard(key);
            if (card is not null)
                infoText = $"{card.Name} · {ModEntry.VariantName(selectedVariant)} · {selectedCondition} · 판매확률 {Mod.Core.GetSaleChance(selected) * 100:0}%";
        }
        if (!string.Equals(Message, "슬롯을 선택하세요", StringComparison.Ordinal))
            infoText = Message;

        Rectangle info = new(xPositionOnScreen + 20, yPositionOnScreen + 477, width - 40, 34);
        CardUi.CenterText(b, Game1.smallFont, infoText, info, CardUi.Ink, 1.06f);

        BalancedUi036.MiniButton(b, Add, selected is null ? "카드 넣기" : "사용 중", selected is null);
        BalancedUi036.MiniButton(b, Down, "가격 -50", selected is not null);
        BalancedUi036.MiniButton(b, Up, "가격 +50", selected is not null);
        BalancedUi036.MiniButton(b, Remove, "회수", selected is not null);
        BalancedUi036.MiniButton(b, Back, "뒤로");
        drawMouse(b);
    }
}
