using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

// v0.3.3 readability + reveal-FX pass.
// Keep the compact wording from v0.3.2, make the overall UI a little larger,
// and add a real card-flip / bounce / glow / sparkle reveal sequence.
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
        Rectangle r = CardUi.Center(1180, 700);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int tileW = (r.Width - 200) / 3;
        int tileY = r.Y + 260;
        Pack = new Rectangle(r.X + 70, tileY, tileW, 170);
        Collection = new Rectangle(Pack.Right + 30, tileY, tileW, 170);
        Shelf = new Rectangle(Collection.Right + 30, tileY, tileW, 170);
        Close = new Rectangle(r.Right - 180, r.Bottom - 68, 125, 46);
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
        CardUi.Begin(b, this, "주니모 카드샵", "팩 · 컬렉션 · 판매");

        int unique = Mod.Core.UniqueCardCount();
        Rectangle stats = new(xPositionOnScreen + 70, yPositionOnScreen + 138, width - 140, 100);
        CardUi.Panel(b, stats);
        int cell = stats.Width / 4;
        DrawStat(b, new Rectangle(stats.X, stats.Y, cell, stats.Height), "골드", $"{Game1.player.Money:N0}G");
        DrawStat(b, new Rectangle(stats.X + cell, stats.Y, cell, stats.Height), "미개봉", $"{Mod.State.UnopenedPacks}팩");
        DrawStat(b, new Rectangle(stats.X + cell * 2, stats.Y, cell, stats.Height), "컬렉션", $"{unique}/{Mod.Cards.Count}");
        DrawStat(b, new Rectangle(stats.X + cell * 3, stats.Y, stats.Width - cell * 3, stats.Height), "매출", $"{Mod.State.LifetimeCardRevenue:N0}G");

        DrawHomeTile(b, Pack, "팩 구매", $"1팩 {Mod.Config.PackPrice:N0}G");
        DrawHomeTile(b, Collection, "컬렉션", $"수집 {unique}/{Mod.Cards.Count}");
        DrawHomeTile(b, Shelf, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots}");

        Rectangle today = new(xPositionOnScreen + 70, yPositionOnScreen + 462, width - 140, 94);
        CardUi.Panel(b, today);
        CardUi.Heading(b, "오늘", new Vector2(today.X + 22, today.Y + 20), CardUi.Ink, 0.88f);
        string summary = $"손님 {Mod.State.LastCustomerCount}명   판매 {Mod.State.LastCardsSold}장   +{Mod.State.LastDailyRevenue:N0}G";
        CardUi.CenterText(b, Game1.dialogueFont, summary, new Rectangle(today.X + 120, today.Y + 10, today.Width - 145, 68), CardUi.GreenDark, 0.86f);

        CardUi.Button(b, Close, "닫기");
        drawMouse(b);
    }

    private static void DrawStat(SpriteBatch b, Rectangle r, string label, string value)
    {
        CardUi.CenterText(b, Game1.smallFont, label, new Rectangle(r.X + 4, r.Y + 10, r.Width - 8, 28), CardUi.Muted, 1.16f);
        CardUi.CenterText(b, Game1.dialogueFont, value, new Rectangle(r.X + 4, r.Y + 39, r.Width - 8, 50), CardUi.Ink, 0.84f);
    }

    private static void DrawHomeTile(SpriteBatch b, Rectangle r, string title, string value)
    {
        CardUi.Panel(b, r);
        CardUi.CenterText(b, Game1.dialogueFont, title, new Rectangle(r.X + 14, r.Y + 25, r.Width - 28, 58), CardUi.GreenDark, 0.94f);
        CardUi.CenterText(b, Game1.smallFont, value, new Rectangle(r.X + 14, r.Y + 104, r.Width - 28, 38), CardUi.Ink, 1.28f);
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
        Rectangle r = CardUi.Center(1060, 650);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int buttonW = 245;
        int buttonY = r.Y + 410;
        BuyOne = new Rectangle(r.X + 85, buttonY, buttonW, 66);
        BuyFive = new Rectangle(r.X + (r.Width - buttonW) / 2, buttonY, buttonW, 66);
        Open = new Rectangle(r.Right - 85 - buttonW, buttonY, buttonW, 66);
        Back = new Rectangle(r.Right - 170, r.Bottom - 65, 120, 44);
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
        CardUi.Begin(b, this, "팩 구매", "Pelican Origins · 5장");

        Rectangle card = new(xPositionOnScreen + 105, yPositionOnScreen + 150, 190, 235);
        CardUi.DrawCardBack(b, card);

        Rectangle info = new(xPositionOnScreen + 345, yPositionOnScreen + 150, width - 450, 235);
        CardUi.Panel(b, info);
        CardUi.CenterText(b, Game1.dialogueFont, $"보유 {Mod.State.UnopenedPacks}팩", new Rectangle(info.X + 20, info.Y + 20, info.Width - 40, 58), CardUi.Ink, 1.0f);
        CardUi.CenterText(b, Game1.smallFont, "5번째 · 언커먼 이상", new Rectangle(info.X + 20, info.Y + 98, info.Width - 40, 40), CardUi.GreenDark, 1.28f);
        CardUi.CenterText(b, Game1.smallFont, $"Rare+ 천장 · {Mod.State.PacksSinceRare}/10", new Rectangle(info.X + 20, info.Y + 148, info.Width - 40, 40), CardUi.GreenDark, 1.28f);

        CardUi.Button(b, BuyOne, $"1팩  {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        CardUi.Button(b, BuyFive, $"5팩  {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        CardUi.Button(b, Open, $"개봉  {Mod.State.UnopenedPacks}팩", Mod.State.UnopenedPacks > 0, true);

        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 95, yPositionOnScreen + 505, width - 190, 44), CardUi.Muted, 1.18f);
        CardUi.Button(b, Back, "뒤로");
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
    private readonly Rectangle Done;
    private readonly string OpeningMessage;
    private bool CompletionCelebrated;

    internal ReadablePackOpeningMenu032(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(1280, 700);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        if (!Mod.TryOpenPack(out Pulls, out OpeningMessage))
            Pulls = new List<CardPull>();

        Revealed = new bool[Pulls.Count];
        RevealFx = Enumerable.Repeat(-1f, Pulls.Count).ToArray();

        int gap = 14;
        int available = r.Width - 110 - gap * 4;
        int cardW = Math.Min(210, available / 5);
        int total = cardW * 5 + gap * 4;
        int startX = r.X + (r.Width - total) / 2;
        int cardH = Math.Min(330, r.Height - 300);
        for (int i = 0; i < 5; i++)
            Cards.Add(new Rectangle(startX + i * (cardW + gap), r.Y + 145, cardW, cardH));

        Done = new Rectangle(r.X + r.Width / 2 - 165, r.Bottom - 72, 330, 52);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pulls.Count == 0)
        {
            Game1.activeClickableMenu = ReturnMenu;
            return;
        }

        for (int i = 0; i < Pulls.Count && i < Cards.Count; i++)
        {
            if (!Revealed[i] && Cards[i].Contains(x, y))
            {
                Revealed[i] = true;
                RevealFx[i] = 0f;

                CardDefinition? def = Mod.FindCard(Pulls[i].CardKey);
                int rank = def is null ? 0 : ModEntry.GetRarityRank(def.Rarity);

                // "cardboardBox" isn't a valid Stardew cue. Use stable vanilla cues instead.
                if (rank >= 4)
                    Game1.playSound("reward");
                else if (rank >= 2)
                    Game1.playSound("newArtifact");
                else
                    Game1.playSound("coin");
                return;
            }
        }

        if (Revealed.Length > 0 && Revealed.All(p => p) && Done.Contains(x, y))
            Game1.activeClickableMenu = new ReadableCollectionMenu032(Mod, ReturnMenu);
    }

    public override void update(GameTime time)
    {
        base.update(time);
        float dt = Math.Min(0.05f, (float)time.ElapsedGameTime.TotalSeconds);
        for (int i = 0; i < RevealFx.Length; i++)
        {
            if (RevealFx[i] >= 0f && RevealFx[i] < 1.4f)
                RevealFx[i] += dt;
        }

        if (!CompletionCelebrated && Revealed.Length > 0 && Revealed.All(p => p) && RevealFx.All(p => p >= 0.32f))
        {
            CompletionCelebrated = true;
            Game1.playSound("reward");
        }
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "팩 개봉", OpeningMessage.Contains("천장") ? OpeningMessage : "카드를 눌러 공개하세요");

        DrawScreenFlash(b);

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
                CardUi.DrawCardBack(b, baseRect);
                CardUi.CenterText(b, Game1.smallFont, "클릭", new Rectangle(baseRect.X, baseRect.Bottom + 8, baseRect.Width, 30), CardUi.GreenDark, 1.18f);
                continue;
            }

            CardPull pull = Pulls[i];
            CardDefinition? def = Mod.FindCard(pull.CardKey);
            if (def is null)
                continue;

            float t = Math.Max(0f, RevealFx[i]);
            int rank = ModEntry.GetRarityRank(def.Rarity);
            DrawAnimatedCard(b, baseRect, def, pull, rank, t);
        }

        int opened = Revealed.Count(p => p);
        CardUi.CenterText(b, Game1.dialogueFont, $"{opened}/5", new Rectangle(xPositionOnScreen + width / 2 - 80, yPositionOnScreen + height - 135, 160, 48), CardUi.Ink, 0.94f);
        if (Revealed.Length > 0 && Revealed.All(p => p))
            CardUi.Button(b, Done, "컬렉션 확인", true, true);
        drawMouse(b);
    }

    private void DrawScreenFlash(SpriteBatch b)
    {
        float best = 0f;
        Color color = Color.Transparent;
        for (int i = 0; i < RevealFx.Length; i++)
        {
            float t = RevealFx[i];
            if (t < 0f || t > 0.32f || i >= Pulls.Count)
                continue;
            CardDefinition? def = Mod.FindCard(Pulls[i].CardKey);
            if (def is null)
                continue;
            int rank = ModEntry.GetRarityRank(def.Rarity);
            if (rank < 2)
                continue;
            float strength = (1f - t / 0.32f) * (rank >= 4 ? 0.22f : 0.12f);
            if (strength > best)
            {
                best = strength;
                color = CardUi.RarityColor(def.Rarity);
            }
        }

        if (best > 0f)
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), color * best);
    }

    private void DrawAnimatedCard(SpriteBatch b, Rectangle baseRect, CardDefinition def, CardPull pull, int rank, float t)
    {
        Color accent = rank == 0 ? CardUi.Gold : CardUi.RarityColor(def.Rarity);
        Rectangle drawRect = baseRect;

        if (t < 0.18f)
        {
            float p = SmoothStep(t / 0.18f);
            float widthScale = 1f - p * 0.93f;
            drawRect = ScaleAroundCenter(baseRect, widthScale, 1.02f);
            CardUi.DrawCardBack(b, drawRect);
            return;
        }

        float revealP = Math.Clamp((t - 0.18f) / 0.30f, 0f, 1f);
        float width = 0.07f + 0.93f * EaseOutBack(revealP);
        float bounce = t < 0.70f ? 1f + MathF.Sin(Math.Clamp((t - 0.18f) / 0.52f, 0f, 1f) * MathF.PI) * 0.08f : 1f;
        float lift = t < 0.72f ? MathF.Sin(Math.Clamp((t - 0.18f) / 0.54f, 0f, 1f) * MathF.PI) * 16f : 0f;
        float shake = rank >= 2 && t < 0.55f ? MathF.Sin(t * 52f) * (rank >= 4 ? 4f : 2f) : 0f;

        drawRect = ScaleAroundCenter(baseRect, Math.Max(0.06f, width) * bounce, bounce);
        drawRect.Offset((int)Math.Round(shake), -(int)Math.Round(lift));

        if (t < 0.95f)
            DrawBurst(b, baseRect, accent, rank, t);

        CardUi.DrawCardFront(b, drawRect, def, pull, rank >= 2);

        if (t < 1.05f)
        {
            float glow = Math.Max(0f, 1f - t / 1.05f);
            int pad = 4 + (int)(glow * (rank >= 4 ? 18 : 10));
            CardUi.Border(b, new Rectangle(drawRect.X - pad, drawRect.Y - pad, drawRect.Width + pad * 2, drawRect.Height + pad * 2), accent * (0.25f + glow * 0.60f), rank >= 4 ? 5 : 3);
        }

        if (t < 0.95f)
        {
            string rarity = ModEntry.RarityName(def.Rarity);
            string banner = rank >= 4 ? $"★ {rarity}! ★" : rank >= 2 ? $"{rarity}!" : rarity;
            float labelScale = 0.92f + MathF.Sin(Math.Clamp(t / 0.80f, 0f, 1f) * MathF.PI) * 0.18f;
            CardUi.CenterText(b, Game1.dialogueFont, banner, new Rectangle(baseRect.X - 10, baseRect.Y - 42, baseRect.Width + 20, 38), accent, labelScale);
        }
    }

    private static void DrawBurst(SpriteBatch b, Rectangle r, Color color, int rank, float t)
    {
        float life = Math.Max(0f, 1f - t / 0.95f);
        if (life <= 0f)
            return;

        int count = rank >= 4 ? 14 : rank >= 2 ? 10 : 6;
        float radius = 25f + t * (rank >= 4 ? 120f : 82f);
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.PI * 2f / count + t * 1.8f;
            int px = r.Center.X + (int)(MathF.Cos(a) * radius);
            int py = r.Center.Y + (int)(MathF.Sin(a) * radius * 0.65f);
            int size = rank >= 4 ? 7 : 5;
            b.Draw(Game1.fadeToBlackRect, new Rectangle(px - size / 2, py - size / 2, size, size), color * (0.25f + life * 0.65f));
        }
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
        Rectangle r = CardUi.Center(1280, 720);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        string[] names = { "All", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Secret" };
        int gap = 8;
        int fw = (r.Width - 90 - gap * 6) / 7;
        int start = r.X + 45;
        for (int i = 0; i < names.Length; i++)
            Filters[names[i]] = new Rectangle(start + i * (fw + gap), r.Y + 124, fw, 48);

        Prev = new Rectangle(r.X + 235, r.Bottom - 58, 115, 44);
        Next = new Rectangle(r.X + 620, r.Bottom - 58, 115, 44);
        List = new Rectangle(r.Right - 350, r.Y + 470, 300, 54);
        Bonus = new Rectangle(r.Right - 350, r.Y + 532, 300, 54);
        Shelf = new Rectangle(r.Right - 350, r.Y + 594, 175, 44);
        Back = new Rectangle(r.Right - 165, r.Y + 594, 115, 44);
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
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
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
        CardUi.Begin(b, this, "컬렉션", $"수집 {Mod.Core.UniqueCardCount()}/{Mod.Cards.Count}");

        foreach (var pair in Filters)
        {
            int count = pair.Key == "All" ? Mod.Core.UniqueCardCount() : Mod.Core.UniqueCountForRarity(pair.Key);
            CardUi.Button(b, pair.Value, $"{FilterName(pair.Key)} {count}", true, string.Equals(Filter, pair.Key, StringComparison.OrdinalIgnoreCase));
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        Page = Math.Clamp(Page, 0, maxPage);
        int start = Page * 6;
        Hits.Clear();

        int detailW = 360;
        int gridLeft = xPositionOnScreen + 48;
        int gridTop = yPositionOnScreen + 190;
        int gridRight = xPositionOnScreen + width - detailW - 80;
        int gapX = 14;
        int cardW = (gridRight - gridLeft - gapX * 2) / 3;
        int cardH = 182;
        for (int i = 0; i < 6 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            int col = i % 3;
            int rr = i / 3;
            Rectangle card = new(gridLeft + col * (cardW + gapX), gridTop + rr * 194, cardW, cardH);
            CardUi.Panel(b, card, row.CollectionKey == SelectedKey);
            Rectangle band = new(card.X + 8, card.Y + 8, card.Width - 16, 34);
            b.Draw(Game1.fadeToBlackRect, band, CardUi.RarityColor(row.Card.Rarity));
            CardUi.CenterText(b, Game1.smallFont, ModEntry.RarityName(row.Card.Rarity), band, Color.White, 1.12f);
            CardUi.CenterText(b, Game1.dialogueFont, row.Card.Name, new Rectangle(card.X + 12, card.Y + 54, card.Width - 24, 56), CardUi.Ink, 0.86f);
            CardUi.CenterText(b, Game1.smallFont, $"보유 {row.Count}장", new Rectangle(card.X + 12, card.Y + 122, card.Width - 24, 34), CardUi.Ink, 1.20f);
            Hits.Add((row.CollectionKey, card));
        }

        Rectangle detail = new(xPositionOnScreen + width - detailW - 48, yPositionOnScreen + 190, detailW, 255);
        CardUi.Panel(b, detail, true);
        var selected = rows.FirstOrDefault(p => p.CollectionKey == SelectedKey);
        if (selected.Card is not null)
        {
            CardUi.CenterText(b, Game1.dialogueFont, selected.Card.Name, new Rectangle(detail.X + 20, detail.Y + 18, detail.Width - 40, 54), CardUi.Ink, 0.98f);
            CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(selected.Variant)} · {selected.Condition}", new Rectangle(detail.X + 20, detail.Y + 84, detail.Width - 40, 36), CardUi.Ink, 1.22f);
            CardUi.CenterText(b, Game1.smallFont, $"보유 {selected.Count}장   진열 {Mod.GetListedCount(selected.CollectionKey)}장", new Rectangle(detail.X + 20, detail.Y + 132, detail.Width - 40, 36), CardUi.Muted, 1.16f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{selected.Value:N0}G", new Rectangle(detail.X + 20, detail.Y + 184, detail.Width - 40, 50), CardUi.GreenDark, 0.92f);
        }
        else
        {
            CardUi.CenterText(b, Game1.dialogueFont, "카드를 선택하세요", new Rectangle(detail.X + 20, detail.Y + 80, detail.Width - 40, 80), CardUi.Muted, 0.88f);
        }

        int available = string.IsNullOrWhiteSpace(SelectedKey) ? 0 : CardShopRules.GetListableCount(Mod, SelectedKey);
        CardUi.Button(b, List, TargetSlot >= 0 ? $"{TargetSlot + 1}번에 진열" : $"판매 진열  {available}장 가능", available > 0, true);
        var bonus = Mod.Core.GetNextCollectionBonus();
        CardUi.Button(b, Bonus, bonus.Complete ? "보너스 완료" : $"보너스 {bonus.Required}종 → 팩 {bonus.Reward}", !bonus.Complete, bonus.CanClaim);
        CardUi.Button(b, Shelf, "판매대");
        CardUi.Button(b, Back, "뒤로");
        CardUi.Button(b, Prev, "이전", Page > 0);
        CardUi.Button(b, Next, "다음", Page < maxPage);
        CardUi.CenterText(b, Game1.smallFont, $"{Page + 1}/{maxPage + 1}", new Rectangle(xPositionOnScreen + 435, yPositionOnScreen + height - 58, 130, 44), CardUi.Muted, 1.20f);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + width - detailW - 48, yPositionOnScreen + height - 58, detailW, 44), CardUi.Muted, 1.05f);
        drawMouse(b);
    }

    private static string FilterName(string filter) => filter switch
    {
        "All" => "전체",
        "Common" => "커먼",
        "Uncommon" => "언커먼",
        "Rare" => "레어",
        "Epic" => "에픽",
        "Legendary" => "레전더리",
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
        Rectangle r = CardUi.Center(1280, 720);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int gap = 14;
        int sx = r.X + 48;
        int sy = r.Y + 150;
        int slotW = (r.Width - 96 - gap * 3) / 4;
        int slotH = 170;
        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Slots.Add(new Rectangle(sx + col * (slotW + gap), sy + row * 188, slotW, slotH));
        }

        Add = new Rectangle(r.X + 58, r.Bottom - 58, 235, 46);
        Down = new Rectangle(r.X + 315, r.Bottom - 58, 150, 46);
        Up = new Rectangle(r.X + 480, r.Bottom - 58, 150, 46);
        Remove = new Rectangle(r.X + 645, r.Bottom - 58, 175, 46);
        Back = new Rectangle(r.Right - 165, r.Bottom - 58, 115, 46);
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
        CardUi.Begin(b, this, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots} · 하루 최대 {Mod.Config.MaxDailySales}장");

        IReadOnlyList<SaleListing?> shelf = Mod.Core.GetShelfSlots();
        for (int i = 0; i < Slots.Count; i++)
        {
            Rectangle r = Slots[i];
            CardUi.Panel(b, r, i == SelectedSlot);
            SaleListing? listing = i < shelf.Count ? shelf[i] : null;
            if (listing is null)
            {
                CardUi.CenterText(b, Game1.dialogueFont, "+", new Rectangle(r.X + 10, r.Y + 22, r.Width - 20, 62), CardUi.Muted, 1.08f);
                CardUi.CenterText(b, Game1.smallFont, $"{i + 1}번", new Rectangle(r.X + 10, r.Y + 106, r.Width - 20, 34), CardUi.Muted, 1.20f);
                continue;
            }

            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out _, out _))
                continue;
            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null)
                continue;

            CardUi.CenterText(b, Game1.dialogueFont, card.Name, new Rectangle(r.X + 12, r.Y + 22, r.Width - 24, 58), CardUi.Ink, 0.90f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{listing.Price:N0}G", new Rectangle(r.X + 12, r.Y + 94, r.Width - 24, 48), CardUi.GreenDark, 0.88f);
        }

        SaleListing? selected = Mod.Core.GetListingAtSlot(SelectedSlot);
        string selectedInfo = "빈 슬롯";
        if (selected is not null && CardKeys.TryParse(selected.CollectionKey, out string key, out string variant, out string condition))
        {
            CardDefinition? card = Mod.FindCard(key);
            if (card is not null)
                selectedInfo = $"{card.Name} · {ModEntry.VariantName(variant)} · {condition} · 판매확률 {Mod.Core.GetSaleChance(selected) * 100:0}%";
        }

        CardUi.CenterText(b, Game1.smallFont, selectedInfo, new Rectangle(xPositionOnScreen + 58, yPositionOnScreen + 535, width - 116, 42), CardUi.Ink, 1.18f);
        CardUi.Button(b, Add, selected is null ? $"{SelectedSlot + 1}번 카드 넣기" : "슬롯 사용 중", selected is null, true);
        CardUi.Button(b, Down, "가격 -50", selected is not null);
        CardUi.Button(b, Up, "가격 +50", selected is not null);
        CardUi.Button(b, Remove, "회수", selected is not null);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 840, yPositionOnScreen + height - 58, 220, 46), CardUi.Muted, 1.02f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }
}
