using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

// v0.3.0 temporary verification UI. This intentionally prioritizes complete, testable behavior.
// The final visual pass will replace these screens after the gameplay loop is approved.
internal sealed class FeatureCardShopMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly Rectangle Pack;
    private readonly Rectangle Collection;
    private readonly Rectangle Shelf;
    private readonly Rectangle Close;

    internal FeatureCardShopMenu(ModEntry mod)
    {
        Mod = mod;
        Rectangle r = CardUi.Center(1000, 650);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        Pack = new Rectangle(r.X + 70, r.Y + 235, 250, 150);
        Collection = new Rectangle(r.X + 375, r.Y + 235, 250, 150);
        Shelf = new Rectangle(r.X + 680, r.Y + 235, 250, 150);
        Close = new Rectangle(r.Right - 160, r.Bottom - 70, 110, 42);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pack.Contains(x, y)) { Game1.activeClickableMenu = new FeaturePackShopMenu(Mod, this); return; }
        if (Collection.Contains(x, y)) { Game1.activeClickableMenu = new FeatureCollectionMenu(Mod, this); return; }
        if (Shelf.Contains(x, y)) { Game1.activeClickableMenu = new FeatureSaleShelfMenu(Mod, this); return; }
        if (Close.Contains(x, y)) exitThisMenu();
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        CardUi.Begin(b, this, "JUNIMO CARDS · 기능 검증", "v0.3.0 기능 완성 코어 · UI 디자인은 다음 단계");
        Rectangle stats = new(xPositionOnScreen + 70, yPositionOnScreen + 135, width - 140, 72);
        CardUi.Panel(b, stats);
        CardUi.CenterText(b, Game1.smallFont,
            $"골드 {Game1.player.Money:N0}G  ·  미개봉 팩 {Mod.State.UnopenedPacks}  ·  컬렉션 {Mod.Core.UniqueCardCount()}/{Mod.Cards.Count}  ·  총 {Mod.Core.TotalOwnedCopies()}장  ·  누적 매출 {Mod.State.LifetimeCardRevenue:N0}G",
            stats, CardUi.Ink, 1.05f);

        DrawHomeButton(b, Pack, "팩 구매 / 개봉", $"1팩 {Mod.Config.PackPrice:N0}G\n5팩 {Mod.Config.FivePackPrice:N0}G\n천장 {Mod.State.PacksSinceRare}/10");
        DrawHomeButton(b, Collection, "컬렉션", $"등급 필터\n카드 상세 / 진열\n컬렉션 보너스");
        DrawHomeButton(b, Shelf, "판매 진열대", $"고정 8슬롯\n진열가 조정\n하루 최대 {Mod.Config.MaxDailySales}건");

        Rectangle today = new(xPositionOnScreen + 70, yPositionOnScreen + 420, width - 140, 105);
        CardUi.Panel(b, today);
        CardUi.Text(b, "오늘의 카드샵", new Vector2(today.X + 20, today.Y + 15), CardUi.Ink, 1.1f);
        CardUi.CenterText(b, Game1.smallFont, Mod.State.LastDailySalesSummary,
            new Rectangle(today.X + 20, today.Y + 50, today.Width - 40, 35), CardUi.Muted, 1.0f);
        CardUi.Text(b, $"손님 {Mod.State.LastCustomerCount}명 · 판매 {Mod.State.LastCardsSold}장 · 오늘 매출 {Mod.State.LastDailyRevenue:N0}G",
            new Vector2(today.X + 20, today.Bottom + 10), CardUi.Muted, 0.95f);
        CardUi.Button(b, Close, "닫기");
        drawMouse(b);
    }

    private static void DrawHomeButton(SpriteBatch b, Rectangle r, string title, string body)
    {
        CardUi.Panel(b, r);
        CardUi.CenterText(b, Game1.dialogueFont, title, new Rectangle(r.X + 12, r.Y + 15, r.Width - 24, 44), CardUi.GreenDark, 0.75f);
        string[] lines = body.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            CardUi.CenterText(b, Game1.smallFont, lines[i], new Rectangle(r.X + 14, r.Y + 70 + i * 25, r.Width - 28, 24), CardUi.Ink, 0.95f);
    }
}

