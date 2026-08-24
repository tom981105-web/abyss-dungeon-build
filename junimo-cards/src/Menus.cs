using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

internal static class CardUi
{
    internal static readonly Color Ink = new(67, 45, 31);
    internal static readonly Color Muted = new(111, 88, 65);
    internal static readonly Color Green = new(46, 104, 65);
    internal static readonly Color GreenDark = new(28, 69, 44);
    internal static readonly Color Cream = new(247, 229, 185);
    internal static readonly Color Gold = new(218, 157, 45);

    internal static Rectangle Center(int width, int height)
    {
        width = Math.Min(width, Game1.uiViewport.Width - 70);
        height = Math.Min(height, Game1.uiViewport.Height - 60);
        return new Rectangle(Math.Max(0, (Game1.uiViewport.Width - width) / 2), Math.Max(0, (Game1.uiViewport.Height - height) / 2), width, height);
    }

    internal static void Begin(SpriteBatch b, IClickableMenu menu, string title, string subtitle = "")
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.55f);
        IClickableMenu.drawTextureBox(b, menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height, Color.White);
        Rectangle header = new(menu.xPositionOnScreen + 28, menu.yPositionOnScreen + 24, menu.width - 56, 88);
        IClickableMenu.drawTextureBox(b, header.X, header.Y, header.Width, header.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(header.X + 8, header.Y + 8, header.Width - 16, header.Height - 16), GreenDark);
        CenterText(b, Game1.dialogueFont, title, new Rectangle(header.X + 12, header.Y + 6, header.Width - 24, 48), Color.White, 0.95f);
        if (!string.IsNullOrWhiteSpace(subtitle))
            CenterText(b, Game1.smallFont, subtitle, new Rectangle(header.X + 14, header.Y + 52, header.Width - 28, 25), new Color(232, 241, 223), 1.2f);
    }

    internal static void Panel(SpriteBatch b, Rectangle r, bool selected = false)
    {
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14), (selected ? new Color(255, 242, 196) : Cream) * 0.45f);
        if (selected) Border(b, r, Gold, 3);
    }

    internal static void Button(SpriteBatch b, Rectangle r, string text, bool enabled = true, bool green = false)
    {
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
        Color fill = !enabled ? new Color(135, 128, 112) : green ? Green : new Color(172, 118, 50);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14), fill * 0.86f);
        CenterText(b, Game1.dialogueFont, text, new Rectangle(r.X + 8, r.Y + 7, r.Width - 16, r.Height - 14), enabled ? Color.White : new Color(223, 219, 205), 0.72f);
    }

    internal static void Border(SpriteBatch b, Rectangle r, Color color, int t = 2)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Y, r.Width, t), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Bottom - t, r.Width, t), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Y, t, r.Height), color);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.Right - t, r.Y, t, r.Height), color);
    }

    internal static void Text(SpriteBatch b, string text, Vector2 pos, Color color, float scale = 1.15f)
        => b.DrawString(Game1.smallFont, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);

    internal static void Heading(SpriteBatch b, string text, Vector2 pos, Color color, float scale = 0.88f)
        => b.DrawString(Game1.dialogueFont, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);

    internal static void CenterText(SpriteBatch b, SpriteFont font, string text, Rectangle r, Color color, float maxScale = 1f)
    {
        Vector2 size = font.MeasureString(text);
        float scale = Math.Min(maxScale, Math.Min((r.Width - 8) / Math.Max(1f, size.X), (r.Height - 4) / Math.Max(1f, size.Y)));
        Vector2 pos = new(r.Center.X - size.X * scale / 2f, r.Center.Y - size.Y * scale / 2f);
        b.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
    }

    internal static Color RarityColor(string rarity) => rarity switch
    {
        "Uncommon" => new Color(61, 139, 79),
        "Rare" => new Color(54, 111, 184),
        "Epic" => new Color(135, 77, 177),
        "Legendary" => new Color(220, 142, 31),
        "Secret" => new Color(199, 57, 92),
        _ => new Color(116, 107, 89)
    };

    internal static void DrawCardBack(SpriteBatch b, Rectangle r)
    {
        b.Draw(Game1.fadeToBlackRect, r, new Color(32, 73, 48));
        Border(b, r, new Color(224, 177, 76), 5);
        Rectangle inner = new(r.X + 13, r.Y + 13, r.Width - 26, r.Height - 26);
        Border(b, inner, new Color(247, 224, 158), 2);
        CenterText(b, Game1.dialogueFont, "?", new Rectangle(r.X + 20, r.Y + 42, r.Width - 40, r.Height - 90), new Color(246, 222, 145), 1.45f);
        CenterText(b, Game1.smallFont, "JUNIMO CARDS", new Rectangle(r.X + 10, r.Bottom - 52, r.Width - 20, 28), Color.White, 0.9f);
    }

    internal static void DrawCardFront(SpriteBatch b, Rectangle r, CardDefinition card, CardPull pull, bool glow = false)
    {
        Color rarity = RarityColor(card.Rarity);
        b.Draw(Game1.fadeToBlackRect, r, new Color(250, 235, 198));
        Border(b, r, glow ? rarity : new Color(96, 70, 43), glow ? 6 : 3);
        Rectangle top = new(r.X + 8, r.Y + 8, r.Width - 16, 32);
        b.Draw(Game1.fadeToBlackRect, top, rarity);
        CenterText(b, Game1.smallFont, card.SetNo, top, Color.White, 0.9f);

        Rectangle emblem = new(r.X + 18, r.Y + 53, r.Width - 36, 88);
        b.Draw(Game1.fadeToBlackRect, emblem, rarity * 0.28f);
        string initial = string.IsNullOrWhiteSpace(card.Name) ? "?" : card.Name[..1];
        CenterText(b, Game1.dialogueFont, initial, emblem, rarity, 1.2f);

        CenterText(b, Game1.dialogueFont, card.Name, new Rectangle(r.X + 10, r.Y + 150, r.Width - 20, 38), Ink, 0.72f);
        CenterText(b, Game1.smallFont, ModEntry.RarityName(card.Rarity), new Rectangle(r.X + 10, r.Y + 190, r.Width - 20, 25), rarity, 1.05f);
        CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(pull.Variant)} · {pull.Condition}", new Rectangle(r.X + 8, r.Y + 217, r.Width - 16, 25), Ink, 0.88f);
        CenterText(b, Game1.smallFont, $"{pull.MarketValue:N0}G", new Rectangle(r.X + 8, r.Bottom - 35, r.Width - 16, 25), Green, 1.0f);
    }
}

