using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductCatalog100Menu : LiveProductionUi100Base
{
    private int Page;
    private int Filter;
    private string SelectedKey;
    private string Message = "제품을 선택하면 실제 생산 정보가 즉시 갱신됩니다.";
    private double MessageUntil;

    internal ProductCatalog100Menu(ModEntry mod, string selectedKey = "")
        : base(mod, "assets/ui_100_catalog_base.png")
    {
        Mod.Production.EnsureState();
        SelectedKey = selectedKey;
        if (string.IsNullOrWhiteSpace(SelectedKey))
            SelectedKey = Rows().FirstOrDefault()?.Key ?? "";
        Show("Live Product Catalog 0.10.0");
    }

    private void Show(string message, double seconds = 3.0)
    {
        Message = message;
        MessageUntil = Game1.currentGameTime.TotalGameTime.TotalSeconds + seconds;
    }

    private List<ProductionRecipeDefinition> Rows()
    {
        IEnumerable<ProductionRecipeDefinition> q = Mod.Production.GetCatalogRecipes(true);
        if (Filter == 1) q = q.Where(p => p.OutputKind.Equals("Intermediate", StringComparison.OrdinalIgnoreCase));
        else if (Filter == 2) q = q.Where(p => !p.OutputKind.Equals("Intermediate", StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y) || Back().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.activeClickableMenu = new Production100Menu(Mod);
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            if (!FilterButton(i).Contains(x, y)) continue;
            Filter = i;
            Page = 0;
            SelectedKey = Rows().FirstOrDefault()?.Key ?? "";
            Show(i == 0 ? "전체 제품" : i == 1 ? "중간재만 표시" : "완제품만 표시");
            Game1.playSound("smallSelect");
            return;
        }

        List<ProductionRecipeDefinition> rows = Rows();
        int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            int idx = start + i;
            if (idx >= rows.Count || !RecipeCard(i).Contains(x, y)) continue;
            SelectedKey = rows[idx].Key;
            Show($"{rows[idx].DisplayName} 선택");
            Game1.playSound("smallSelect");
            return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedKey) ?? rows.Skip(start).FirstOrDefault();
        if (selected is not null && OneBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out string lockReason)) { Show(lockReason); Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, 1, out string msg);
            Show(msg); Game1.playSound(ok ? "Ship" : "cancel"); return;
        }
        if (selected is not null && MaxBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out string lockReason)) { Show(lockReason); Game1.playSound("cancel"); return; }
            int max = Math.Min(10, Mod.Production.GetMaxBatches(selected));
            if (max <= 0) { Show($"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다."); Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, max, out string msg);
            Show(msg); Game1.playSound(ok ? "Ship" : "cancel"); return;
        }

        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (Prev().Contains(x, y))
        {
            if (Page <= 0) { Show("첫 페이지입니다."); Game1.playSound("cancel"); }
            else { Page--; SelectFirstVisible(); Show($"{Page + 1}/{maxPage + 1} 페이지"); Game1.playSound("shwip"); }
            return;
        }
        if (Next().Contains(x, y))
        {
            if (Page >= maxPage) { Show("마지막 페이지입니다."); Game1.playSound("cancel"); }
            else { Page++; SelectFirstVisible(); Show($"{Page + 1}/{maxPage + 1} 페이지"); Game1.playSound("shwip"); }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int max = Math.Max(0, (rows.Count - 1) / 6);
        if (direction < 0 && Page < max) { Page++; SelectFirstVisible(); Show($"{Page + 1}/{max + 1} 페이지"); }
        else if (direction > 0 && Page > 0) { Page--; SelectFirstVisible(); Show($"{Page + 1}/{max + 1} 페이지"); }
    }

    private void SelectFirstVisible()
    {
        SelectedKey = Rows().Skip(Page * 6).FirstOrDefault()?.Key ?? "";
    }

    public override void draw(SpriteBatch b)
    {
        DrawBackground(b);
        DrawFilters(b);
        DrawCards(b);
        DrawDetails(b);
        DrawFooter(b);
        DrawMessage(b);
        drawMouse(b);
    }

    private void DrawFilters(SpriteBatch b)
    {
        string[] labels = { "전체 제품", "중간재", "완제품" };
        for (int i = 0; i < 3; i++)
        {
            Rectangle r = FilterImage(i);
            if (Filter == i) Fill(b, H(r.X, r.Y, r.Width, r.Height), new Color(46, 129, 52) * 0.30f);
            if (Filter == i) Outline(b, H(r.X, r.Y, r.Width, r.Height), Gold, 4);
            TextCentered(b, Game1.dialogueFont, labels[i], r, 0.62f, i == Filter ? DeepGreen : Ink);
        }
    }

    private void DrawCards(SpriteBatch b)
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            Rectangle card = RecipeCardImage(i);
            int idx = start + i;
            if (idx >= rows.Count)
            {
                TextCentered(b, Game1.smallFont, "빈 슬롯", card, 0.62f, Muted);
                continue;
            }

            ProductionRecipeDefinition recipe = rows[idx];
            bool selected = string.Equals(recipe.Key, SelectedKey, StringComparison.OrdinalIgnoreCase);
            if (selected) Outline(b, H(card.X - 2, card.Y - 2, card.Width + 4, card.Height + 4), Orange, 5);
            bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string lockReason);
            DrawProduct(b, recipe, new Rectangle(card.X + 12, card.Y + 22, 142, 142), unlocked ? 1f : 0.42f);
            Text(b, Game1.dialogueFont, recipe.DisplayName, card.X + 170, card.Y + 20, 0.60f, unlocked ? Ink : Muted);
            Text(b, Game1.smallFont, $"{KindName(recipe)} · {LineName(recipe.LineType)} 라인", card.X + 170, card.Y + 60, 0.55f, Blue);
            Text(b, Game1.smallFont, $"{Mod.Production.GetIngredientDisplayName(recipe)} × {recipe.InputQuantity}", card.X + 170, card.Y + 102, 0.55f);
            int stock = ProductQuantity(recipe);
            int max = Mod.Production.GetMaxBatches(recipe);
            Text(b, Game1.smallFont, $"재고 {stock} · 최대 {max}배치", card.X + 170, card.Y + 134, 0.52f, DeepGreen);
            if (!unlocked) Text(b, Game1.smallFont, $"잠금: {lockReason}", card.X + 170, card.Y + 160, 0.50f, Red);
        }
    }

    private void DrawDetails(SpriteBatch b)
    {
        List<ProductionRecipeDefinition> rows = Rows();
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedKey) ?? rows.Skip(Page * 6).FirstOrDefault();
        if (recipe is null)
        {
            TextCentered(b, Game1.dialogueFont, "표시할 제품이 없습니다.", new Rectangle(1050, 300, 470, 200), 0.66f, Muted);
            return;
        }
        SelectedKey = recipe.Key;
        bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string lockReason);
        DrawProduct(b, recipe, new Rectangle(1190, 215, 225, 225), unlocked ? 1f : 0.42f);
        TextCentered(b, Game1.dialogueFont, recipe.DisplayName, new Rectangle(1080, 438, 450, 48), 0.74f);
        TextCentered(b, Game1.smallFont, KindName(recipe), new Rectangle(1080, 485, 450, 28), 0.62f, Green);

        int y = 535;
        DetailLine(b, "필요 재료", $"{Mod.Production.GetIngredientDisplayName(recipe)} × {recipe.InputQuantity}", y); y += 40;
        DetailLine(b, "현재 재고", ProductQuantity(recipe).ToString("N0"), y); y += 40;
        DetailLine(b, "생산 라인", $"{LineName(recipe.LineType)} 라인", y); y += 40;
        DetailLine(b, "예상 시간", ProductionCore.FormatDuration(recipe.DurationMinutes), y); y += 40;
        ProductionForecast fc = Mod.Quality.GetForecast(recipe, 1);
        DetailLine(b, "예상 생산량", $"{fc.MinOutput} ~ {fc.MaxOutput}{recipe.OutputUnit}", y); y += 40;
        DetailLine(b, "예상 등급", fc.MostLikelyGrade, y);
        TextCentered(b, Game1.smallFont, unlocked ? $"해금: 회사 Lv.{Math.Max(1, recipe.RequiredCompanyLevel)} · 브랜드 {Math.Max(0, recipe.RequiredBrandPoints)}" : $"잠금: {lockReason}", new Rectangle(1060, 735, 490, 28), 0.53f, unlocked ? DeepGreen : Red);
    }

    private void DetailLine(SpriteBatch b, string label, string value, int y)
    {
        Text(b, Game1.smallFont, label, 1068, y, 0.55f);
        Fill(b, H(1195, y + 17, 240, 2), new Color(164, 121, 70) * 0.55f);
        Text(b, Game1.smallFont, value, 1450, y, 0.55f);
    }

    private void DrawFooter(SpriteBatch b)
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        TextCentered(b, Game1.dialogueFont, $"{rows.Count}개 레시피 · {Page + 1}/{maxPage + 1}", new Rectangle(620, 844, 430, 32), 0.57f, new Color(244, 202, 95));
        TextCentered(b, Game1.smallFont, "카드/필터/페이지가 실제 데이터에 따라 즉시 바뀝니다.", new Rectangle(515, 882, 650, 28), 0.50f, Color.White);
    }

    private void DrawMessage(SpriteBatch b)
    {
        if (string.IsNullOrWhiteSpace(Message) || Game1.currentGameTime.TotalGameTime.TotalSeconds > MessageUntil) return;
        Rectangle box = H(530, 908, 612, 27);
        Fill(b, box, new Color(42, 73, 37) * 0.94f);
        Outline(b, box, Gold, 2);
        TextCentered(b, Game1.smallFont, Message, new Rectangle(530, 908, 612, 27), 0.50f, Color.White);
    }

    private Rectangle Close() => H(1562, 14, 70, 70);
    private Rectangle Back() => H(36, 844, 258, 72);
    private Rectangle Prev() => Back();
    private Rectangle Next() => H(1378, 844, 252, 72);
    private Rectangle OneBatch() => H(1040, 755, 267, 82);
    private Rectangle MaxBatch() => H(1310, 755, 267, 82);
    private Rectangle FilterButton(int i) { Rectangle r = FilterImage(i); return H(r.X, r.Y, r.Width, r.Height); }
    private Rectangle FilterImage(int i) => i switch
    {
        0 => new Rectangle(52, 91, 276, 66),
        1 => new Rectangle(338, 91, 260, 66),
        _ => new Rectangle(610, 91, 260, 66)
    };
    private Rectangle RecipeCard(int i) { Rectangle r = RecipeCardImage(i); return H(r.X, r.Y, r.Width, r.Height); }
    private Rectangle RecipeCardImage(int i)
    {
        int col = i % 2;
        int row = i / 2;
        return new Rectangle(88 + col * 449, 220 + row * 207, 419, 179);
    }
}
