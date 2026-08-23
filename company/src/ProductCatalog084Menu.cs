using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed class ProductCatalog084Menu : Company084MenuBase
{
    private int Page;
    private int Filter;
    private string SelectedKey;
    private string Message = "제품 카드를 선택하면 상세 생산 정보를 확인할 수 있습니다.";

    internal ProductCatalog084Menu(ModEntry mod, string selectedKey = "") : base(mod)
    {
        Mod.Production.EnsureState();
        SelectedKey = selectedKey;
    }

    private List<ProductionRecipeDefinition> Rows()
    {
        IEnumerable<ProductionRecipeDefinition> q = Mod.Production.GetCatalogRecipes(true);
        if (Filter == 1) q = q.Where(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        else if (Filter == 2) q = q.Where(p => !string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y) || Back().Contains(x, y)) { Game1.playSound("bigDeSelect"); Game1.activeClickableMenu = new Production084Menu(Mod); return; }
        for (int i = 0; i < 3; i++) if (FilterBtn(i).Contains(x, y)) { Filter = i; Page = 0; Game1.playSound("smallSelect"); return; }
        List<ProductionRecipeDefinition> rows = Rows(); int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            int idx = start + i; if (idx >= rows.Count) break;
            if (RecipeCard(i).Contains(x, y)) { SelectedKey = rows[idx].Key; Game1.playSound("smallSelect"); return; }
        }
        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedKey);
        if (selected is not null && OneBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out string reason)) { Message = reason; Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, 1, out string m); Message = m; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }
        if (selected is not null && MaxBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out string reason)) { Message = reason; Game1.playSound("cancel"); return; }
            int max = Math.Min(10, Mod.Production.GetMaxBatches(selected)); if (max <= 0) { Message = $"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다."; Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, max, out string m); Message = m; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }
        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (Prev().Contains(x, y) && Page > 0) { Page--; Game1.playSound("shwip"); }
        else if (Next().Contains(x, y) && Page < maxPage) { Page++; Game1.playSound("shwip"); }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<ProductionRecipeDefinition> rows = Rows(); int max = Math.Max(0, (rows.Count - 1) / 6);
        if (direction < 0 && Page < max) Page++; else if (direction > 0 && Page > 0) Page--;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.69f);
        Frame(b); Header(b); Filters(b); Cards(b); Detail(b); Footer(b); drawMouse(b);
    }

    private void Header(SpriteBatch b)
    {
        Plaque(b, D(250, 15, 900, 64), "생산품 카탈로그 · Production 2.4", 0.96f);
        WoodButton(b, Back(), "← 생산 관리", false); WoodButton(b, Close(), "×", false, new Color(196, 106, 55));
    }

    private void Filters(SpriteBatch b)
    {
        string[] names = { "전체 제품", "중간재", "완제품" };
        for (int i = 0; i < 3; i++) WoodButton(b, FilterBtn(i), names[i], Filter == i, Filter == i ? Green : null);
        Text(b, Game1.smallFont, "제품을 선택하면 오른쪽에서 재료·시간·수율·해금 조건을 확인할 수 있습니다.", D(520, 102, 820, 30), Muted, 0.75f);
    }

    private void Cards(SpriteBatch b)
    {
        Paper(b, D(24, 145, 855, 590), Cream2); Plaque(b, D(45, 143, 813, 40), "제품 목록", 0.72f);
        List<ProductionRecipeDefinition> rows = Rows(); int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            Rectangle r = RecipeCard(i); Paper(b, r, Cream);
            int idx = start + i; if (idx >= rows.Count) { Text(b, Game1.smallFont, "빈 슬롯", r, Muted, 0.72f, true); continue; }
            ProductionRecipeDefinition recipe = rows[idx]; bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason); bool selected = string.Equals(recipe.Key, SelectedKey, StringComparison.OrdinalIgnoreCase);
            if (selected) { Fill(b, new Rectangle(r.X, r.Y, S(6), r.Height), Green); Fill(b, new Rectangle(r.X, r.Y, r.Width, S(5)), Gold); }
            Mod.Icons.DrawRecipeIcon(b, recipe, new Rectangle(r.X + S(14), r.Y + S(18), S(67), S(67)), unlocked ? 1f : 0.45f);
            Text(b, Game1.dialogueFont, recipe.DisplayName, new Rectangle(r.X + S(94), r.Y + S(12), r.Width - S(110), S(34)), unlocked ? Ink : Muted, 0.67f);
            string kind = string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품";
            Text(b, Game1.smallFont, $"{kind} · {LineName(recipe.LineType)} 라인", new Rectangle(r.X + S(95), r.Y + S(49), r.Width - S(110), S(24)), kind == "중간재" ? Blue : Green, 0.70f);
            string ingredient = Mod.Production.GetIngredientDisplayName(recipe); int have = Mod.Production.GetIngredientQuantity(recipe); int max = Mod.Production.GetMaxBatches(recipe);
            Text(b, Game1.smallFont, $"{ingredient} {recipe.InputQuantity} → {recipe.OutputQuantity}{recipe.OutputUnit}", new Rectangle(r.X + S(95), r.Y + S(74), r.Width - S(110), S(23)), Muted, 0.66f);
            Text(b, Game1.smallFont, unlocked ? $"재고 {have} · 최대 {max}배치" : reason, new Rectangle(r.X + S(95), r.Y + S(98), r.Width - S(110), S(23)), unlocked ? Ink : Red, 0.64f);
        }
    }

    private void Detail(SpriteBatch b)
    {
        Paper(b, D(894, 145, 482, 590), Cream2); Plaque(b, D(916, 143, 438, 40), "선택 제품 상세", 0.72f);
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedKey) ?? Rows().FirstOrDefault();
        if (recipe is null) return; SelectedKey = recipe.Key;
        bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason);
        Mod.Icons.DrawRecipeIcon(b, recipe, D(1082, 202, 104, 104), unlocked ? 1f : 0.48f);
        Text(b, Game1.dialogueFont, recipe.DisplayName, D(930, 316, 410, 45), unlocked ? Ink : Muted, 0.82f, true);
        Text(b, Game1.smallFont, string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품", D(1030, 360, 210, 27), Green, 0.78f, true);

        string ing = Mod.Production.GetIngredientDisplayName(recipe); int have = Mod.Production.GetIngredientQuantity(recipe); ProductionForecast fc = Mod.Quality.GetForecast(recipe, 1);
        DetailRow(b, "필요 재료", $"{ing} × {recipe.InputQuantity}", 405);
        DetailRow(b, "현재 재고", have.ToString("N0"), 443);
        DetailRow(b, "생산 라인", $"{LineName(recipe.LineType)} 라인", 481);
        DetailRow(b, "예상 시간", ProductionCore.FormatDuration(recipe.DurationMinutes), 519);
        DetailRow(b, "예상 생산량", $"{fc.MinOutput} ~ {fc.MaxOutput}{recipe.OutputUnit}", 557);
        DetailRow(b, "예상 등급", fc.MostLikelyGrade, 595);
        Text(b, Game1.smallFont, unlocked ? $"해금: 회사 Lv.{recipe.RequiredCompanyLevel} · 브랜드 {recipe.RequiredBrandPoints}" : reason, D(927, 631, 414, 28), unlocked ? GreenDeep : Red, 0.68f, true);
        WoodButton(b, OneBatch(), "1배치 생산", unlocked, unlocked ? Green : new Color(133, 121, 97));
        WoodButton(b, MaxBatch(), "최대 생산", false, unlocked ? Blue : new Color(133, 121, 97));
    }

    private void DetailRow(SpriteBatch b, string label, string value, int y)
    {
        Text(b, Game1.smallFont, label, D(936, y, 118, 27), Ink, 0.74f); Dots(b, D(1055, y + 14, 115, 2)); Text(b, Game1.smallFont, value, D(1175, y, 165, 27), Ink, 0.72f);
    }

    private void Footer(SpriteBatch b)
    {
        List<ProductionRecipeDefinition> rows = Rows(); int max = Math.Max(0, (rows.Count - 1) / 6);
        WoodButton(b, Prev(), "◀ 이전", false); WoodButton(b, Next(), "다음 ▶", false);
        Text(b, Game1.smallFont, $"{rows.Count}개 레시피 · {Page + 1}/{max + 1}", D(570, 746, 260, 28), Ink, 0.73f, true);
        Text(b, Game1.smallFont, Message, D(300, 785, 800, 22), Muted, 0.60f, true);
    }

    private Rectangle Back() => D(24, 22, 190, 48);
    private Rectangle Close() => D(1331, 20, 50, 50);
    private Rectangle FilterBtn(int i) => D(45 + i * 150, 99, 135, 34);
    private Rectangle RecipeCard(int i) { int col = i % 2, row = i / 2; return D(45 + col * 405, 198 + row * 168, 385, 150); }
    private Rectangle OneBatch() => D(933, 671, 190, 48);
    private Rectangle MaxBatch() => D(1140, 671, 190, 48);
    private Rectangle Prev() => D(42, 744, 120, 37);
    private Rectangle Next() => D(1238, 744, 120, 37);
}