internal sealed class CardShopHomeMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly Rectangle PackButton;
    private readonly Rectangle CollectionButton;
    private readonly Rectangle ShelfButton;
    private readonly Rectangle CloseButton;

    internal CardShopHomeMenu(ModEntry mod)
    {
        Mod = mod;
        Rectangle r = CardUi.Center(980, 660);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        int cardW = (width - 160) / 3;
        int y = yPositionOnScreen + 205;
        PackButton = new Rectangle(xPositionOnScreen + 56, y, cardW, 210);
        CollectionButton = new Rectangle(PackButton.Right + 24, y, cardW, 210);
        ShelfButton = new Rectangle(CollectionButton.Right + 24, y, cardW, 210);
        CloseButton = new Rectangle(xPositionOnScreen + width - 176, yPositionOnScreen + height - 72, 120, 44);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (PackButton.Contains(x, y)) { Game1.activeClickableMenu = new PackShopMenu(Mod, this); Game1.playSound("bigSelect"); return; }
        if (CollectionButton.Contains(x, y)) { Game1.activeClickableMenu = new CardCollectionMenu(Mod, this); Game1.playSound("bigSelect"); return; }
        if (ShelfButton.Contains(x, y)) { Game1.activeClickableMenu = new SaleShelfMenu(Mod, this); Game1.playSound("bigSelect"); return; }
        if (CloseButton.Contains(x, y)) exitThisMenu();
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        int unique = Mod.State.Collection.Count(p => p.Value > 0);
        int copies = Mod.State.Collection.Values.Where(p => p > 0).Sum();
        CardUi.Begin(b, this, "JUNIMO CARDS", "팩을 까고 · 모으고 · 진열하고 · 손님에게 판매하는 작은 카드샵");

        Rectangle stats = new(xPositionOnScreen + 56, yPositionOnScreen + 132, width - 112, 52);
        CardUi.Panel(b, stats);
        CardUi.CenterText(b, Game1.smallFont, $"보유 골드 {Game1.player.Money:N0}G   ·   미개봉 팩 {Mod.State.UnopenedPacks}   ·   컬렉션 {unique}종 / {copies}장   ·   카드 매출 {Mod.State.LifetimeCardRevenue:N0}G", stats, CardUi.Ink, 1.14f);

        DrawHomeCard(b, PackButton, "카드팩", $"Pelican Origins\n1팩 {Mod.Config.PackPrice:N0}G\n보유 {Mod.State.UnopenedPacks}팩", CardUi.Green);
        DrawHomeCard(b, CollectionButton, "컬렉션", $"수집한 카드 확인\n변형·상태별 보관\n여분 카드 진열", new Color(65, 104, 154));
        DrawHomeCard(b, ShelfButton, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots}\n하루 최대 {Mod.Config.MaxDailySales}장 판매\n손님은 아침에 방문", new Color(149, 91, 58));

        Rectangle summary = new(xPositionOnScreen + 56, yPositionOnScreen + 442, width - 112, 98);
        CardUi.Panel(b, summary);
        CardUi.Heading(b, "오늘의 카드샵", new Vector2(summary.X + 20, summary.Y + 14), CardUi.Ink, 0.70f);
        CardUi.Text(b, Mod.State.LastDailySalesSummary, new Vector2(summary.X + 22, summary.Y + 57), CardUi.Muted, 1.12f);
        CardUi.Text(b, $"Rare 천장까지 {Math.Max(0, 10 - Mod.State.PacksSinceRare)}팩 · 누적 개봉 {Mod.State.PacksOpened}팩", new Vector2(xPositionOnScreen + 62, yPositionOnScreen + height - 63), CardUi.Muted, 1.02f);
        CardUi.Button(b, CloseButton, "닫기");
        drawMouse(b);
    }

    private static void DrawHomeCard(SpriteBatch b, Rectangle r, string title, string body, Color accent)
    {
        CardUi.Panel(b, r);
        Rectangle band = new(r.X + 10, r.Y + 10, r.Width - 20, 48);
        b.Draw(Game1.fadeToBlackRect, band, accent);
        CardUi.CenterText(b, Game1.dialogueFont, title, band, Color.White, 0.80f);
        string[] lines = body.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            CardUi.CenterText(b, Game1.smallFont, lines[i], new Rectangle(r.X + 16, r.Y + 82 + i * 38, r.Width - 32, 30), i == 0 ? CardUi.Ink : CardUi.Muted, 1.12f);
    }
}

