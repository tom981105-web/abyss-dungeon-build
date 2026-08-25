using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

internal sealed class TcgPackMenu037 : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle BuyOne;
    private readonly Rectangle BuyFive;
    private readonly Rectangle Open;
    private readonly Rectangle Back;
    private string Message = "TEST · 모든 등급 16.67%";

    internal TcgPackMenu037(ModEntry mod, IClickableMenu returnMenu)
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
        BuyOne = new Rectangle(r.X + 38, buttonY, buttonW, 54);
        BuyFive = new Rectangle(BuyOne.Right + gap, buttonY, buttonW, 54);
        Open = new Rectangle(BuyFive.Right + gap, buttonY, buttonW, 54);
        Back = new Rectangle(r.Right - 132, r.Bottom - 48, 98, 36);
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
            Game1.activeClickableMenu = new TcgPackOpeningMenu037(Mod, this);
            return;
        }
        if (Back.Contains(x, y))
            Game1.activeClickableMenu = ReturnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        TcgUi037.Begin(b, this, "팩 구매", "Junimo Cards · 부스터 팩");

        Rectangle pack = new(xPositionOnScreen + 54, yPositionOnScreen + 114, 178, 226);
        TcgVisuals037.DrawBoosterPack(b, pack);

        Rectangle info = new(pack.Right + 22, yPositionOnScreen + 114,
            width - 54 - (pack.Right + 22 - xPositionOnScreen), 226);
        CardUi.Panel(b, info);

        CardUi.CenterText(b, Game1.dialogueFont, $"보유 {Mod.State.UnopenedPacks}팩",
            new Rectangle(info.X + 16, info.Y + 14, info.Width - 32, 50), CardUi.Ink, 0.96f);
        CardUi.CenterText(b, Game1.smallFont, "커먼 · 언커먼 · 레어",
            new Rectangle(info.X + 16, info.Y + 76, info.Width - 32, 32), CardUi.GreenDark, 1.30f);
        CardUi.CenterText(b, Game1.smallFont, "에픽 · 레전더리 · 시크릿",
            new Rectangle(info.X + 16, info.Y + 112, info.Width - 32, 32), CardUi.GreenDark, 1.30f);
        CardUi.CenterText(b, Game1.dialogueFont, "각 16.67%",
            new Rectangle(info.X + 16, info.Y + 158, info.Width - 32, 44), TcgUi037.TestGold, 0.90f);

        TcgUi037.Button(b, BuyOne, $"1팩 {Mod.Config.PackPrice:N0}G", Game1.player.Money >= Mod.Config.PackPrice);
        TcgUi037.Button(b, BuyFive, $"5팩 {Mod.Config.FivePackPrice:N0}G", Game1.player.Money >= Mod.Config.FivePackPrice);
        TcgUi037.Button(b, Open, $"개봉 {Mod.State.UnopenedPacks}팩", Mod.State.UnopenedPacks > 0, true);
        CardUi.CenterText(b, Game1.smallFont, Message,
            new Rectangle(xPositionOnScreen + 40, BuyOne.Bottom + 5, width - 185, 32), CardUi.Muted, 1.28f);
        TcgUi037.MiniButton(b, Back, "뒤로");
        drawMouse(b);
    }
}