internal sealed class FeaturePackShopMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle BuyOne;
    private readonly Rectangle BuyFive;
    private readonly Rectangle Open;
    private readonly Rectangle Back;
    private string Message = "5번째 카드는 언커먼 이상 확정 · 10팩 Rare+ 천장";

    internal FeaturePackShopMenu(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod; ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(900, 600);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        BuyOne = new Rectangle(r.X + 100, r.Y + 350, 190, 58);
        BuyFive = new Rectangle(r.X + 355, r.Y + 350, 190, 58);
        Open = new Rectangle(r.X + 610, r.Y + 350, 190, 58);
        Back = new Rectangle(r.Right - 160, r.Bottom - 65, 110, 42);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (BuyOne.Contains(x, y)) { Mod.TryBuyPacks(1, out Message); return; }
        if (BuyFive.Contains(x, y)) { Mod.TryBuyPacks(5, out Message); return; }
        if (Open.Contains(x, y) && Mod.State.UnopenedPacks > 0) { Game1.activeClickableMenu = new FeaturePackOpeningMenu(Mod, this); return; }
        if (Back.Contains(x, y)) Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "팩 구매", "Pelican Origins · 30종 · 5장입");
        Rectangle info = new(xPositionOnScreen + 80, yPositionOnScreen + 145, width - 160, 165);
        CardUi.Panel(b, info);
        string[] lines =
        {
            "기본: 커먼 68% · 언커먼 22% · 레어 7% · 에픽 2.2% · 레전더리 0.65% · 시크릿 0.15%",
            "5번째 카드: 언커먼 이상 확정",
            $"Rare 이상 천장: {Mod.State.PacksSinceRare}/10 (9팩 연속 미등장 후 다음 팩 Rare+ 보장)",
            "변형: 일반 82% · 홀로 12% · 골드 5% · 레인보우 1%",
            "상태: Good 10% · Near Mint 72% · Mint 18%"
        };
        for (int i = 0; i < lines.Length; i++)
            CardUi.Text(b, lines[i], new Vector2(info.X + 20, info.Y + 18 + i * 28), i == 2 ? CardUi.Green : CardUi.Ink, 0.9f);

        CardUi.Button(b, BuyOne, $"1팩 {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        CardUi.Button(b, BuyFive, $"5팩 {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        CardUi.Button(b, Open, $"팩 개봉 ({Mod.State.UnopenedPacks})", Mod.State.UnopenedPacks > 0, true);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 100, yPositionOnScreen + 435, width - 200, 40), CardUi.Muted, 0.95f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }
}

internal sealed class FeaturePackOpeningMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly List<CardPull> Pulls;
    private readonly List<Rectangle> CardBounds = new();
    private readonly Rectangle Done;
    private readonly string OpeningMessage;
    private int Revealed;

    internal FeaturePackOpeningMenu(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod; ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(1180, 690);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        if (!Mod.TryOpenPack(out Pulls, out OpeningMessage))
            Pulls = new List<CardPull>();
        int gap = 18;
        int cw = 190;
        int total = cw * 5 + gap * 4;
        int sx = r.X + (r.Width - total) / 2;
        for (int i = 0; i < 5; i++)
            CardBounds.Add(new Rectangle(sx + i * (cw + gap), r.Y + 160, cw, 300));
        Done = new Rectangle(r.X + r.Width / 2 - 150, r.Bottom - 80, 300, 50);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pulls.Count == 0) { Game1.activeClickableMenu = ReturnMenu; return; }
        if (Revealed < Pulls.Count && CardBounds[Revealed].Contains(x, y))
        {
            CardDefinition? card = Mod.FindCard(Pulls[Revealed].CardKey);
            Revealed++;
            int rank = card is null ? 0 : ModEntry.GetRarityRank(card.Rarity);
            Game1.playSound(rank >= 4 ? "reward" : rank >= 2 ? "newArtifact" : "cardboardBox");
            return;
        }
        if (Revealed >= Pulls.Count && Done.Contains(x, y))
            Game1.activeClickableMenu = new FeatureCollectionMenu(Mod, ReturnMenu);
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "팩 개봉", OpeningMessage);
        for (int i = 0; i < 5; i++)
        {
            Rectangle r = CardBounds[i];
            if (i >= Pulls.Count || i >= Revealed)
            {
                CardUi.DrawCardBack(b, r);
                CardUi.CenterText(b, Game1.smallFont, i == Revealed ? "클릭해서 공개" : $"{i + 1}", new Rectangle(r.X, r.Bottom + 6, r.Width, 28), i == Revealed ? CardUi.Green : CardUi.Muted, 0.9f);
                continue;
            }
            CardPull pull = Pulls[i];
            CardDefinition? card = Mod.FindCard(pull.CardKey);
            if (card is not null)
                CardUi.DrawCardFront(b, r, card, pull, ModEntry.GetRarityRank(card.Rarity) >= 2);
        }

        CardUi.CenterText(b, Game1.smallFont, $"Rare 천장 진행 {Mod.State.PacksSinceRare}/10 · 미개봉 팩 {Mod.State.UnopenedPacks}",
            new Rectangle(xPositionOnScreen + 100, yPositionOnScreen + 505, width - 200, 30), CardUi.Muted, 0.95f);
        if (Revealed >= Pulls.Count)
            CardUi.Button(b, Done, "컬렉션에서 확인", true, true);
        drawMouse(b);
    }
}

