using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace WatermelonGeneticsCore;

public sealed class HybridizerMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly string LocationName;
    private readonly int TileX;
    private readonly int TileY;
    private readonly Rectangle SlotA;
    private readonly Rectangle SlotB;
    private readonly Rectangle StartButton;
    private VarietyDefinition? ParentA;
    private VarietyDefinition? ParentB;

    public HybridizerMenu(ModEntry mod, string locationName, int tileX, int tileY)
        : base(Game1.viewport.Width / 2 - 360, Game1.viewport.Height / 2 - 260, 720, 520, true)
    {
        Mod = mod;
        LocationName = locationName;
        TileX = tileX;
        TileY = tileY;
        SlotA = new Rectangle(xPositionOnScreen + 95, yPositionOnScreen + 155, 128, 128);
        SlotB = new Rectangle(xPositionOnScreen + width - 223, yPositionOnScreen + 155, 128, 128);
        StartButton = new Rectangle(xPositionOnScreen + width / 2 - 105, yPositionOnScreen + height - 105, 210, 64);

        IReadOnlyList<VarietyDefinition> owned = Mod.GetOwnedSeedVarieties();
        ParentA = owned.FirstOrDefault();
        ParentB = owned.Skip(1).FirstOrDefault() ?? owned.FirstOrDefault();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            exitThisMenu();
            return;
        }
        if (SlotA.Contains(x, y))
        {
            ParentA = NextOwned(ParentA);
            Game1.playSound("shwip");
            return;
        }
        if (SlotB.Contains(x, y))
        {
            ParentB = NextOwned(ParentB);
            Game1.playSound("shwip");
            return;
        }
        if (StartButton.Contains(x, y))
        {
            if (ParentA is null || ParentB is null)
            {
                Game1.addHUDMessage(new HUDMessage(Mod.Helper.Translation.Get("hybridizer.need_two"), HUDMessage.error_type));
                return;
            }
            if (!Mod.TryStartHybrid(LocationName, TileX, TileY, ParentA, ParentB, out _))
            {
                Game1.addHUDMessage(new HUDMessage(Mod.Helper.Translation.Get("hybridizer.need_items"), HUDMessage.error_type));
                return;
            }
            Game1.playSound("discoverMineral");
            Game1.addHUDMessage(new HUDMessage(Mod.Helper.Translation.Get("hybridizer.started")));
            exitThisMenu();
        }
    }

    private VarietyDefinition? NextOwned(VarietyDefinition? current)
    {
        IReadOnlyList<VarietyDefinition> owned = Mod.GetOwnedSeedVarieties();
        if (owned.Count == 0)
            return null;
        int index = current is null ? -1 : owned.ToList().FindIndex(v => v.Key == current.Key);
        return owned[(index + 1 + owned.Count) % owned.Count];
    }

    public override void draw(SpriteBatch b)
    {
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        string title = Mod.Helper.Translation.Get("hybridizer.title");
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        b.DrawString(Game1.dialogueFont, title, new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 40), Game1.textColor);

        DrawSlot(b, SlotA, ParentA, Mod.Helper.Translation.Get("hybridizer.parent_a"));
        DrawSlot(b, SlotB, ParentB, Mod.Helper.Translation.Get("hybridizer.parent_b"));

        b.DrawString(Game1.dialogueFont, "×", new Vector2(xPositionOnScreen + width / 2 - 12, yPositionOnScreen + 195), Game1.textColor);

        int chance = ParentA is null || ParentB is null ? 0 : GeneticsEngine.GetSuccessChance(ParentA, ParentB);
        string chanceText = $"예상 성공률  {chance}%";
        Vector2 chanceSize = Game1.smallFont.MeasureString(chanceText);
        b.DrawString(Game1.smallFont, chanceText, new Vector2(xPositionOnScreen + width / 2 - chanceSize.X / 2, yPositionOnScreen + 320), Game1.textColor);
        string resultText = $"예상 결과  {Mod.Helper.Translation.Get("hybridizer.result_unknown")}";
        Vector2 resultSize = Game1.smallFont.MeasureString(resultText);
        b.DrawString(Game1.smallFont, resultText, new Vector2(xPositionOnScreen + width / 2 - resultSize.X / 2, yPositionOnScreen + 355), Game1.textColor);

        drawTextureBox(b, StartButton.X, StartButton.Y, StartButton.Width, StartButton.Height, Color.White);
        string start = Mod.Helper.Translation.Get("hybridizer.start");
        Vector2 startSize = Game1.smallFont.MeasureString(start);
        b.DrawString(Game1.smallFont, start, new Vector2(StartButton.Center.X - startSize.X / 2, StartButton.Center.Y - startSize.Y / 2), Game1.textColor);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawSlot(SpriteBatch b, Rectangle rect, VarietyDefinition? variety, string label)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, Color.White);
        Vector2 labelSize = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label, new Vector2(rect.Center.X - labelSize.X / 2, rect.Y - 35), Game1.textColor);
        if (variety is null)
            return;
        ParsedItemData? data = ItemRegistry.GetData(variety.SeedId);
        if (data is not null)
        {
            Rectangle dest = new Rectangle(rect.Center.X - 32, rect.Center.Y - 40, 64, 64);
            b.Draw(data.GetTexture(), dest, data.GetSourceRect(), Color.White);
        }
        string name = Mod.Helper.Translation.Get(variety.NameKey);
        Vector2 size = Game1.smallFont.MeasureString(name);
        b.DrawString(Game1.smallFont, name, new Vector2(rect.Center.X - size.X / 2, rect.Bottom - 38), Game1.textColor);
    }
}
