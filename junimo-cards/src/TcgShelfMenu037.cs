using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

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
