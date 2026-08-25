using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

// Entry menu kept under the 0.3.5 class name for save/runtime compatibility,
// but its layout and navigation are the v0.3.6 balance pass.
internal sealed class ReadableCardShopMenu035 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly Rectangle Pack;
    private readonly Rectangle Collection;
    private readonly Rectangle Shelf;
    private readonly Rectangle Close;

    internal ReadableCardShopMenu035(ModEntry mod)
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
        int tileY = yPositionOnScreen + 214;
        Pack = new Rectangle(xPositionOnScreen + margin, tileY, tileW, 132);
        Collection = new Rectangle(Pack.Right + gap, tileY, tileW, 132);
        Shelf = new Rectangle(Collection.Right + gap, tileY, tileW, 132);
        Close = new Rectangle(xPositionOnScreen + width - 132, yPositionOnScreen + height - 48, 96, 36);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pack.Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new BalancedPackMenu036(Mod, this);
            return;
        }
        if (Collection.Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new BalancedCollectionMenu036(Mod, this);
            return;
        }
        if (Shelf.Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new BalancedShelfMenu036(Mod, this);
            return;
        }
        if (Close.Contains(x, y))
            exitThisMenu();
    }

    public override void draw(SpriteBatch b)
    {
        Mod.EnsureState();
        BalancedUi036.Begin(b, this, "주니모 카드샵", "TEST · 6개 등급 모두 16.67%");

        int unique = Mod.Core.UniqueCardCount();
        Rectangle stats = new(xPositionOnScreen + 34, yPositionOnScreen + 106, width - 68, 90);
        CardUi.Panel(b, stats);
        int cell = stats.Width / 4;
        DrawStat(b, new Rectangle(stats.X, stats.Y, cell, stats.Height), "골드", $"{Game1.player.Money:N0}G");
        DrawStat(b, new Rectangle(stats.X + cell, stats.Y, cell, stats.Height), "미개봉", $"{Mod.State.UnopenedPacks}팩");
        DrawStat(b, new Rectangle(stats.X + cell * 2, stats.Y, cell, stats.Height), "컬렉션", $"{unique}/{Mod.Cards.Count}");
        DrawStat(b, new Rectangle(stats.X + cell * 3, stats.Y, stats.Width - cell * 3, stats.Height), "매출", $"{Mod.State.LifetimeCardRevenue:N0}G");

        DrawTile(b, Pack, "팩 구매", $"1팩 {Mod.Config.PackPrice:N0}G");
        DrawTile(b, Collection, "컬렉션", $"수집 {unique}/{Mod.Cards.Count}");
        DrawTile(b, Shelf, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots}");

        Rectangle today = new(xPositionOnScreen + 34, yPositionOnScreen + 360, width - 68, 64);
        CardUi.Panel(b, today);
        CardUi.CenterText(b, Game1.smallFont, "오늘", new Rectangle(today.X + 10, today.Y + 10, 82, 44), CardUi.Ink, 1.16f);
        string summary = $"손님 {Mod.State.LastCustomerCount}명   판매 {Mod.State.LastCardsSold}장   +{Mod.State.LastDailyRevenue:N0}G";
        CardUi.CenterText(b, Game1.dialogueFont, summary, new Rectangle(today.X + 94, today.Y + 8, today.Width - 106, 48), CardUi.GreenDark, 0.82f);

        BalancedUi036.MiniButton(b, Close, "닫기");
        drawMouse(b);
    }

    private static void DrawStat(SpriteBatch b, Rectangle r, string label, string value)
    {
        CardUi.CenterText(b, Game1.smallFont, label, new Rectangle(r.X + 5, r.Y + 8, r.Width - 10, 27), CardUi.Muted, 1.16f);
        CardUi.CenterText(b, Game1.dialogueFont, value, new Rectangle(r.X + 5, r.Y + 35, r.Width - 10, 46), CardUi.Ink, 0.82f);
    }

    private static void DrawTile(SpriteBatch b, Rectangle r, string title, string sub)
    {
        CardUi.Panel(b, r);
        CardUi.CenterText(b, Game1.dialogueFont, title, new Rectangle(r.X + 12, r.Y + 19, r.Width - 24, 50), CardUi.GreenDark, 0.88f);
        CardUi.CenterText(b, Game1.smallFont, sub, new Rectangle(r.X + 12, r.Y + 80, r.Width - 24, 30), CardUi.Ink, 1.12f);
    }
}