internal sealed class FeatureCollectionMenu : IClickableMenu
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
    private string Message = "카드를 선택하세요.";
    private int Page;

    internal FeatureCollectionMenu(ModEntry mod, IClickableMenu returnMenu, int targetSlot = -1)
    {
        Mod = mod; ReturnMenu = returnMenu; TargetSlot = targetSlot;
        Rectangle r = CardUi.Center(1220, 740);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        string[] names = { "All", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Secret" };
        int fy = r.Y + 138;
        for (int i = 0; i < names.Length; i++)
            Filters[names[i]] = new Rectangle(r.X + 35, fy + i * 58, 150, 44);
        Prev = new Rectangle(r.X + 220, r.Bottom - 65, 100, 42);
        Next = new Rectangle(r.X + 650, r.Bottom - 65, 100, 42);
        List = new Rectangle(r.X + 835, r.Y + 500, 300, 52);
        Bonus = new Rectangle(r.X + 835, r.Y + 560, 300, 52);
        Shelf = new Rectangle(r.X + 835, r.Y + 620, 180, 42);
        Back = new Rectangle(r.X + 1030, r.Y + 620, 105, 42);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (var pair in Filters)
        {
            if (!pair.Value.Contains(x, y)) continue;
            Filter = pair.Key; Page = 0; SelectedKey = ""; Game1.playSound("smallSelect"); return;
        }
        foreach (var hit in Hits)
        {
            if (hit.Bounds.Contains(x, y)) { SelectedKey = hit.Key; Game1.playSound("smallSelect"); return; }
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (Prev.Contains(x, y) && Page > 0) { Page--; SelectedKey = ""; return; }
        if (Next.Contains(x, y) && Page < maxPage) { Page++; SelectedKey = ""; return; }
        if (Bonus.Contains(x, y)) { Mod.Core.TryClaimCollectionBonus(out Message); return; }
        if (List.Contains(x, y) && !string.IsNullOrWhiteSpace(SelectedKey))
        {
            bool ok = TargetSlot >= 0
                ? Mod.Core.TryListForSaleAtSlot(SelectedKey, TargetSlot, out Message)
                : Mod.Core.TryListForSale(SelectedKey, out Message);
            if (ok && TargetSlot >= 0)
                Game1.activeClickableMenu = ReturnMenu;
            return;
        }
        if (Shelf.Contains(x, y)) { Game1.activeClickableMenu = new FeatureSaleShelfMenu(Mod, this); return; }
        if (Back.Contains(x, y)) Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "컬렉션", TargetSlot >= 0 ? $"{TargetSlot + 1}번 진열 슬롯에 넣을 카드를 선택" : "등급별 필터 · 상세정보 · 컬렉션 보너스");
        foreach (var pair in Filters)
        {
            int count = pair.Key == "All" ? Mod.Core.UniqueCardCount() : Mod.Core.UniqueCountForRarity(pair.Key);
            CardUi.Button(b, pair.Value, $"{FilterName(pair.Key)} ({count})", true, string.Equals(Filter, pair.Key, StringComparison.OrdinalIgnoreCase));
        }

        var rows = Mod.Core.GetCollectionRows(Filter);
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        Page = Math.Clamp(Page, 0, maxPage);
        int start = Page * 6;
        Hits.Clear();
        int bx = xPositionOnScreen + 215;
        int by = yPositionOnScreen + 138;
        for (int i = 0; i < 6 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            int col = i % 3; int rr = i / 3;
            Rectangle card = new(bx + col * 190, by + rr * 190, 175, 170);
            CardUi.Panel(b, card, row.CollectionKey == SelectedKey);
            Rectangle band = new(card.X + 8, card.Y + 8, card.Width - 16, 28);
            b.Draw(Game1.fadeToBlackRect, band, CardUi.RarityColor(row.Card.Rarity));
            CardUi.CenterText(b, Game1.smallFont, $"{row.Card.SetNo} · {ModEntry.RarityName(row.Card.Rarity)}", band, Color.White, 0.8f);
            CardUi.CenterText(b, Game1.dialogueFont, row.Card.Name, new Rectangle(card.X + 8, card.Y + 45, card.Width - 16, 38), CardUi.Ink, 0.62f);
            CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(row.Variant)} · {row.Condition}", new Rectangle(card.X + 8, card.Y + 88, card.Width - 16, 25), CardUi.Ink, 0.78f);
            CardUi.CenterText(b, Game1.smallFont, $"보유 {row.Count} · 진열 {Mod.GetListedCount(row.CollectionKey)}", new Rectangle(card.X + 8, card.Y + 118, card.Width - 16, 24), CardUi.Muted, 0.8f);
            CardUi.CenterText(b, Game1.smallFont, $"최고 진열가 {row.Value:N0}G", new Rectangle(card.X + 8, card.Y + 143, card.Width - 16, 22), CardUi.Green, 0.78f);
            Hits.Add((row.CollectionKey, card));
        }

        Rectangle detail = new(xPositionOnScreen + 800, yPositionOnScreen + 138, 355, 335);
        CardUi.Panel(b, detail, true);
        var selected = rows.FirstOrDefault(p => p.CollectionKey == SelectedKey);
        if (selected.Card is not null)
        {
            CardUi.CenterText(b, Game1.dialogueFont, selected.Card.Name, new Rectangle(detail.X + 20, detail.Y + 18, detail.Width - 40, 45), CardUi.Ink, 0.75f);
            string[] lines =
            {
                $"등급  {ModEntry.RarityName(selected.Card.Rarity)}",
                $"변형  {ModEntry.VariantName(selected.Variant)}",
                $"상태  {selected.Condition}",
                $"보유  {selected.Count}장",
                $"진열  {Mod.GetListedCount(selected.CollectionKey)}장",
                $"최고 진열가  {selected.Value:N0}G"
            };
            for (int i = 0; i < lines.Length; i++)
                CardUi.Text(b, lines[i], new Vector2(detail.X + 28, detail.Y + 75 + i * 32), i == 5 ? CardUi.Green : CardUi.Ink, 0.95f);
            CardUi.CenterText(b, Game1.smallFont, selected.Card.Flavor, new Rectangle(detail.X + 20, detail.Bottom - 65, detail.Width - 40, 45), CardUi.Muted, 0.85f);
        }

        int available = string.IsNullOrWhiteSpace(SelectedKey) ? 0 : Mod.GetOwned(SelectedKey) - Mod.GetListedCount(SelectedKey);
        CardUi.Button(b, List, TargetSlot >= 0 ? $"{TargetSlot + 1}번 슬롯에 진열" : $"판매 진열 +1 ({Math.Max(0, available)}장 가능)", available > 0, true);
        var bonus = Mod.Core.GetNextCollectionBonus();
        CardUi.Button(b, Bonus, bonus.Complete ? "컬렉션 보너스 완료" : $"보너스 {bonus.Required}종 → 팩 {bonus.Reward}개", !bonus.Complete, bonus.CanClaim);
        CardUi.Button(b, Shelf, "판매 진열대");
        CardUi.Button(b, Prev, "이전", Page > 0);
        CardUi.Button(b, Next, "다음", Page < maxPage);
        CardUi.CenterText(b, Game1.smallFont, $"{FilterName(Filter)} · {rows.Count}개 변형/상태 · {Page + 1}/{maxPage + 1}", new Rectangle(xPositionOnScreen + 335, yPositionOnScreen + height - 65, 300, 42), CardUi.Muted, 0.9f);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 790, yPositionOnScreen + 675, 340, 28), CardUi.Muted, 0.82f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }

    private static string FilterName(string filter) => filter switch
    {
        "All" => "전체", "Common" => "커먼", "Uncommon" => "언커먼", "Rare" => "레어", "Epic" => "에픽", "Legendary" => "레전더리", "Secret" => "시크릿", _ => filter
    };
}

