using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed class ProductExpansionUi
{
    private readonly ModEntry Mod;

    internal ProductExpansionUi(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        Mod.Helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private Rectangle CatalogButton()
    {
        int w = Math.Min(1440, Math.Max(960, Game1.viewport.Width - 28));
        int h = Math.Min(930, Math.Max(690, Game1.viewport.Height - 28));
        int x = Game1.viewport.Width / 2 - w / 2;
        int y = Game1.viewport.Height / 2 - h / 2;
        return new Rectangle(x + w - 405, y + 24, 165, 34);
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Game1.activeClickableMenu is not Production2Menu)
            return;

        Rectangle rect = CatalogButton();
        IClickableMenu.drawTextureBox(e.SpriteBatch, rect.X, rect.Y, rect.Width, rect.Height, new Color(51, 97, 73));
        CenterText(e.SpriteBatch, Game1.smallFont, "제품 카탈로그", rect, Color.White);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || e.Button != SButton.MouseLeft || Game1.activeClickableMenu is not Production2Menu)
            return;
        if (!CatalogButton().Contains(Game1.getMouseX(), Game1.getMouseY()))
            return;

        Mod.Helper.Input.Suppress(e.Button);
        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new ProductCatalogMenu(Mod);
    }

    private static void CenterText(SpriteBatch b, SpriteFont font, string text, Rectangle rect, Color color)
    {
        Vector2 size = font.MeasureString(text);
        b.DrawString(font, text, new Vector2(rect.X + rect.Width / 2f - size.X / 2f, rect.Y + rect.Height / 2f - size.Y / 2f), color);
    }
}

