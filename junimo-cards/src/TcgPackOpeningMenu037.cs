using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

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
                    ? TcgUi037.RainbowColor(i, t)
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
                ? TcgUi037.RainbowColor((int)(t * 10f), t)
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
            Color particle = rank >= 5 ? TcgUi037.RainbowColor(i, t) : color;
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
            Color c = TcgUi037.RainbowColor(i, CompletionFx);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, 5, 9), c * (0.38f + life * 0.55f));
        }

        if (CompletionFx < 1.2f)
            CardUi.CenterText(b, Game1.dialogueFont, "팩 오픈 완료!",
                new Rectangle(xPositionOnScreen + width / 2 - 180, yPositionOnScreen + 415, 360, 45),
                TcgUi037.TestGold, 0.96f);
    }
}
