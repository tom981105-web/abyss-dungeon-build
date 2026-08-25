using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

// v0.3.2 readability pass.
// Keep every v0.3.1 gameplay action, but show fewer words at once and use larger type.
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
        Rectangle r = CardUi.Center(1040, 650);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        Pack = new Rectangle(r.X + 70, r.Y + 255, 270, 155);
        Collection = new Rectangle(r.X + 385, r.Y + 255, 270, 155);
        Shelf = new Rectangle(r.X + 700, r.Y + 255, 270, 155);
        Close = new Rectangle(r.Right - 165, r.Bottom - 66, 115, 42);
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
        Rectangle stats = new(xPositionOnScreen + 70, yPositionOnScreen + 135, width - 140, 88);
        CardUi.Panel(b, stats);
        int cell = stats.Width / 4;
        DrawStat(b, new Rectangle(stats.X, stats.Y, cell, stats.Height), "골드", $"{Game1.player.Money:N0}G");
        DrawStat(b, new Rectangle(stats.X + cell, stats.Y, cell, stats.Height), "미개봉", $"{Mod.State.UnopenedPacks}팩");
        DrawStat(b, new Rectangle(stats.X + cell * 2, stats.Y, cell, stats.Height), "컬렉션", $"{unique}/{Mod.Cards.Count}");
        DrawStat(b, new Rectangle(stats.X + cell * 3, stats.Y, stats.Width - cell * 3, stats.Height), "매출", $"{Mod.State.LifetimeCardRevenue:N0}G");

        DrawHomeTile(b, Pack, "팩 구매", $"1팩 {Mod.Config.PackPrice:N0}G");
        DrawHomeTile(b, Collection, "컬렉션", $"수집 {unique}/{Mod.Cards.Count}");
        DrawHomeTile(b, Shelf, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots}");

        Rectangle today = new(xPositionOnScreen + 70, yPositionOnScreen + 440, width - 140, 88);
        CardUi.Panel(b, today);
        CardUi.Heading(b, "오늘", new Vector2(today.X + 20, today.Y + 20), CardUi.Ink, 0.80f);
        string summary = $"손님 {Mod.State.LastCustomerCount}명   판매 {Mod.State.LastCardsSold}장   +{Mod.State.LastDailyRevenue:N0}G";
        CardUi.CenterText(b, Game1.dialogueFont, summary, new Rectangle(today.X + 115, today.Y + 12, today.Width - 135, 60), CardUi.GreenDark, 0.78f);

        CardUi.Button(b, Close, "닫기");
        drawMouse(b);
    }

    private static void DrawStat(SpriteBatch b, Rectangle r, string label, string value)
    {
        CardUi.CenterText(b, Game1.smallFont, label, new Rectangle(r.X + 4, r.Y + 10, r.Width - 8, 25), CardUi.Muted, 1.05f);
        CardUi.CenterText(b, Game1.dialogueFont, value, new Rectangle(r.X + 4, r.Y + 36, r.Width - 8, 42), CardUi.Ink, 0.76f);
    }

    private static void DrawHomeTile(SpriteBatch b, Rectangle r, string title, string value)
    {
        CardUi.Panel(b, r);
        CardUi.CenterText(b, Game1.dialogueFont, title, new Rectangle(r.X + 14, r.Y + 25, r.Width - 28, 50), CardUi.GreenDark, 0.86f);
        CardUi.CenterText(b, Game1.smallFont, value, new Rectangle(r.X + 14, r.Y + 92, r.Width - 28, 34), CardUi.Ink, 1.18f);
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
        Rectangle r = CardUi.Center(920, 600);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        BuyOne = new Rectangle(r.X + 80, r.Y + 380, 220, 62);
        BuyFive = new Rectangle(r.X + 350, r.Y + 380, 220, 62);
        Open = new Rectangle(r.X + 620, r.Y + 380, 220, 62);
        Back = new Rectangle(r.Right - 160, r.Bottom - 65, 110, 42);
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
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "팩 구매", "Pelican Origins · 5장");

        Rectangle card = new(xPositionOnScreen + 95, yPositionOnScreen + 150, 170, 205);
        CardUi.DrawCardBack(b, card);

        Rectangle info = new(xPositionOnScreen + 310, yPositionOnScreen + 150, 500, 205);
        CardUi.Panel(b, info);
        CardUi.CenterText(b, Game1.dialogueFont, $"보유 {Mod.State.UnopenedPacks}팩", new Rectangle(info.X + 20, info.Y + 18, info.Width - 40, 52), CardUi.Ink, 0.92f);
        CardUi.CenterText(b, Game1.smallFont, "5번째 카드 · 언커먼 이상", new Rectangle(info.X + 20, info.Y + 88, info.Width - 40, 34), CardUi.GreenDark, 1.15f);
        CardUi.CenterText(b, Game1.smallFont, $"Rare+ 천장 · {Mod.State.PacksSinceRare}/10", new Rectangle(info.X + 20, info.Y + 132, info.Width - 40, 34), CardUi.GreenDark, 1.15f);

        CardUi.Button(b, BuyOne, $"1팩  {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        CardUi.Button(b, BuyFive, $"5팩  {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        CardUi.Button(b, Open, $"개봉  {Mod.State.UnopenedPacks}팩", Mod.State.UnopenedPacks > 0, true);

        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 90, yPositionOnScreen + 470, width - 180, 42), CardUi.Muted, 1.08f);
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
    private readonly List<Rectangle> Cards = new();
    private readonly Rectangle Done;
    private readonly string OpeningMessage;

    internal ReadablePackOpeningMenu032(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(1240, 690);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        if (!Mod.TryOpenPack(out Pulls, out OpeningMessage))
            Pulls = new List<CardPull>();
        Revealed = new bool[Pulls.Count];

        int gap = 16;
        int cardW = 205;
        int total = cardW * 5 + gap * 4;
        int startX = r.X + (r.Width - total) / 2;
        for (int i = 0; i < 5; i++)
            Cards.Add(new Rectangle(startX + i * (cardW + gap), r.Y + 160, cardW, 320));

        Done = new Rectangle(r.X + r.Width / 2 - 150, r.Bottom - 78, 300, 50);
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
                CardDefinition? def = Mod.FindCard(Pulls[i].CardKey);
                int rank = def is null ? 0 : ModEntry.GetRarityRank(def.Rarity);
                Game1.playSound(rank >= 4 ? "reward" : rank >= 2 ? "newArtifact" : "cardboardBox");
                return;
            }
        }

        if (Revealed.Length > 0 && Revealed.All(p => p) && Done.Contains(x, y))
            Game1.activeClickableMenu = new ReadableCollectionMenu032(Mod, ReturnMenu);
    }

    public override void draw(SpriteBatch b)
    {
        CardUi.Begin(b, this, "팩 개봉", OpeningMessage.Contains("천장") ? OpeningMessage : "카드를 눌러 공개하세요");

        for (int i = 0; i < Cards.Count; i++)
        {
            Rectangle r = Cards[i];
            if (i >= Pulls.Count || !Revealed[i])
            {
                CardUi.DrawCardBack(b, r);
                if (i < Pulls.Count)
                    CardUi.CenterText(b, Game1.smallFont, "공개", new Rectangle(r.X, r.Bottom + 8, r.Width, 30), CardUi.GreenDark, 1.08f);
                continue;
            }

            CardPull pull = Pulls[i];
            CardDefinition? def = Mod.FindCard(pull.CardKey);
            if (def is not null)
                CardUi.DrawCardFront(b, r, def, pull, ModEntry.GetRarityRank(def.Rarity) >= 2);
        }

        int opened = Revealed.Count(p => p);
        CardUi.CenterText(b, Game1.dialogueFont, $"{opened}/5", new Rectangle(xPositionOnScreen + width / 2 - 70, yPositionOnScreen + 515, 140, 45), CardUi.Ink, 0.85f);
        if (Revealed.Length > 0 && Revealed.All(p => p))
            CardUi.Button(b, Done, "컬렉션 확인", true, true);
        drawMouse(b);
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
        Rectangle r = CardUi.Center(1260, 760);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        string[] names = { "All", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Secret" };
        int fw = 152;
        int gap = 10;
        int start = r.X + 45;
        for (int i = 0; i < names.Length; i++)
            Filters[names[i]] = new Rectangle(start + i * (fw + gap), r.Y + 125, fw, 46);

        Prev = new Rectangle(r.X + 235, r.Bottom - 62, 110, 42);
        Next = new Rectangle(r.X + 610, r.Bottom - 62, 110, 42);
        List = new Rectangle(r.X + 885, r.Y + 500, 300, 52);
        Bonus = new Rectangle(r.X + 885, r.Y + 560, 300, 52);
        Shelf = new Rectangle(r.X + 885, r.Y + 622, 180, 42);
        Back = new Rectangle(r.X + 1080, r.Y + 622, 105, 42);
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

        int sx = xPositionOnScreen + 50;
        int sy = yPositionOnScreen + 195;
        for (int i = 0; i < 6 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            int col = i % 3;
            int rr = i / 3;
            Rectangle card = new(sx + col * 255, sy + rr * 205, 235, 185);
            CardUi.Panel(b, card, row.CollectionKey == SelectedKey);
            Rectangle band = new(card.X + 8, card.Y + 8, card.Width - 16, 32);
            b.Draw(Game1.fadeToBlackRect, band, CardUi.RarityColor(row.Card.Rarity));
            CardUi.CenterText(b, Game1.smallFont, ModEntry.RarityName(row.Card.Rarity), band, Color.White, 1.02f);
            CardUi.CenterText(b, Game1.dialogueFont, row.Card.Name, new Rectangle(card.X + 12, card.Y + 55, card.Width - 24, 50), CardUi.Ink, 0.78f);
            CardUi.CenterText(b, Game1.smallFont, $"보유 {row.Count}장", new Rectangle(card.X + 12, card.Y + 118, card.Width - 24, 32), CardUi.Ink, 1.10f);
            Hits.Add((row.CollectionKey, card));
        }

        Rectangle detail = new(xPositionOnScreen + 820, yPositionOnScreen + 195, 370, 280);
        CardUi.Panel(b, detail, true);
        var selected = rows.FirstOrDefault(p => p.CollectionKey == SelectedKey);
        if (selected.Card is not null)
        {
            CardUi.CenterText(b, Game1.dialogueFont, selected.Card.Name, new Rectangle(detail.X + 20, detail.Y + 18, detail.Width - 40, 50), CardUi.Ink, 0.90f);
            CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(selected.Variant)} · {selected.Condition}", new Rectangle(detail.X + 20, detail.Y + 82, detail.Width - 40, 34), CardUi.Ink, 1.12f);
            CardUi.CenterText(b, Game1.smallFont, $"보유 {selected.Count}장   진열 {Mod.GetListedCount(selected.CollectionKey)}장", new Rectangle(detail.X + 20, detail.Y + 128, detail.Width - 40, 34), CardUi.Muted, 1.08f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{selected.Value:N0}G", new Rectangle(detail.X + 20, detail.Y + 180, detail.Width - 40, 48), CardUi.GreenDark, 0.82f);
        }
        else
        {
            CardUi.CenterText(b, Game1.dialogueFont, "카드를 선택하세요", new Rectangle(detail.X + 20, detail.Y + 80, detail.Width - 40, 80), CardUi.Muted, 0.80f);
        }

        int available = string.IsNullOrWhiteSpace(SelectedKey) ? 0 : CardShopRules.GetListableCount(Mod, SelectedKey);
        CardUi.Button(b, List, TargetSlot >= 0 ? $"{TargetSlot + 1}번에 진열" : $"판매 진열  {available}장 가능", available > 0, true);
        var bonus = Mod.Core.GetNextCollectionBonus();
        CardUi.Button(b, Bonus, bonus.Complete ? "보너스 완료" : $"보너스 {bonus.Required}종 → 팩 {bonus.Reward}", !bonus.Complete, bonus.CanClaim);
        CardUi.Button(b, Shelf, "판매대");
        CardUi.Button(b, Back, "뒤로");
        CardUi.Button(b, Prev, "이전", Page > 0);
        CardUi.Button(b, Next, "다음", Page < maxPage);
        CardUi.CenterText(b, Game1.smallFont, $"{Page + 1}/{maxPage + 1}", new Rectangle(xPositionOnScreen + 430, yPositionOnScreen + height - 62, 120, 42), CardUi.Muted, 1.10f);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 820, yPositionOnScreen + 680, 365, 34), CardUi.Muted, 0.98f);
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
        Rectangle r = CardUi.Center(1220, 730);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int sx = r.X + 55;
        int sy = r.Y + 160;
        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Slots.Add(new Rectangle(sx + col * 275, sy + row * 195, 250, 170));
        }

        Add = new Rectangle(r.X + 60, r.Bottom - 66, 220, 44);
        Down = new Rectangle(r.X + 305, r.Bottom - 66, 145, 44);
        Up = new Rectangle(r.X + 465, r.Bottom - 66, 145, 44);
        Remove = new Rectangle(r.X + 625, r.Bottom - 66, 165, 44);
        Back = new Rectangle(r.Right - 160, r.Bottom - 66, 110, 44);
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
                CardUi.CenterText(b, Game1.dialogueFont, "+", new Rectangle(r.X + 10, r.Y + 24, r.Width - 20, 60), CardUi.Muted, 1.0f);
                CardUi.CenterText(b, Game1.smallFont, $"{i + 1}번", new Rectangle(r.X + 10, r.Y + 105, r.Width - 20, 32), CardUi.Muted, 1.10f);
                continue;
            }

            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out _, out _))
                continue;
            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null)
                continue;

            CardUi.CenterText(b, Game1.dialogueFont, card.Name, new Rectangle(r.X + 12, r.Y + 24, r.Width - 24, 55), CardUi.Ink, 0.82f);
            CardUi.CenterText(b, Game1.dialogueFont, $"{listing.Price:N0}G", new Rectangle(r.X + 12, r.Y + 92, r.Width - 24, 45), CardUi.GreenDark, 0.80f);
        }

        SaleListing? selected = Mod.Core.GetListingAtSlot(SelectedSlot);
        string selectedInfo = "빈 슬롯";
        if (selected is not null && CardKeys.TryParse(selected.CollectionKey, out string key, out string variant, out string condition))
        {
            CardDefinition? card = Mod.FindCard(key);
            if (card is not null)
                selectedInfo = $"{card.Name} · {ModEntry.VariantName(variant)} · {condition} · 판매확률 {Mod.Core.GetSaleChance(selected) * 100:0}%";
        }

        CardUi.CenterText(b, Game1.smallFont, selectedInfo, new Rectangle(xPositionOnScreen + 60, yPositionOnScreen + 555, width - 120, 38), CardUi.Ink, 1.08f);
        CardUi.Button(b, Add, selected is null ? $"{SelectedSlot + 1}번 카드 넣기" : "슬롯 사용 중", selected is null, true);
        CardUi.Button(b, Down, "가격 -50", selected is not null);
        CardUi.Button(b, Up, "가격 +50", selected is not null);
        CardUi.Button(b, Remove, "회수", selected is not null);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 815, yPositionOnScreen + height - 66, 220, 44), CardUi.Muted, 0.96f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }
}