internal sealed class PackShopMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle BuyOne;
    private readonly Rectangle BuyFive;
    private readonly Rectangle OpenPack;
    private readonly Rectangle Back;
    private string Message = "팩 마지막 카드는 언커먼 이상 보장입니다.";

    internal PackShopMenu(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod; ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(940, 660);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        BuyOne = new Rectangle(xPositionOnScreen + 80, yPositionOnScreen + 485, 210, 58);
        BuyFive = new Rectangle(xPositionOnScreen + 310, yPositionOnScreen + 485, 210, 58);
        OpenPack = new Rectangle(xPositionOnScreen + 540, yPositionOnScreen + 485, 210, 58);
        Back = new Rectangle(xPositionOnScreen + width - 160, yPositionOnScreen + height - 64, 110, 42);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (BuyOne.Contains(x, y)) { Mod.TryBuyPacks(1, out Message); return; }
        if (BuyFive.Contains(x, y)) { Mod.TryBuyPacks(5, out Message); return; }
        if (OpenPack.Contains(x, y) && Mod.State.UnopenedPacks > 0) { Game1.activeClickableMenu = new PackOpeningMenu(Mod, this); return; }
        if (Back.Contains(x, y)) Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "PACK SHOP", "Pelican Origins · 30종 · 5장입");
        Rectangle pack = new(xPositionOnScreen + 86, yPositionOnScreen + 148, 250, 300);
        CardUi.DrawCardBack(b, pack);
        CardUi.CenterText(b, Game1.dialogueFont, "PELICAN\nORIGINS", new Rectangle(pack.X + 20, pack.Y + 105, pack.Width - 40, 100), Color.White, 0.74f);

        Rectangle info = new(xPositionOnScreen + 380, yPositionOnScreen + 148, 470, 300);
        CardUi.Panel(b, info);
        CardUi.Heading(b, "팩 확률", new Vector2(info.X + 24, info.Y + 18), CardUi.Ink, 0.76f);
        string[] lines =
        {
            "커먼 68% · 언커먼 22% · 레어 7%",
            "에픽 2.2% · 레전더리 0.65% · 시크릿 0.15%",
            "5번째 카드: 언커먼 이상 확정",
            "10팩 연속 레어 이상 미등장 시 천장 발동",
            "변형: 일반 82% · 홀로 12% · 골드 5% · 레인보우 1%",
            "상태: Good 10% · Near Mint 72% · Mint 18%"
        };
        for (int i = 0; i < lines.Length; i++) CardUi.Text(b, lines[i], new Vector2(info.X + 26, info.Y + 72 + i * 34), i >= 2 ? CardUi.Green : CardUi.Ink, 1.02f);

        CardUi.Button(b, BuyOne, $"1팩 {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        CardUi.Button(b, BuyFive, $"5팩 {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        CardUi.Button(b, OpenPack, $"팩 개봉 ({Mod.State.UnopenedPacks})", Mod.State.UnopenedPacks > 0, true);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 80, yPositionOnScreen + 555, width - 240, 34), CardUi.Muted, 1.08f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }
}

