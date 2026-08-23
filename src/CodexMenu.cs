using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace WatermelonGeneticsCore;

public sealed class CodexMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private int Selected;
    private readonly Rectangle PrevButton;
    private readonly Rectangle NextButton;

    public CodexMenu(ModEntry mod)
        : base(Game1.viewport.Width / 2 - 430, Game1.viewport.Height / 2 - 285, 860, 570, true)
    {
        Mod = mod;
        Mod.RefreshDiscoveries();
        PrevButton = new Rectangle(xPositionOnScreen + 35, yPositionOnScreen + height - 90, 80, 50);
        NextButton = new Rectangle(xPositionOnScreen + width - 115, yPositionOnScreen + height - 90, 80, 50);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            exitThisMenu();
            return;
        }
        if (PrevButton.Contains(x, y))
        {
            Selected = (Selected - 1 + Mod.Varieties.Count) % Mod.Varieties.Count;
            Game1.playSound("shwip");
        }
        else if (NextButton.Contains(x, y))
        {
            Selected = (Selected + 1) % Mod.Varieties.Count;
            Game1.playSound("shwip");
        }
    }

    public override void draw(SpriteBatch b)
    {
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        string title = Mod.Helper.Translation.Get("codex.title");
        b.DrawString(Game1.dialogueFont, title, new Vector2(xPositionOnScreen + 55, yPositionOnScreen + 38), Game1.textColor);

        if (Mod.Varieties.Count > 0)
        {
            VarietyDefinition v = Mod.Varieties[Math.Clamp(Selected, 0, Mod.Varieties.Count - 1)];
            bool discovered = Mod.State.Discovered.Contains(v.Key);
            DrawVariety(b, v, discovered);
        }

        DrawNav(b, PrevButton, "◀");
        DrawNav(b, NextButton, "▶");
        string count = $"발견 {Mod.State.Discovered.Count(k => Mod.Varieties.Any(v => v.Key == k))} / {Mod.Varieties.Count}";
        b.DrawString(Game1.smallFont, count, new Vector2(xPositionOnScreen + width / 2 - 45, yPositionOnScreen + height - 75), Game1.textColor);
        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawVariety(SpriteBatch b, VarietyDefinition v, bool discovered)
    {
        Rectangle iconBox = new Rectangle(xPositionOnScreen + 70, yPositionOnScreen + 135, 230, 230);
        drawTextureBox(b, iconBox.X, iconBox.Y, iconBox.Width, iconBox.Height, Color.White);

        if (discovered)
        {
            ParsedItemData? data = ItemRegistry.GetData(v.FruitId);
            if (data is not null)
            {
                Rectangle dest = new Rectangle(iconBox.Center.X - 64, iconBox.Center.Y - 64, 128, 128);
                b.Draw(data.GetTexture(), dest, data.GetSourceRect(), Color.White);
            }
        }
        else
        {
            string q = "?";
            Vector2 qSize = Game1.dialogueFont.MeasureString(q);
            b.DrawString(Game1.dialogueFont, q, new Vector2(iconBox.Center.X - qSize.X / 2, iconBox.Center.Y - qSize.Y / 2), Game1.textColor);
        }

        int tx = xPositionOnScreen + 350;
        int ty = yPositionOnScreen + 135;
        string name = discovered ? Mod.Helper.Translation.Get(v.NameKey) : Mod.Helper.Translation.Get("codex.locked");
        b.DrawString(Game1.dialogueFont, name, new Vector2(tx, ty), Game1.textColor);
        if (!discovered)
            return;

        DrawTrait(b, Mod.Helper.Translation.Get("trait.sweetness"), v.Sweetness, tx, ty + 85);
        DrawTrait(b, Mod.Helper.Translation.Get("trait.size"), v.Size, tx, ty + 130);
        DrawTrait(b, Mod.Helper.Translation.Get("trait.growth"), v.Growth, tx, ty + 175);
        DrawTrait(b, Mod.Helper.Translation.Get("trait.resistance"), v.Resistance, tx, ty + 220);
        DrawTrait(b, Mod.Helper.Translation.Get("trait.rarity"), v.Rarity, tx, ty + 265);

        if (Mod.State.Records.TryGetValue(v.Key, out VarietyRecord? record))
        {
            b.DrawString(Game1.smallFont, $"교배 결과 {record.TimesBred}회  ·  발견 기록 {record.TimesFound}회", new Vector2(tx, ty + 335), Game1.textColor);
        }
    }

    private static void DrawTrait(SpriteBatch b, string label, int stars, int x, int y)
    {
        string line = $"{label,-8}  {new string('★', Math.Clamp(stars, 0, 6))}{new string('☆', Math.Max(0, 6 - stars))}";
        b.DrawString(Game1.smallFont, line, new Vector2(x, y), Game1.textColor);
    }

    private static void DrawNav(SpriteBatch b, Rectangle rect, string text)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, Color.White);
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(rect.Center.X - size.X / 2, rect.Center.Y - size.Y / 2), Game1.textColor);
    }
}