internal sealed class FeatureSaleShelfMenu : IClickableMenu
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
    private string Message = "슬롯을 선택하면 진열가와 판매 확률을 관리할 수 있습니다.";

    internal FeatureSaleShelfMenu(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod; ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(1180, 720);
        xPositionOnScreen = r.X; yPositionOnScreen = r.Y; width = r.Width; height = r.Height;
        int sx = r.X + 55, sy = r.Y + 150;
        for (int i = 0; i < 8; i++)
        {
            int col = i % 4, row = i / 4;
            Slots.Add(new Rectangle(sx + col * 250, sy + row * 190, 225, 165));
        }
        Add = new Rectangle(r.X + 60, r.Bottom - 68, 210, 44);
        Down = new Rectangle(r.X + 300, r.Bottom - 68, 130, 44);
        Up = new Rectangle(r.X + 445, r.Bottom - 68, 130, 44);
        Remove = new Rectangle(r.X + 590, r.Bottom - 68, 170, 44);
        Back = new Rectangle(r.Right - 160, r.Bottom - 68, 110, 44);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < Slots.Count; i++)
            if (Slots[i].Contains(x, y)) { SelectedSlot = i; Game1.playSound("smallSelect"); return; }

        SaleListing? listing = Mod.Core.GetListingAtSlot(SelectedSlot);
        if (Add.Contains(x, y) && listing is null)
        {
            Game1.activeClickableMenu = new FeatureCollectionMenu(Mod, this, SelectedSlot);
            return;
        }
        if (Down.Contains(x, y) && listing is not null) { Mod.Core.TryAdjustListingPrice(SelectedSlot, -1, out Message); return; }
        if (Up.Contains(x, y) && listing is not null) { Mod.Core.TryAdjustListingPrice(SelectedSlot, 1, out Message); return; }
        if (Remove.Contains(x, y) && listing is not null) { Mod.Core.RemoveListingBySlot(SelectedSlot, out Message); return; }
        if (Back.Contains(x, y)) Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "판매 진열대", "8개 고정 슬롯 · 좋은 카드일수록 손님 선택 확률 상승 · 하루 최대 3건 판매");
        IReadOnlyList<SaleListing?> shelf = Mod.Core.GetShelfSlots();
        for (int i = 0; i < Slots.Count; i++)
        {
            Rectangle r = Slots[i];
            CardUi.Panel(b, r, i == SelectedSlot);
            SaleListing? listing = shelf[i];
            if (listing is null)
            {
                CardUi.CenterText(b, Game1.dialogueFont, "+", new Rectangle(r.X + 10, r.Y + 25, r.Width - 20, 55), CardUi.Muted, 0.85f);
                CardUi.CenterText(b, Game1.smallFont, $"{i + 1}번 슬롯 · 비어 있음", new Rectangle(r.X + 10, r.Y + 95, r.Width - 20, 30), CardUi.Muted, 0.85f);
                continue;
            }

            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out string condition))
                continue;
            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null) continue;
            CardUi.CenterText(b, Game1.dialogueFont, card.Name, new Rectangle(r.X + 10, r.Y + 18, r.Width - 20, 38), CardUi.Ink, 0.62f);
            CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.RarityName(card.Rarity)} · {ModEntry.VariantName(variant)} · {condition}", new Rectangle(r.X + 10, r.Y + 62, r.Width - 20, 26), CardUi.RarityColor(card.Rarity), 0.78f);
            CardUi.CenterText(b, Game1.smallFont, $"진열가 {listing.Price:N0}G", new Rectangle(r.X + 10, r.Y + 97, r.Width - 20, 26), CardUi.Green, 0.9f);
            CardUi.CenterText(b, Game1.smallFont, $"예상 판매확률 {Mod.Core.GetSaleChance(listing) * 100:0}%", new Rectangle(r.X + 10, r.Y + 127, r.Width - 20, 24), CardUi.Muted, 0.78f);
        }

        SaleListing? selected = Mod.Core.GetListingAtSlot(SelectedSlot);
        CardUi.Button(b, Add, selected is null ? $"{SelectedSlot + 1}번에 카드 넣기" : "슬롯 사용 중", selected is null, true);
        CardUi.Button(b, Down, "가격 -50", selected is not null);
        CardUi.Button(b, Up, "가격 +50", selected is not null);
        CardUi.Button(b, Remove, "카드 회수", selected is not null);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 785, yPositionOnScreen + height - 68, 220, 44), CardUi.Muted, 0.75f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }
}