internal sealed class PackOpeningMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly List<CardPull> Pulls;
    private readonly string OpeningMessage;
    private readonly Rectangle NextButton;
    private readonly Rectangle DoneButton;
    private int Revealed;

    internal PackOpeningMenu(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod; ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(1180, 690);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        if (!Mod.TryOpenPack(out Pulls, out OpeningMessage)) Pulls = new List<CardPull>();
        NextButton = new Rectangle(xPositionOnScreen + width / 2 - 150, yPositionOnScreen + height - 92, 300, 58);
        DoneButton = NextButton;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pulls.Count == 0) { Game1.activeClickableMenu = ReturnMenu; return; }
        if (!NextButton.Contains(x, y)) return;
        if (Revealed < Pulls.Count)
        {
            CardDefinition? card = Mod.FindCard(Pulls[Revealed].CardKey);
            Revealed++;
            if (card is not null && ModEntry.GetRarityRank(card.Rarity) >= ModEntry.GetRarityRank("Rare")) Game1.playSound("newArtifact");
            else Game1.playSound("cardboardBox");
            return;
        }
        Game1.activeClickableMenu = new CardCollectionMenu(Mod, ReturnMenu);
        Game1.playSound("bigSelect");
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "PACK OPENING", OpeningMessage);
        int gap = 16;
        int cw = Math.Min(190, (width - 120 - gap * 4) / 5);
        int ch = 290;
        int total = cw * 5 + gap * 4;
        int startX = xPositionOnScreen + (width - total) / 2;
        int y = yPositionOnScreen + 160;
        double time = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 1000.0;
        for (int i = 0; i < 5; i++)
        {
            Rectangle cardRect = new(startX + i * (cw + gap), y, cw, ch);
            if (i >= Pulls.Count || i >= Revealed) { CardUi.DrawCardBack(b, cardRect); continue; }
            CardPull pull = Pulls[i];
            CardDefinition? def = Mod.FindCard(pull.CardKey);
            if (def is null) continue;
            bool glow = ModEntry.GetRarityRank(def.Rarity) >= 2;
            if (glow)
            {
                float pulse = 0.55f + (float)((Math.Sin(time * 4.0 + i) + 1.0) * 0.20);
                Rectangle halo = new(cardRect.X - 6, cardRect.Y - 6, cardRect.Width + 12, cardRect.Height + 12);
                b.Draw(Game1.fadeToBlackRect, halo, CardUi.RarityColor(def.Rarity) * pulse);
            }
            CardUi.DrawCardFront(b, cardRect, def, pull, glow);
        }

        if (Pulls.Count == 0) CardUi.Button(b, DoneButton, "돌아가기");
        else if (Revealed < Pulls.Count) CardUi.Button(b, NextButton, $"다음 카드  {Revealed + 1}/5", true, true);
        else CardUi.Button(b, DoneButton, "컬렉션에서 확인", true, true);
        drawMouse(b);
    }
}

