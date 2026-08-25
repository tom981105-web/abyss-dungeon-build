using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

// v0.3.5: rebuild the two most-used screens around a 1024x768-style viewport.
// The previous pass filled the safe viewport too aggressively and left a large empty lower half.
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
        Rectangle r = CardUi.Center(940, 555);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int margin = 36;
        int gap = 18;
        int tileW = (width - margin * 2 - gap * 2) / 3;
        int tileY = yPositionOnScreen + 226;
        Pack = new Rectangle(xPositionOnScreen + margin, tileY, tileW, 148);
        Collection = new Rectangle(Pack.Right + gap, tileY, tileW, 148);
        Shelf = new Rectangle(Collection.Right + gap, tileY, tileW, 148);
        Close = new Rectangle(xPositionOnScreen + width - 150, yPositionOnScreen + height - 56, 110, 40);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Pack.Contains(x, y))
        {
            Game1.activeClickableMenu = new ReadablePackMenu035(Mod, this);
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
        DrawShell(b, "주니모 카드샵", "TEST · 6개 등급 모두 16.67%");

        int unique = Mod.Core.UniqueCardCount();
        Rectangle stats = new(xPositionOnScreen + 36, yPositionOnScreen + 112, width - 72, 94);
        CardUi.Panel(b, stats);
        int cell = stats.Width / 4;
        DrawStat(b, new Rectangle(stats.X, stats.Y, cell, stats.Height), "골드", $"{Game1.player.Money:N0}G");
        DrawStat(b, new Rectangle(stats.X + cell, stats.Y, cell, stats.Height), "미개봉", $"{Mod.State.UnopenedPacks}팩");
        DrawStat(b, new Rectangle(stats.X + cell * 2, stats.Y, cell, stats.Height), "컬렉션", $"{unique}/{Mod.Cards.Count}");
        DrawStat(b, new Rectangle(stats.X + cell * 3, stats.Y, stats.Width - cell * 3, stats.Height), "매출", $"{Mod.State.LifetimeCardRevenue:N0}G");

        DrawTile(b, Pack, "팩 구매", $"1팩 {Mod.Config.PackPrice:N0}G");
        DrawTile(b, Collection, "컬렉션", $"수집 {unique}/{Mod.Cards.Count}");
        DrawTile(b, Shelf, "판매 진열대", $"진열 {Mod.State.SaleShelf.Count}/{Mod.Config.SaleShelfSlots}");

        Rectangle today = new(xPositionOnScreen + 36, yPositionOnScreen + 394, width - 72, 72);
        CardUi.Panel(b, today);
        CardUi.CenterText(b, Game1.smallFont, "오늘", new Rectangle(today.X + 12, today.Y + 12, 90, 48), CardUi.Ink, 1.45f);
        string summary = $"손님 {Mod.State.LastCustomerCount}명   판매 {Mod.State.LastCardsSold}장   +{Mod.State.LastDailyRevenue:N0}G";
        CardUi.CenterText(b, Game1.dialogueFont, summary, new Rectangle(today.X + 105, today.Y + 8, today.Width - 120, 56), CardUi.GreenDark, 0.92f);

        CardUi.Button(b, Close, "닫기");
        drawMouse(b);
    }

    private void DrawShell(SpriteBatch b, string title, string subtitle)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.50f);
        IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        Rectangle header = new(xPositionOnScreen + 26, yPositionOnScreen + 20, width - 52, 78);
        IClickableMenu.drawTextureBox(b, header.X, header.Y, header.Width, header.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(header.X + 7, header.Y + 7, header.Width - 14, header.Height - 14), CardUi.GreenDark);
        CardUi.CenterText(b, Game1.dialogueFont, title, new Rectangle(header.X + 15, header.Y + 4, header.Width - 30, 43), Color.White, 1.10f);
        CardUi.CenterText(b, Game1.smallFont, subtitle, new Rectangle(header.X + 15, header.Y + 47, header.Width - 30, 24), new Color(244, 229, 145), 1.22f);
    }

    private static void DrawStat(SpriteBatch b, Rectangle r, string label, string value)
    {
        CardUi.CenterText(b, Game1.smallFont, label, new Rectangle(r.X + 6, r.Y + 8, r.Width - 12, 30), CardUi.Muted, 1.35f);
        CardUi.CenterText(b, Game1.dialogueFont, value, new Rectangle(r.X + 6, r.Y + 38, r.Width - 12, 48), CardUi.Ink, 0.94f);
    }

    private static void DrawTile(SpriteBatch b, Rectangle r, string title, string sub)
    {
        CardUi.Panel(b, r);
        CardUi.CenterText(b, Game1.dialogueFont, title, new Rectangle(r.X + 14, r.Y + 22, r.Width - 28, 56), CardUi.GreenDark, 1.00f);
        CardUi.CenterText(b, Game1.smallFont, sub, new Rectangle(r.X + 14, r.Y + 91, r.Width - 28, 34), CardUi.Ink, 1.36f);
    }
}

