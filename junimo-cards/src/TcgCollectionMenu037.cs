using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

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