internal sealed class CardCollectionMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly List<(string Key, Rectangle Bounds)> CardHits = new();
    private readonly Rectangle Prev;
    private readonly Rectangle Next;
    private readonly Rectangle ListButton;
    private readonly Rectangle ShelfButton;
    private readonly Rectangle Back;
    private int Page;
    private string SelectedKey = "";
    private string Message = "카드를 선택하면 오른쪽에서 판매 진열이 가능합니다.";

    internal CardCollectionMenu(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod; ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(1180, 730);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        Prev = new Rectangle(xPositionOnScreen + 64, yPositionOnScreen + height - 72, 130, 46);
        Next = new Rectangle(xPositionOnScreen + 590, yPositionOnScreen + height - 72, 130, 46);
        ListButton = new Rectangle(xPositionOnScreen + 812, yPositionOnScreen + 530, 260, 58);
        ShelfButton = new Rectangle(xPositionOnScreen + 812, yPositionOnScreen + 598, 260, 48);
        Back = new Rectangle(xPositionOnScreen + 970, yPositionOnScreen + height - 72, 110, 46);
    }

    private List<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> Rows()
        => Mod.GetCollectionRows().OrderByDescending(p => ModEntry.GetRarityRank(p.Card.Rarity)).ThenBy(p => p.Card.SetNo).ThenBy(p => p.Variant).ToList();

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (var hit in CardHits)
            if (hit.Bounds.Contains(x, y)) { SelectedKey = hit.Key; Game1.playSound("smallSelect"); return; }
        List< (string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> rows = Rows();
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (Prev.Contains(x, y) && Page > 0) { Page--; SelectedKey = ""; return; }
        if (Next.Contains(x, y) && Page < maxPage) { Page++; SelectedKey = ""; return; }
        if (ListButton.Contains(x, y) && !string.IsNullOrWhiteSpace(SelectedKey)) { Mod.TryListForSale(SelectedKey, out Message); return; }
        if (ShelfButton.Contains(x, y)) { Game1.activeClickableMenu = new SaleShelfMenu(Mod, this); return; }
        if (Back.Contains(x, y)) Game1.activeClickableMenu = ReturnMenu;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> rows = Rows();
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (direction < 0 && Page < maxPage) { Page++; SelectedKey = ""; }
        if (direction > 0 && Page > 0) { Page--; SelectedKey = ""; }
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "COLLECTION", "카드는 변형과 상태별로 따로 수집됩니다.");
        List<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> rows = Rows();
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        Page = Math.Clamp(Page, 0, maxPage);
        if (string.IsNullOrWhiteSpace(SelectedKey) && rows.Count > 0) SelectedKey = rows[Math.Min(Page * 6, rows.Count - 1)].CollectionKey;

        CardHits.Clear();
        int start = Page * 6;
        int cardW = 220;
        int cardH = 205;
        int baseX = xPositionOnScreen + 58;
        int baseY = yPositionOnScreen + 138;
        for (int i = 0; i < 6 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            int col = i % 3; int rr = i / 3;
            Rectangle card = new(baseX + col * 236, baseY + rr * 222, cardW, cardH);
            DrawCollectionCard(b, row, card, row.CollectionKey == SelectedKey);
            CardHits.Add((row.CollectionKey, card));
        }

        Rectangle detail = new(xPositionOnScreen + 790, yPositionOnScreen + 138, width - 848, 370);
        CardUi.Panel(b, detail, true);
        DrawDetail(b, detail, rows.FirstOrDefault(p => p.CollectionKey == SelectedKey));

        CardUi.Button(b, Prev, "이전", Page > 0);
        CardUi.Button(b, Next, "다음", Page < maxPage);
        CardUi.CenterText(b, Game1.smallFont, $"{rows.Count}종 보유 · {Page + 1}/{maxPage + 1}", new Rectangle(xPositionOnScreen + 210, yPositionOnScreen + height - 72, 360, 46), CardUi.Muted, 1.1f);
        int available = string.IsNullOrWhiteSpace(SelectedKey) ? 0 : Mod.GetOwned(SelectedKey) - Mod.GetListedCount(SelectedKey);
        CardUi.Button(b, ListButton, $"판매 진열 +1 ({Math.Max(0, available)}장 가능)", available > 0, true);
        CardUi.Button(b, ShelfButton, $"판매 진열대 보기 ({Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots})");
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 786, yPositionOnScreen + 660, width - 910, 28), CardUi.Muted, 0.95f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }

    private static void DrawCollectionCard(SpriteBatch b, (string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value) row, Rectangle r, bool selected)
    {
        CardUi.Panel(b, r, selected);
        Color rarity = CardUi.RarityColor(row.Card.Rarity);
        Rectangle band = new(r.X + 10, r.Y + 10, r.Width - 20, 34);
        b.Draw(Game1.fadeToBlackRect, band, rarity);
        CardUi.CenterText(b, Game1.smallFont, $"{row.Card.SetNo} · {ModEntry.RarityName(row.Card.Rarity)}", band, Color.White, 0.95f);
        CardUi.CenterText(b, Game1.dialogueFont, row.Card.Name, new Rectangle(r.X + 12, r.Y + 55, r.Width - 24, 43), CardUi.Ink, 0.70f);
        CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(row.Variant)} · {row.Condition}", new Rectangle(r.X + 12, r.Y + 105, r.Width - 24, 28), rarity, 1.02f);
        CardUi.CenterText(b, Game1.smallFont, $"보유 {row.Count}장", new Rectangle(r.X + 12, r.Y + 140, r.Width - 24, 25), CardUi.Ink, 1.05f);
        CardUi.CenterText(b, Game1.smallFont, $"시세 {row.Value:N0}G", new Rectangle(r.X + 12, r.Y + 170, r.Width - 24, 25), CardUi.Green, 1.05f);
    }

    private void DrawDetail(SpriteBatch b, Rectangle r, (string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value) row)
    {
        if (row.Card is null) { CardUi.CenterText(b, Game1.dialogueFont, "아직 수집한 카드가 없습니다.", r, CardUi.Muted, 0.72f); return; }
        Color rarity = CardUi.RarityColor(row.Card.Rarity);
        CardUi.CenterText(b, Game1.dialogueFont, row.Card.Name, new Rectangle(r.X + 18, r.Y + 22, r.Width - 36, 48), CardUi.Ink, 0.82f);
        CardUi.CenterText(b, Game1.smallFont, $"{row.Card.SetNo} · {row.Card.Category} · {ModEntry.RarityName(row.Card.Rarity)}", new Rectangle(r.X + 18, r.Y + 70, r.Width - 36, 30), rarity, 1.05f);
        string[] lines =
        {
            $"변형   {ModEntry.VariantName(row.Variant)}",
            $"상태   {row.Condition}",
            $"보유   {row.Count}장",
            $"진열   {Mod.GetListedCount(row.CollectionKey)}장",
            $"현재 시세   {row.Value:N0}G"
        };
        for (int i = 0; i < lines.Length; i++) CardUi.Text(b, lines[i], new Vector2(r.X + 28, r.Y + 120 + i * 38), i == 4 ? CardUi.Green : CardUi.Ink, 1.10f);
        Rectangle flavor = new(r.X + 24, r.Bottom - 72, r.Width - 48, 50);
        CardUi.CenterText(b, Game1.smallFont, row.Card.Flavor, flavor, CardUi.Muted, 0.90f);
    }
}

