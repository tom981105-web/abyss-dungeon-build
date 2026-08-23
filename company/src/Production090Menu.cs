using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>
/// 0.9.0 standalone production-management screen.
/// This replaces the 0.8.x production menu + visual-overlay stack with one renderer.
/// </summary>
internal sealed class Production090Menu : Company084MenuBase
{
    private Texture2D? Atlas;
    private Texture2D? Skin;
    private string SelectedRecipeKey = "";
    private int PlanPage;
    private string Message = "생산 계획을 등록하면 빈 라인에 자동으로 배정됩니다.";

    internal Production090Menu(ModEntry mod) : base(mod)
    {
        Mod.Production.EnsureState();
        SelectedRecipeKey = Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, "TomatoJuice", StringComparison.OrdinalIgnoreCase))?.Key
            ?? Mod.Recipes.FirstOrDefault(p => !p.RequiresCropGenetics)?.Key
            ?? Mod.Recipes.FirstOrDefault()?.Key ?? "";
        try { Atlas = Mod.Helper.ModContent.Load<Texture2D>("assets/production_visuals_087.png"); } catch { Atlas = null; }
        try { Skin = Mod.Helper.ModContent.Load<Texture2D>("assets/ui_skin_090.png"); } catch { Skin = null; }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y)) { Game1.playSound("bigDeSelect"); exitThisMenu(); return; }
        if (Company().Contains(x, y)) { Game1.playSound("bigDeSelect"); Game1.activeClickableMenu = new CompanyMenu(Mod); return; }
        if (Catalog().Contains(x, y) || PlanAdd().Contains(x, y)) { Game1.playSound("bigSelect"); Game1.activeClickableMenu = new ProductCatalog090Menu(Mod, SelectedRecipeKey); return; }

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < Math.Min(3, lines.Count); i++)
        {
            if (!LineCard(i).Contains(x, y)) continue;
            ProductionJob? job = Mod.Production.GetLineJob(lines[i].Id);
            ProductionRecipeDefinition? recipe = job is null
                ? Mod.Recipes.FirstOrDefault(p => string.Equals(p.LineType, lines[i].LineType, StringComparison.OrdinalIgnoreCase))
                : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is not null) SelectedRecipeKey = recipe.Key;
            Game1.playSound("smallSelect");
            return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedRecipeKey);
        if (selected is not null && OneBatch().Contains(x, y))
        {
            bool ok = Mod.Production.TryStart(selected.Key, 1, out string message); Message = message; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }
        if (selected is not null && MaxBatch().Contains(x, y))
        {
            int max = Mod.Production.GetMaxBatches(selected);
            if (max <= 0) { Message = $"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다."; Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, Math.Min(10, max), out string message); Message = message; Game1.playSound(ok ? "Ship" : "cancel"); return;
        }

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int idx = start + row;
            if (idx >= plans.Count || !PlanRow(row).Contains(x, y)) continue;
            ProductionPlanEntry plan = plans[idx];
            SelectedRecipeKey = plan.RecipeKey;
            if (PlanUp(row).Contains(x, y)) { bool ok = Mod.Production.TryMovePlan(plan.Id, -1, out string m); Message = m; Game1.playSound(ok ? "shiny4" : "cancel"); }
            else if (PlanDown(row).Contains(x, y)) { bool ok = Mod.Production.TryMovePlan(plan.Id, 1, out string m); Message = m; Game1.playSound(ok ? "shiny4" : "cancel"); }
            else if (PlanRemove(row).Contains(x, y)) { bool ok = Mod.Production.TryRemovePlan(plan.Id, out string m); Message = m; Game1.playSound(ok ? "trashcan" : "cancel"); }
            else Game1.playSound("smallSelect");
            return;
        }
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int idx = start + row;
            if (idx >= plans.Count || !PlanRow(row).Contains(x, y)) continue;
            bool ok = Mod.Production.TryRemovePlan(plans[idx].Id, out string m); Message = m; Game1.playSound(ok ? "trashcan" : "cancel"); return;
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int max = Math.Max(0, (Mod.Production.GetPlans().Count - 1) / 5);
        if (direction < 0 && PlanPage < max) PlanPage++;
        else if (direction > 0 && PlanPage > 0) PlanPage--;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.72f);
        DrawFrame090(b);
        DrawHeader(b);
        DrawStats(b);
        DrawLines(b);
        DrawCurrent(b);
        DrawPlans(b);
        DrawBottom(b);
        if (!string.IsNullOrWhiteSpace(Message))
            Text(b, Game1.smallFont, Message, D(375, 803, 650, 14), new Color(88, 58, 31), 0.56f, true);
        drawMouse(b);
    }

    private void DrawFrame090(SpriteBatch b)
    {
        Rectangle all = D(0, 0, 1400, 820);
        Fill(b, all, new Color(47, 27, 13));
        Tile(b, D(5, 5, 1390, 810), 0, Color.White);
        Fill(b, D(17, 84, 1366, 6), new Color(55, 31, 15));
        Fill(b, D(17, 672, 1366, 6), new Color(55, 31, 15));
        for (int x = 18; x <= 1372; x += 338)
        {
            Fill(b, D(x, 8, 7, 7), Gold);
            Fill(b, D(x, 805, 7, 7), Gold);
        }
    }

    private void DrawHeader(SpriteBatch b)
    {
        WoodButton(b, Company(), CompanyName(), false);
        Plaque(b, D(355, 11, 690, 67), $"{CompanyName()} · 생산 관리 2.0", 1.06f);
        Leaf(b, D(323, 27, 27, 34)); Leaf(b, D(1050, 27, 27, 34));
        WoodButton(b, Close(), "×", false, new Color(188, 75, 44));
    }

    private void DrawStats(SpriteBatch b)
    {
        Stat(b, D(25, 97, 327, 72), 0, "회사 자금", $"{Mod.State.CompanyFunds:N0}G");
        Stat(b, D(364, 97, 327, 72), 1, "브랜드", Mod.Brand.GetTierName(Mod.State.BrandPoints));
        Stat(b, D(703, 97, 327, 72), 2, "활성 계약", $"{Mod.State.AcceptedContracts.Count}건");
        Stat(b, D(1042, 97, 333, 72), 3, "평판", Mod.State.Reputation.ToString("N0"));
    }

    private void Stat(SpriteBatch b, Rectangle r, int icon, string label, string value)
    {
        SkinPaper(b, r);
        Rectangle ir = new(r.X + S(18), r.Y + S(13), S(45), S(45));
        if (icon == 0) Coin(b, ir); else if (icon == 1) Shield(b, ir); else if (icon == 2) Scroll(b, ir); else Heart(b, ir);
        Text(b, Game1.smallFont, label, new Rectangle(r.X + S(78), r.Y + S(8), r.Width - S(94), S(25)), Ink, 0.96f);
        Text(b, Game1.dialogueFont, value, new Rectangle(r.X + S(78), r.Y + S(31), r.Width - S(94), S(32)), Ink, 0.72f);
    }

    private void DrawLines(SpriteBatch b)
    {
        SkinPanel(b, D(24, 184, 370, 480));
        Plaque(b, D(43, 182, 332, 42), "생산 라인", 0.78f);
        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < 3; i++)
        {
            Rectangle card = LineCard(i); SkinPaper(b, card);
            if (i >= lines.Count) { Text(b, Game1.smallFont, $"라인 {i + 1} · 잠김", new Rectangle(card.X + S(15), card.Y + S(8), card.Width - S(30), S(28)), Muted, 0.88f); continue; }
            ProductionLineState line = lines[i];
            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            ProductionRecipeDefinition? recipe = job is null ? null : Mod.Production.FindRecipe(job.RecipeKey);
            Text(b, Game1.smallFont, $"라인 {i + 1} · {LineName(line.LineType)}", new Rectangle(card.X + S(14), card.Y + S(6), S(210), S(26)), Ink, 0.94f);
            StatusPill(b, new Rectangle(card.Right - S(78), card.Y + S(7), S(64), S(27)), job is null ? "대기" : "가동 중", job is not null);
            DrawAtlas(b, MachineSprite(line.LineType), new Rectangle(card.X + S(10), card.Y + S(34), S(143), S(87)), job is null ? 0.82f : 1f);
            if (recipe is not null) DrawProduct(b, recipe, new Rectangle(card.X + S(160), card.Y + S(38), S(49), S(49)), 1f);
            Text(b, Game1.smallFont, recipe?.DisplayName ?? "대기 중", new Rectangle(card.X + S(216), card.Y + S(36), card.Width - S(230), S(28)), Ink, 0.90f);
            string stage = job is null ? "작업 없음" : Mod.Production.GetCurrentStageName(job);
            Text(b, Game1.smallFont, $"현재 단계  {stage}", new Rectangle(card.X + S(159), card.Y + S(66), card.Width - S(174), S(24)), job is null ? Muted : GreenDeep, 0.73f);
            float p = job is null ? 0f : (float)Mod.Production.GetJobProgress(job);
            Progress(b, new Rectangle(card.X + S(159), card.Y + S(93), S(142), S(13)), p);
            Text(b, Game1.smallFont, $"{Math.Clamp((int)(p * 100), 0, 100)}%", new Rectangle(card.X + S(306), card.Y + S(87), S(42), S(24)), Ink, 0.73f, true);
            int eff = job?.EfficiencyPercent ?? Mod.Production.GetLineEfficiency(line);
            string remain = job is null ? "-" : ProductionCore.FormatDuration(job.RemainingMinutes);
            Text(b, Game1.smallFont, $"◷ {remain}", new Rectangle(card.X + S(160), card.Y + S(111), S(100), S(22)), Ink, 0.64f);
            Text(b, Game1.smallFont, $"♣ 효율 {eff}%", new Rectangle(card.X + S(260), card.Y + S(109), S(88), S(23)), Green, 0.64f, true);
        }
        WoodButton(b, D(43, 627, 331, 29), "⚙ 작업 배정", false);
    }

    private void DrawCurrent(SpriteBatch b)
    {
        SkinPanel(b, D(405, 184, 582, 480));
        Plaque(b, D(428, 182, 536, 42), "현재 생산 상세", 0.78f);
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedRecipeKey) ?? Mod.Recipes.FirstOrDefault();
        if (recipe is null) return;
        ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, recipe.Key, StringComparison.OrdinalIgnoreCase));
        ProductionForecast forecast = active is null ? Mod.Quality.GetForecast(recipe, 1) : Mod.Quality.GetForecast(active);

        DrawProduct(b, recipe, D(585, 229, 67, 67), 1f);
        Text(b, Game1.dialogueFont, recipe.DisplayName, D(663, 232, 250, 38), Ink, 0.79f);
        Text(b, Game1.smallFont, string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품", D(665, 267, 120, 22), Green, 0.68f);
        DrawFlow(b, recipe, active);

        Rectangle metric = D(430, 463, 326, 121); SkinPaper(b, metric);
        Metric(b, "진행률", $"{Math.Clamp((int)((active is null ? 0f : (float)Mod.Production.GetJobProgress(active)) * 100), 0, 100)}%", 474);
        Metric(b, "예상 생산량", $"{forecast.MinOutput} ~ {forecast.MaxOutput}{recipe.OutputUnit}", 501);
        Metric(b, "예상 등급", forecast.MostLikelyGrade, 528);
        Metric(b, "예상 시간", ProductionCore.FormatDuration(active?.RemainingMinutes ?? recipe.DurationMinutes), 555);

        Rectangle q = D(767, 463, 190, 121); SkinPaper(b, q);
        Text(b, Game1.smallFont, "품질 요약", new Rectangle(q.X, q.Y + S(4), q.Width, S(24)), Ink, 0.84f, true);
        GradeChance(b, "S", forecast.SChance, q.X + S(12), q.Y + S(33), Gold);
        GradeChance(b, "A", forecast.AChance, q.X + S(101), q.Y + S(33), GreenBright);
        GradeChance(b, "B", forecast.BChance, q.X + S(12), q.Y + S(75), Blue);
        GradeChance(b, "C", forecast.CChance, q.X + S(101), q.Y + S(75), new Color(188, 116, 57));

        WoodButton(b, OneBatch(), "+ 1배치 추가", true);
        WoodButton(b, MaxBatch(), "⚙ 최대 생산", false, Blue);
        WoodButton(b, Catalog(), "▦ 제품 카탈로그", false);
    }

    private void DrawFlow(SpriteBatch b, ProductionRecipeDefinition recipe, ProductionJob? active)
    {
        List<(string name, int idx)> nodes = new() { ("원재료", -1) };
        foreach (var stage in recipe.Stages.Take(4).Select((s, i) => (s.DisplayName, i))) nodes.Add(stage);
        nodes.Add(("완제품", 99));
        int n = nodes.Count;
        float total = 520f, gap = 10f, nodeW = (total - gap * (n - 1)) / n;
        for (int i = 0; i < n; i++)
        {
            int dx = 433 + (int)MathF.Round(i * (nodeW + gap));
            bool current = active is not null && nodes[i].idx >= 0 && nodes[i].idx < 99 && active.CurrentStageIndex == nodes[i].idx;
            Rectangle card = D(dx, 304, (int)nodeW, 143);
            if (current) { Fill(b, D(dx - 4, 300, (int)nodeW + 8, 151), Orange); SkinPaper(b, card, new Color(255, 226, 177)); }
            else SkinPaper(b, card);
            Rectangle icon = D(dx + Math.Max(2, ((int)nodeW - 62) / 2), 319, 62, 68);
            if (nodes[i].idx == -1) DrawAtlas(b, 3, icon, 1f);
            else if (nodes[i].idx == 99) DrawProduct(b, recipe, icon, 1f);
            else DrawAtlas(b, ProcessSprite(nodes[i].name), icon, current ? 1f : 0.94f);
            Text(b, Game1.smallFont, nodes[i].name, D(dx + 2, 392, (int)nodeW - 4, 34), current ? GreenDeep : Ink, 0.65f, true);
            if (i < n - 1) Arrow(b, D(dx + (int)nodeW + 1, 353, (int)gap + 8, 20));
        }
    }

    private void DrawPlans(SpriteBatch b)
    {
        SkinPanel(b, D(999, 184, 377, 480));
        Plaque(b, D(1020, 182, 335, 42), "생산 계획", 0.78f);
        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            Rectangle r = PlanRow(row); SkinPaper(b, r);
            Rectangle number = new(r.X, r.Y, S(49), r.Height); Fill(b, number, GreenDeep);
            Text(b, Game1.dialogueFont, (start + row + 1).ToString(), number, Color.White, 0.72f, true);
            int idx = start + row;
            if (idx >= plans.Count) { Text(b, Game1.smallFont, "빈 계획", new Rectangle(r.X + S(70), r.Y, S(190), r.Height), Muted, 0.80f); continue; }
            ProductionPlanEntry plan = plans[idx]; ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(plan.RecipeKey);
            if (recipe is not null) DrawProduct(b, recipe, new Rectangle(r.X + S(60), r.Y + S(8), S(48), S(48)), 1f);
            Text(b, Game1.smallFont, $"{recipe?.DisplayName ?? plan.RecipeKey} × {plan.BatchCount}", new Rectangle(r.X + S(116), r.Y + S(6), S(168), S(48)), Ink, 0.75f);
            DrawArrowButton(b, PlanUp(row), true); DrawArrowButton(b, PlanDown(row), false);
            bool running = Mod.State.ProductionQueue.Any(p => string.Equals(p.RecipeKey, plan.RecipeKey, StringComparison.OrdinalIgnoreCase));
            DrawStatusDot(b, new Rectangle(r.Right - S(27), r.Y + S(23), S(14), S(14)), running ? GreenBright : Blue);
            Fill(b, PlanRemove(row), Red); Text(b, Game1.smallFont, "×", PlanRemove(row), Color.White, 0.55f, true);
        }
        WoodButton(b, PlanAdd(), "+ 계획 추가", false);
        Text(b, Game1.smallFont, $"빈 라인 자동 배정  ✓     {PlanPage + 1}/{Math.Max(1, (plans.Count + 4) / 5)}", D(1023, 642, 330, 18), Ink, 0.62f, true);
    }

    private void DrawBottom(SpriteBatch b)
    {
        DrawStockPanel(b, D(24, 682, 665, 120), "중간재", false);
        DrawStockPanel(b, D(701, 682, 675, 120), "완제품", true);
    }

    private void DrawStockPanel(SpriteBatch b, Rectangle r, string title, bool finished)
    {
        SkinPanel(b, r);
        Fill(b, new Rectangle(r.X, r.Y, r.Width, S(31)), WoodDeep);
        Tile(b, new Rectangle(r.X + S(4), r.Y + S(4), r.Width - S(8), S(23)), 0, Color.White * 0.75f);
        Text(b, Game1.dialogueFont, title, new Rectangle(r.X, r.Y, r.Width, S(31)), new Color(248, 220, 147), 0.62f, true);

        if (!finished)
        {
            List<IntermediateStockEntry> rows = Mod.Production.GetIntermediateStock().Where(p => p.Quantity > 0).Take(5).ToList();
            for (int i = 0; i < 5; i++)
            {
                Rectangle slot = new(r.X + S(15 + i * 126), r.Y + S(43), S(112), S(61)); SkinPaper(b, slot);
                if (i >= rows.Count) { Text(b, Game1.smallFont, "빈 슬롯", slot, Muted, 0.58f, true); continue; }
                IntermediateStockEntry row = rows[i];
                Mod.Icons.DrawProductIcon(b, row.Key, new Rectangle(slot.X + S(8), slot.Y + S(9), S(40), S(40)));
                Text(b, Game1.smallFont, row.DisplayName, new Rectangle(slot.X + S(52), slot.Y + S(6), slot.Width - S(57), S(25)), Ink, 0.56f);
                Text(b, Game1.smallFont, row.Quantity.ToString("N0"), new Rectangle(slot.X + S(52), slot.Y + S(31), slot.Width - S(57), S(22)), Ink, 0.66f);
            }
        }
        else
        {
            List<ProductStockEntry> rows = Mod.State.FinishedGoods.Values.Where(p => p is not null && p.Quantity > 0).OrderByDescending(p => p.Quality).ThenByDescending(p => p.Quantity).Take(5).ToList();
            for (int i = 0; i < 5; i++)
            {
                Rectangle slot = new(r.X + S(15 + i * 128), r.Y + S(43), S(114), S(61)); SkinPaper(b, slot);
                if (i >= rows.Count) { Text(b, Game1.smallFont, "빈 슬롯", slot, Muted, 0.58f, true); continue; }
                ProductStockEntry row = rows[i]; ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(row.ProductKey);
                if (recipe is not null) DrawProduct(b, recipe, new Rectangle(slot.X + S(7), slot.Y + S(7), S(44), S(44)), 1f);
                else Mod.Icons.DrawProductIcon(b, row.ProductKey, new Rectangle(slot.X + S(7), slot.Y + S(7), S(44), S(44)));
                Text(b, Game1.smallFont, recipe?.DisplayName ?? row.ProductKey, new Rectangle(slot.X + S(54), slot.Y + S(4), slot.Width - S(58), S(22)), Ink, 0.54f);
                Grade(b, new Rectangle(slot.X + S(54), slot.Y + S(28), S(47), S(23)), row.Grade);
                Text(b, Game1.smallFont, row.Quantity.ToString("N0"), new Rectangle(slot.Right - S(38), slot.Y + S(30), S(31), S(21)), Ink, 0.60f, true);
            }
        }
    }

    private void Metric(SpriteBatch b, string label, string value, int y)
    {
        Text(b, Game1.smallFont, label, D(446, y, 120, 24), Ink, 0.70f); Dots(b, D(566, y + 12, 95, 2)); Text(b, Game1.smallFont, value, D(665, y, 80, 24), Ink, 0.72f, true);
    }

    private void GradeChance(SpriteBatch b, string grade, int chance, int x, int y, Color color)
    {
        Star(b, new Rectangle(x, y, S(21), S(21)), color);
        Text(b, Game1.smallFont, $"{grade}급 {chance}%", new Rectangle(x + S(28), y - S(1), S(62), S(23)), Ink, 0.60f);
    }

    private void DrawArrowButton(SpriteBatch b, Rectangle r, bool up)
    {
        SkinPaper(b, r, new Color(238, 211, 163)); int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2; Color c = new(107, 75, 40);
        Fill(b, new Rectangle(cx - S(2), up ? cy - S(2) : cy - S(7), S(4), S(9)), c);
        if (up) TriangleUp090(b, new Rectangle(cx - S(7), cy - S(9), S(14), S(8)), c); else TriangleDown(b, new Rectangle(cx - S(7), cy + S(1), S(14), S(8)), c);
    }

    private void TriangleUp090(SpriteBatch b, Rectangle r, Color c)
    {
        for (int i = 0; i < Math.Max(1, r.Height); i++) { int w = Math.Max(1, (int)(r.Width * ((i + 1f) / r.Height))); Fill(b, new Rectangle(r.X + (r.Width - w) / 2, r.Bottom - 1 - i, w, 1), c); }
    }

    private void DrawStatusDot(SpriteBatch b, Rectangle r, Color c)
    {
        Fill(b, new Rectangle(r.X + r.Width / 3, r.Y, r.Width / 3, r.Height), c); Fill(b, new Rectangle(r.X, r.Y + r.Height / 3, r.Width, r.Height / 3), c); Fill(b, Inset(r, S(2)), c);
    }

    private void SkinPanel(SpriteBatch b, Rectangle r, Color? fill = null)
    {
        Fill(b, r, WoodDeep); Tile(b, Inset(r, S(3)), 0, Color.White * 0.85f); Tile(b, Inset(r, S(8)), 1, Color.White); if (fill.HasValue) Fill(b, Inset(r, S(9)), fill.Value * 0.32f);
    }

    private void SkinPaper(SpriteBatch b, Rectangle r, Color? fill = null)
    {
        Fill(b, r, WoodDeep); Fill(b, Inset(r, S(3)), Gold * 0.85f); Tile(b, Inset(r, S(6)), 1, Color.White); if (fill.HasValue) Fill(b, Inset(r, S(7)), fill.Value * 0.38f);
    }

    private void Tile(SpriteBatch b, Rectangle dest, int tile, Color tint)
    {
        if (Skin is null) { Fill(b, dest, tile == 0 ? Wood : tile == 2 ? GreenDeep : Cream); return; }
        Rectangle srcBase = new(tile * 64, 0, 64, 64);
        for (int y = dest.Y; y < dest.Bottom; y += Math.Max(1, S(64)))
        {
            for (int x = dest.X; x < dest.Right; x += Math.Max(1, S(64)))
            {
                int w = Math.Min(Math.Max(1, S(64)), dest.Right - x), h = Math.Min(Math.Max(1, S(64)), dest.Bottom - y);
                Rectangle src = new(srcBase.X, srcBase.Y, Math.Max(1, (int)MathF.Round(w / Scale)), Math.Max(1, (int)MathF.Round(h / Scale)));
                src.Width = Math.Min(64, src.Width); src.Height = Math.Min(64, src.Height);
                b.Draw(Skin, new Rectangle(x, y, w, h), src, tint);
            }
        }
    }

    private void DrawAtlas(SpriteBatch b, int index, Rectangle dest, float alpha)
    {
        if (Atlas is null || index < 0 || index >= 16) return;
        Rectangle src = new((index % 4) * 128, (index / 4) * 128, 128, 128);
        b.Draw(Atlas, dest, src, Color.White * alpha);
    }

    private void DrawProduct(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle dest, float alpha)
    {
        int idx = ProductSprite(recipe);
        if (Atlas is not null) DrawAtlas(b, idx, dest, alpha); else Mod.Icons.DrawRecipeIcon(b, recipe, dest, alpha);
    }

    private static int MachineSprite(string type) => type switch { "Fermentation" => 1, "Packaging" => 2, _ => 0 };
    private static int ProcessSprite(string? stage)
    {
        string s = stage ?? "";
        if (s.Contains("세척")) return 4;
        if (s.Contains("착즙") || s.Contains("압착") || s.Contains("파쇄") || s.Contains("분쇄") || s.Contains("절단")) return 5;
        if (s.Contains("살균") || s.Contains("가열") || s.Contains("숙성") || s.Contains("발효") || s.Contains("염장")) return 6;
        if (s.Contains("병입")) return 7;
        if (s.Contains("포장") || s.Contains("세트")) return 11;
        return 5;
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

    private Rectangle Company() => D(18, 16, 245, 62);
    private Rectangle Close() => D(1331, 17, 50, 50);
    private Rectangle LineCard(int i) => D(39, 228 + i * 139, 340, 132);
    private Rectangle OneBatch() => D(438, 602, 170, 49);
    private Rectangle MaxBatch() => D(620, 602, 170, 49);
    private Rectangle Catalog() => D(802, 602, 154, 49);
    private Rectangle PlanRow(int row) => D(1015, 230 + row * 73, 342, 64);
    private Rectangle PlanUp(int row) => D(1280, 235 + row * 73, 31, 24);
    private Rectangle PlanDown(int row) => D(1280, 261 + row * 73, 31, 24);
    private Rectangle PlanRemove(int row) => D(1320, 268 + row * 73, 21, 21);
    private Rectangle PlanAdd() => D(1016, 599, 338, 43);
}
