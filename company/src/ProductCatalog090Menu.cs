using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>0.9.0 standalone product catalog matching the full visual rebuild.</summary>
internal sealed class ProductCatalog090Menu : Company084MenuBase
{
    private Texture2D? Atlas;
    private Texture2D? Skin;
    private int Page;
    private int Filter;
    private string SelectedKey;
    private string Message = "제품 카드를 선택하면 상세 생산 정보를 확인할 수 있습니다.";

    internal ProductCatalog090Menu(ModEntry mod, string selectedKey = "") : base(mod)
    {
        Mod.Production.EnsureState();
        SelectedKey = selectedKey;
        try { Atlas = Mod.Helper.ModContent.Load<Texture2D>("assets/production_visuals_087.png"); } catch { Atlas = null; }
        try { Skin = Mod.Helper.ModContent.Load<Texture2D>("assets/ui_skin_090.png"); } catch { Skin = null; }
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
        if (Close().Contains(x, y) || Back().Contains(x, y)) { Game1.playSound("bigDeSelect"); Game1.activeClickableMenu = new Production090Menu(Mod); return; }
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
            int max = Math.Min(10, Mod.Production.GetMaxBatches(selected));
            if (max <= 0) { Message = $"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다."; Game1.playSound("cancel"); return; }
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
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.72f);
        DrawFrame090(b); DrawHeader(b); DrawFilters(b); DrawCards(b); DrawDetail(b); DrawFooter(b); drawMouse(b);
    }

    private void DrawFrame090(SpriteBatch b)
    {
        Rectangle all = D(0, 0, 1400, 820);
        Fill(b, all, new Color(47, 27, 13));
        Tile(b, D(5, 5, 1390, 810), 0, Color.White);
        Fill(b, D(17, 89, 1366, 6), new Color(55, 31, 15));
        Fill(b, D(17, 740, 1366, 6), new Color(55, 31, 15));
    }

    private void DrawHeader(SpriteBatch b)
    {
        WoodButton(b, Back(), "← 생산 관리", false);
        Plaque(b, D(350, 12, 700, 68), "생산품 카탈로그", 1.08f);
        Leaf(b, D(319, 29, 27, 32)); Leaf(b, D(1055, 29, 27, 32));
        WoodButton(b, Close(), "×", false, new Color(188, 75, 44));
    }

    private void DrawFilters(SpriteBatch b)
    {
        string[] names = { "전체 제품", "중간재", "완제품" };
        for (int i = 0; i < 3; i++) WoodButton(b, FilterBtn(i), names[i], Filter == i, Filter == i ? Green : null);
        Text(b, Game1.smallFont, "제품을 선택하면 오른쪽에서 재료·생산 시간·예상 생산량·해금 조건을 확인할 수 있습니다.", D(510, 105, 810, 28), Muted, 0.72f);
    }

    private void DrawCards(SpriteBatch b)
    {
        SkinPanel(b, D(24, 145, 820, 585));
        Plaque(b, D(45, 143, 778, 42), "제품 목록", 0.76f);
        List<ProductionRecipeDefinition> rows = Rows(); int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            Rectangle r = RecipeCard(i); SkinPaper(b, r);
            int idx = start + i;
            if (idx >= rows.Count) { Text(b, Game1.smallFont, "빈 슬롯", r, Muted, 0.68f, true); continue; }
            ProductionRecipeDefinition recipe = rows[idx]; bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason); bool selected = string.Equals(recipe.Key, SelectedKey, StringComparison.OrdinalIgnoreCase);
            if (selected) { Fill(b, new Rectangle(r.X, r.Y, S(7), r.Height), Green); Fill(b, new Rectangle(r.X, r.Y, r.Width, S(5)), Gold); }
            Rectangle art = new(r.X + S(14), r.Y + S(17), S(92), S(92)); DrawProduct(b, recipe, art, unlocked ? 1f : 0.34f);
            Text(b, Game1.dialogueFont, recipe.DisplayName, new Rectangle(r.X + S(120), r.Y + S(14), r.Width - S(135), S(36)), unlocked ? Ink : Muted, 0.66f);
            string kind = string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품";
            Text(b, Game1.smallFont, $"{kind} · {LineName(recipe.LineType)} 라인", new Rectangle(r.X + S(121), r.Y + S(52), r.Width - S(136), S(24)), kind == "중간재" ? Blue : Green, 0.68f);
            string ingredient = Mod.Production.GetIngredientDisplayName(recipe); int have = Mod.Production.GetIngredientQuantity(recipe); int max = Mod.Production.GetMaxBatches(recipe);
            Text(b, Game1.smallFont, $"{ingredient} × {recipe.InputQuantity}  →  {recipe.OutputQuantity}{recipe.OutputUnit}", new Rectangle(r.X + S(121), r.Y + S(78), r.Width - S(136), S(23)), Muted, 0.64f);
            Text(b, Game1.smallFont, unlocked ? $"재고 {have:N0} · 최대 {max}배치" : $"🔒 {reason}", new Rectangle(r.X + S(121), r.Y + S(103), r.Width - S(136), S(23)), unlocked ? Ink : Red, 0.61f);
        }
    }

    private void DrawDetail(SpriteBatch b)
    {
        SkinPanel(b, D(857, 145, 519, 585));
        Plaque(b, D(880, 143, 473, 42), "선택 제품 상세", 0.76f);
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedKey) ?? Rows().FirstOrDefault();
        if (recipe is null) return; SelectedKey = recipe.Key;
        bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason);

        DrawProduct(b, recipe, D(1056, 201, 124, 124), unlocked ? 1f : 0.38f);
        Text(b, Game1.dialogueFont, recipe.DisplayName, D(902, 326, 430, 45), unlocked ? Ink : Muted, 0.86f, true);
        Text(b, Game1.smallFont, string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품", D(1015, 367, 205, 27), Green, 0.78f, true);

        string ing = Mod.Production.GetIngredientDisplayName(recipe); int have = Mod.Production.GetIngredientQuantity(recipe); ProductionForecast fc = Mod.Quality.GetForecast(recipe, 1);
        DetailRow(b, "필요 재료", $"{ing} × {recipe.InputQuantity}", 410);
        DetailRow(b, "현재 재고", have.ToString("N0"), 448);
        DetailRow(b, "생산 라인", $"{LineName(recipe.LineType)} 라인", 486);
        DetailRow(b, "예상 시간", ProductionCore.FormatDuration(recipe.DurationMinutes), 524);
        DetailRow(b, "예상 생산량", $"{fc.MinOutput} ~ {fc.MaxOutput}{recipe.OutputUnit}", 562);
        DetailRow(b, "예상 등급", fc.MostLikelyGrade, 600);
        Text(b, Game1.smallFont, unlocked ? $"해금: 회사 Lv.{recipe.RequiredCompanyLevel} · 브랜드 {recipe.RequiredBrandPoints}" : reason, D(904, 640, 426, 27), unlocked ? GreenDeep : Red, 0.68f, true);
        WoodButton(b, OneBatch(), "1배치 생산", unlocked, unlocked ? Green : new Color(133, 121, 97));
        WoodButton(b, MaxBatch(), "최대 생산", false, unlocked ? Blue : new Color(133, 121, 97));
    }

    private void DetailRow(SpriteBatch b, string label, string value, int y)
    {
        Text(b, Game1.smallFont, label, D(910, y, 120, 27), Ink, 0.73f); Dots(b, D(1033, y + 14, 145, 2)); Text(b, Game1.smallFont, value, D(1183, y, 155, 27), Ink, 0.71f);
    }

    private void DrawFooter(SpriteBatch b)
    {
        List<ProductionRecipeDefinition> rows = Rows(); int max = Math.Max(0, (rows.Count - 1) / 6);
        WoodButton(b, Prev(), "◀ 이전", false); WoodButton(b, Next(), "다음 ▶", false);
        Text(b, Game1.smallFont, $"{rows.Count}개 레시피 · {Page + 1}/{max + 1}", D(565, 753, 270, 27), Ink, 0.72f, true);
        Text(b, Game1.smallFont, Message, D(300, 790, 800, 20), Muted, 0.58f, true);
    }

    private void SkinPanel(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, WoodDeep); Tile(b, Inset(r, S(3)), 0, Color.White * 0.86f); Tile(b, Inset(r, S(8)), 1, Color.White);
    }

    private void SkinPaper(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, WoodDeep); Fill(b, Inset(r, S(3)), Gold * 0.88f); Tile(b, Inset(r, S(6)), 1, Color.White);
    }

    private void Tile(SpriteBatch b, Rectangle dest, int tile, Color tint)
    {
        if (Skin is null) { Fill(b, dest, tile == 0 ? Wood : tile == 2 ? GreenDeep : Cream); return; }
        Rectangle srcBase = new(tile * 64, 0, 64, 64);
        int step = Math.Max(1, S(64));
        for (int y = dest.Y; y < dest.Bottom; y += step)
        {
            for (int x = dest.X; x < dest.Right; x += step)
            {
                int w = Math.Min(step, dest.Right - x), h = Math.Min(step, dest.Bottom - y);
                int sw = Math.Min(64, Math.Max(1, (int)MathF.Round(w / Scale))), sh = Math.Min(64, Math.Max(1, (int)MathF.Round(h / Scale)));
                b.Draw(Skin, new Rectangle(x, y, w, h), new Rectangle(srcBase.X, srcBase.Y, sw, sh), tint);
            }
        }
    }

    private void DrawProduct(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle dest, float alpha)
    {
        if (Atlas is null) { Mod.Icons.DrawRecipeIcon(b, recipe, dest, alpha); return; }
        int idx = ProductSprite(recipe); Rectangle src = new((idx % 4) * 128, (idx / 4) * 128, 128, 128); b.Draw(Atlas, dest, src, Color.White * alpha);
    }

    private static int ProductSprite(ProductionRecipeDefinition recipe)
    {
        string n = recipe.DisplayName ?? "", k = recipe.Key ?? "";
        if (n.Contains("토마토주스") || k.Contains("TomatoJuice", StringComparison.OrdinalIgnoreCase)) return 9;
        if (n.Contains("수박주스") || k.Contains("WatermelonJuice", StringComparison.OrdinalIgnoreCase)) return 10;
        if (n.Contains("잼") || k.Contains("Jam", StringComparison.OrdinalIgnoreCase)) return 15;
        if (n.Contains("선물세트") || n.Contains("선물 세트")) return 11;
        if (n.Contains("펄프")) return 12;
        if (n.Contains("절임") || n.Contains("피클")) return 13;
        if (n.Contains("밀가루") || n.Contains("분말") || n.Contains("가루")) return 14;
        if (n.Contains("주스") || n.Contains("원액")) return 8;
        if (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase)) return n.Contains("세척") ? 3 : 5;
        return 8;
    }

    private Rectangle Back() => D(24, 22, 190, 48);
    private Rectangle Close() => D(1331, 20, 50, 50);
    private Rectangle FilterBtn(int i) => D(45 + i * 150, 101, 135, 34);
    private Rectangle RecipeCard(int i) { int col = i % 2, row = i / 2; return D(45 + col * 392, 198 + row * 168, 372, 150); }
    private Rectangle OneBatch() => D(902, 673, 196, 48);
    private Rectangle MaxBatch() => D(1112, 673, 210, 48);
    private Rectangle Prev() => D(42, 750, 120, 37);
    private Rectangle Next() => D(1238, 750, 120, 37);
}