internal sealed class SaleShelfMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly List<(int Index, Rectangle Remove)> Removes = new();
    private readonly Rectangle Back;

    internal SaleShelfMenu(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod; ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(1040, 700);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        Back = new Rectangle(xPositionOnScreen + width - 170, yPositionOnScreen + height - 66, 120, 44);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (var rem in Removes)
            if (rem.Remove.Contains(x, y)) { Mod.RemoveListing(rem.Index); return; }
        if (Back.Contains(x, y)) Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "SALE SHELF", "진열한 카드는 다음 날 아침 손님이 확률적으로 구매합니다.");
        Removes.Clear();
        int y = yPositionOnScreen + 140;
        for (int i = 0; i < Mod.State.SaleShelf.Count && i < Mod.Config.SaleShelfSlots; i++)
        {
            SaleListing listing = Mod.State.SaleShelf[i];
            Rectangle row = new(xPositionOnScreen + 62, y + i * 58, width - 124, 50);
            CardUi.Panel(b, row, i == 0);
            string label = listing.CollectionKey;
            if (CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out string condition))
            {
                CardDefinition? card = Mod.FindCard(cardKey);
                if (card is not null) label = $"{i + 1}. {card.Name} · {ModEntry.VariantName(variant)} · {condition}";
            }
            CardUi.Text(b, label, new Vector2(row.X + 18, row.Y + 12), CardUi.Ink, 1.02f);
            CardUi.Text(b, $"{listing.Price:N0}G", new Vector2(row.Right - 180, row.Y + 12), CardUi.Green, 1.05f);
            Rectangle remove = new(row.Right - 60, row.Y + 6, 44, 38);
            CardUi.Button(b, remove, "X");
            Removes.Add((i, remove));
        }
        if (Mod.State.SaleShelf.Count == 0)
        {
            Rectangle empty = new(xPositionOnScreen + 62, yPositionOnScreen + 180, width - 124, 220);
            CardUi.Panel(b, empty);
            CardUi.CenterText(b, Game1.dialogueFont, "진열된 카드가 없습니다.", new Rectangle(empty.X + 20, empty.Y + 45, empty.Width - 40, 60), CardUi.Muted, 0.78f);
            CardUi.CenterText(b, Game1.smallFont, "컬렉션에서 여분 카드를 선택해 판매 진열대에 올려보세요.", new Rectangle(empty.X + 20, empty.Y + 125, empty.Width - 40, 40), CardUi.Green, 1.12f);
        }

        Rectangle summary = new(xPositionOnScreen + 62, yPositionOnScreen + height - 132, width - 250, 58);
        CardUi.Panel(b, summary);
        CardUi.CenterText(b, Game1.smallFont, Mod.State.LastDailySalesSummary, summary, CardUi.Muted, 1.02f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }
}