internal sealed class ProductCatalogMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private int Page;
    private int Filter; // 0 all, 1 intermediate, 2 finished
    private string Message = "각 생산품 아이콘은 포장 형태 + 실제 원재료 스프라이트를 합성해 표시합니다.";
    private Rectangle Panel;

    private static readonly Color Wood = new(82, 53, 31);
    private static readonly Color WoodDark = new(54, 37, 25);
    private static readonly Color Paper = new(248, 235, 199);
    private static readonly Color PaperAlt = new(238, 221, 180);
    private static readonly Color Green = new(49, 89, 53);
    private static readonly Color Green2 = new(72, 116, 70);
    private static readonly Color Blue = new(48, 92, 126);
    private static readonly Color Muted = new(104, 84, 59);
    private static readonly Color Disabled = new(147, 139, 116);
    private static readonly Color Gold = new(190, 139, 43);

    internal ProductCatalogMenu(ModEntry mod)
        : base(0, 0, Game1.viewport.Width, Game1.viewport.Height, true)
    {
        Mod = mod;
        Mod.Production.EnsureState();
        Recalculate();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Recalculate();
    }

    private void Recalculate()
    {
        int w = Math.Min(1180, Math.Max(900, Game1.viewport.Width - 80));
        int h = Math.Min(860, Math.Max(650, Game1.viewport.Height - 70));
        Panel = new Rectangle(Game1.viewport.Width / 2 - w / 2, Game1.viewport.Height / 2 - h / 2, w, h);
        initializeUpperRightCloseButton();
        if (upperRightCloseButton is not null)
        {
            upperRightCloseButton.bounds.X = Panel.Right - upperRightCloseButton.bounds.Width - 10;
            upperRightCloseButton.bounds.Y = Panel.Y + 10;
        }
    }

    private List<ProductionRecipeDefinition> Rows()
    {
        IEnumerable<ProductionRecipeDefinition> query = Mod.Production.GetCatalogRecipes(true);
        if (Filter == 1)
            query = query.Where(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        else if (Filter == 2)
            query = query.Where(p => !string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        return query.ToList();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            Game1.activeClickableMenu = new Production2Menu(Mod);
            return;
        }
        if (BackButton().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.activeClickableMenu = new Production2Menu(Mod);
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            if (FilterButton(i).Contains(x, y))
            {
                Filter = i;
                Page = 0;
                Game1.playSound("smallSelect");
                return;
            }
        }

        List<ProductionRecipeDefinition> rows = Rows();
        int start = Page * 8;
        for (int row = 0; row < 8; row++)
        {
            int index = start + row;
            if (index >= rows.Count)
                break;
            ProductionRecipeDefinition recipe = rows[index];
            if (OneBatchButton(row).Contains(x, y))
            {
                if (!Mod.Production.IsRecipeUnlocked(recipe, out string reason))
                {
                    Message = reason;
                    Game1.playSound("cancel");
                    return;
                }
                bool ok = Mod.Production.TryStart(recipe.Key, 1, out string message);
                Message = message;
                Game1.playSound(ok ? "Ship" : "cancel");
                return;
            }
            if (MaxBatchButton(row).Contains(x, y))
            {
                if (!Mod.Production.IsRecipeUnlocked(recipe, out string reason))
                {
                    Message = reason;
                    Game1.playSound("cancel");
                    return;
                }
                int max = Math.Min(10, Mod.Production.GetMaxBatches(recipe));
                if (max <= 0)
                {
                    Message = $"{Mod.Production.GetIngredientDisplayName(recipe)} 재고가 부족합니다.";
                    Game1.playSound("cancel");
                    return;
                }
                bool ok = Mod.Production.TryStart(recipe.Key, max, out string message);
                Message = message;
                Game1.playSound(ok ? "Ship" : "cancel");
                return;
            }
        }

        int maxPage = Math.Max(0, (rows.Count - 1) / 8);
        if (PrevButton().Contains(x, y) && Page > 0)
        {
            Page--;
            Game1.playSound("shwip");
        }
        else if (NextButton().Contains(x, y) && Page < maxPage)
        {
            Page++;
            Game1.playSound("shwip");
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int maxPage = Math.Max(0, (rows.Count - 1) / 8);
        if (direction < 0 && Page < maxPage) Page++;
        else if (direction > 0 && Page > 0) Page--;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.72f);
        drawTextureBox(b, Panel.X, Panel.Y, Panel.Width, Panel.Height, Paper);

        Rectangle header = new(Panel.X + 8, Panel.Y + 8, Panel.Width - 16, 65);
        b.Draw(Game1.fadeToBlackRect, header, Wood);
        b.DrawString(Game1.dialogueFont, "Production 2.4 · 생산품 아이콘 카탈로그", new Vector2(header.X + 22, header.Y + 15), new Color(247, 226, 164));
        DrawButton(b, BackButton(), "생산 관리", Green2);

        for (int i = 0; i < 3; i++)
        {
            string text = i switch { 1 => "중간재", 2 => "완제품", _ => "전체" };
            DrawButton(b, FilterButton(i), text, Filter == i ? Green : new Color(142, 112, 70));
        }

        List<ProductionRecipeDefinition> rows = Rows();
        int start = Page * 8;
        for (int row = 0; row < 8; row++)
        {
            Rectangle rect = RowRect(row);
            b.Draw(Game1.fadeToBlackRect, rect, row % 2 == 0 ? PaperAlt : new Color(244, 229, 194));
            int index = start + row;
            if (index >= rows.Count)
                continue;

            ProductionRecipeDefinition recipe = rows[index];
            bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason);
            bool intermediate = string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase);
            string kind = intermediate ? "중간재" : "완제품";
            string family = recipe.ProductFamily switch
            {
                "Tomato" => "토마토",
                "Watermelon" => "수박",
                "KoreanMelon" => "참외",
                "NapaCabbage" => "배추",
                "VanillaFlower" => "꽃",
                "VanillaVegetable" => "채소",
                "VanillaFruit" => "과일",
                "VanillaGrain" => "곡물",
                "VanillaBeverage" => "음료원료",
                "VanillaSpecial" => "특수",
                _ => "기타"
            };
            string line = recipe.LineType switch { "Packaging" => "포장", "Fermentation" => "발효", _ => "음료" };
            string ingredient = Mod.Production.GetIngredientDisplayName(recipe);
            int have = Mod.Production.GetIngredientQuantity(recipe);
            int max = Mod.Production.GetMaxBatches(recipe);

            Rectangle icon = new(rect.X + 8, rect.Y + 5, 58, 58);
            Mod.Icons.DrawRecipeIcon(b, recipe, icon, unlocked ? 1f : 0.48f);

            b.DrawString(Game1.dialogueFont, recipe.DisplayName, new Vector2(rect.X + 76, rect.Y + 7), unlocked ? WoodDark : Disabled);
            b.DrawString(Game1.smallFont, $"[{kind}] {family} · {line} 라인", new Vector2(rect.X + 78, rect.Y + 42), intermediate ? Blue : Green);
            b.DrawString(Game1.smallFont, $"{ingredient} {recipe.InputQuantity} → {recipe.OutputQuantity}{recipe.OutputUnit} · 재고 {have} · 최대 {max}배치", new Vector2(rect.X + 330, rect.Y + 16), Muted);
            b.DrawString(Game1.smallFont, unlocked ? $"Lv.{recipe.RequiredCompanyLevel} / 브랜드 {recipe.RequiredBrandPoints}" : reason, new Vector2(rect.X + 330, rect.Y + 43), unlocked ? Gold : Disabled);

            DrawButton(b, OneBatchButton(row), "1배치", unlocked ? Green2 : Disabled);
            DrawButton(b, MaxBatchButton(row), "최대", unlocked ? Blue : Disabled);
        }

        int maxPage = Math.Max(0, (rows.Count - 1) / 8);
        DrawButton(b, PrevButton(), "◀ 이전", Page > 0 ? Green2 : Disabled);
        DrawButton(b, NextButton(), "다음 ▶", Page < maxPage ? Green2 : Disabled);
        b.DrawString(Game1.smallFont, $"{rows.Count}개 레시피 · {Page + 1}/{maxPage + 1}", new Vector2(Panel.X + Panel.Width / 2 - 60, Panel.Bottom - 70), Muted);

        Rectangle footer = new(Panel.X + 18, Panel.Bottom - 40, Panel.Width - 36, 27);
        b.Draw(Game1.fadeToBlackRect, footer, WoodDark);
        Vector2 msg = Game1.smallFont.MeasureString(Message);
        b.DrawString(Game1.smallFont, Message, new Vector2(footer.X + 12, footer.Y + 5), msg.X < footer.Width - 24 ? Color.White : new Color(240, 220, 170));

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private Rectangle BackButton() => new(Panel.Right - 205, Panel.Y + 24, 125, 34);
    private Rectangle FilterButton(int index) => new(Panel.X + 30 + index * 125, Panel.Y + 84, 110, 32);
    private Rectangle RowRect(int row) => new(Panel.X + 24, Panel.Y + 128 + row * 76, Panel.Width - 48, 68);
    private Rectangle OneBatchButton(int row) => new(RowRect(row).Right - 164, RowRect(row).Y + 11, 72, 46);
    private Rectangle MaxBatchButton(int row) => new(RowRect(row).Right - 84, RowRect(row).Y + 11, 68, 46);
    private Rectangle PrevButton() => new(Panel.X + 30, Panel.Bottom - 75, 100, 34);
    private Rectangle NextButton() => new(Panel.Right - 130, Panel.Bottom - 75, 100, 34);

    private static void DrawButton(SpriteBatch b, Rectangle rect, string text, Color fill)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, fill);
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(rect.X + rect.Width / 2f - size.X / 2f, rect.Y + rect.Height / 2f - size.Y / 2f), Color.White);
    }
}