internal sealed class ReadablePackMenu035 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle BuyOne;
    private readonly Rectangle BuyFive;
    private readonly Rectangle Open;
    private readonly Rectangle Back;
    private string Message = "테스트 모드 · 모든 등급 16.67%";

    internal ReadablePackMenu035(ModEntry mod, IClickableMenu returnMenu)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Rectangle r = CardUi.Center(940, 555);
        xPositionOnScreen = r.X;
        yPositionOnScreen = r.Y;
        width = r.Width;
        height = r.Height;

        int buttonY = yPositionOnScreen + 380;
        int buttonW = 245;
        BuyOne = new Rectangle(xPositionOnScreen + 55, buttonY, buttonW, 62);
        BuyFive = new Rectangle(xPositionOnScreen + (width - buttonW) / 2, buttonY, buttonW, 62);
        Open = new Rectangle(xPositionOnScreen + width - 55 - buttonW, buttonY, buttonW, 62);
        Back = new Rectangle(xPositionOnScreen + width - 150, yPositionOnScreen + height - 56, 110, 40);
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
        DrawShell(b);

        Rectangle pack = new(xPositionOnScreen + 70, yPositionOnScreen + 125, 190, 230);
        CardUi.DrawCardBack(b, pack);

        Rectangle info = new(xPositionOnScreen + 300, yPositionOnScreen + 125, width - 370, 230);
        CardUi.Panel(b, info);
        CardUi.CenterText(b, Game1.dialogueFont, $"보유 {Mod.State.UnopenedPacks}팩", new Rectangle(info.X + 20, info.Y + 18, info.Width - 40, 58), CardUi.Ink, 1.03f);
        CardUi.CenterText(b, Game1.smallFont, "테스트 확률", new Rectangle(info.X + 20, info.Y + 84, info.Width - 40, 34), CardUi.Muted, 1.30f);
        CardUi.CenterText(b, Game1.dialogueFont, "커먼 · 언커먼 · 레어 · 에픽 · 레전더리 · 시크릿", new Rectangle(info.X + 18, info.Y + 116, info.Width - 36, 48), CardUi.GreenDark, 0.78f);
        CardUi.CenterText(b, Game1.dialogueFont, "각 16.67%", new Rectangle(info.X + 20, info.Y + 168, info.Width - 40, 42), CardUi.Gold, 0.90f);

        CardUi.Button(b, BuyOne, $"1팩  {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        CardUi.Button(b, BuyFive, $"5팩  {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        CardUi.Button(b, Open, $"개봉  {Mod.State.UnopenedPacks}팩", Mod.State.UnopenedPacks > 0, true);
        CardUi.CenterText(b, Game1.smallFont, Message, new Rectangle(xPositionOnScreen + 55, yPositionOnScreen + 452, width - 230, 38), CardUi.Muted, 1.18f);
        CardUi.Button(b, Back, "뒤로");
        drawMouse(b);
    }

    private void DrawShell(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.50f);
        IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        Rectangle header = new(xPositionOnScreen + 26, yPositionOnScreen + 20, width - 52, 82);
        IClickableMenu.drawTextureBox(b, header.X, header.Y, header.Width, header.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(header.X + 7, header.Y + 7, header.Width - 14, header.Height - 14), CardUi.GreenDark);
        CardUi.CenterText(b, Game1.dialogueFont, "팩 구매", new Rectangle(header.X + 15, header.Y + 4, header.Width - 30, 45), Color.White, 1.12f);
        CardUi.CenterText(b, Game1.smallFont, "Pelican Origins · 테스트 확률 적용 중", new Rectangle(header.X + 15, header.Y + 49, header.Width - 30, 25), new Color(244, 229, 145), 1.20f);
    }
}
