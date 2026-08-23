using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

/// <summary>
/// 0.8.0 production UI rebuild.
/// Uses a 1400x960 virtual canvas based on the approved visual reference, then scales the
/// entire composition into the current Stardew UI viewport so its proportions stay intact.
/// </summary>
internal sealed class Production080Menu : IClickableMenu
{
    private const int DesignW = 1400;
    private const int DesignH = 960;

    private readonly ModEntry Mod;
    private float UiScale = 1f;
    private int OriginX;
    private int OriginY;
    private string SelectedRecipeKey = "";
    private int PlanPage;
    private string Message = "생산 계획을 등록하면 호환되는 빈 라인에 자동으로 배정됩니다.";

    private static readonly Color WoodDark = new(62, 38, 20);
    private static readonly Color Wood = new(113, 70, 32);
    private static readonly Color WoodLight = new(157, 104, 49);
    private static readonly Color Paper = new(250, 235, 194);
    private static readonly Color Paper2 = new(244, 225, 180);
    private static readonly Color Paper3 = new(235, 210, 159);
    private static readonly Color GreenDark = new(31, 73, 29);
    private static readonly Color Green = new(48, 105, 44);
    private static readonly Color GreenLight = new(91, 143, 64);
    private static readonly Color Gold = new(222, 166, 54);
    private static readonly Color Orange = new(223, 126, 48);
    private static readonly Color Ink = new(77, 53, 31);
    private static readonly Color Muted = new(122, 92, 57);
    private static readonly Color Blue = new(46, 96, 143);
    private static readonly Color Red = new(166, 62, 45);
    private static readonly Color Purple = new(128, 75, 146);

    internal Production080Menu(ModEntry mod)
        : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height, false)
    {
        Mod = mod;
        Mod.Production.EnsureState();
        SelectedRecipeKey = Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, "TomatoJuice", StringComparison.OrdinalIgnoreCase))?.Key
            ?? Mod.Recipes.FirstOrDefault(p => !p.RequiresCropGenetics)?.Key
            ?? Mod.Recipes.FirstOrDefault()?.Key
            ?? "";
        Recalculate();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = Game1.uiViewport.Width;
        height = Game1.uiViewport.Height;
        Recalculate();
    }

    private void Recalculate()
    {
        int uiW = Math.Max(640, Game1.uiViewport.Width);
        int uiH = Math.Max(500, Game1.uiViewport.Height);
        UiScale = Math.Min((uiW - 16f) / DesignW, (uiH - 16f) / DesignH);
        UiScale = Math.Clamp(UiScale, 0.52f, 1.10f);
        int actualW = (int)MathF.Round(DesignW * UiScale);
        int actualH = (int)MathF.Round(DesignH * UiScale);
        OriginX = (uiW - actualW) / 2;
        OriginY = (uiH - actualH) / 2;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (CloseButton().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            exitThisMenu();
            return;
        }
        if (CompanyButton().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.activeClickableMenu = new CompanyMenu(Mod);
            return;
        }

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < lines.Count && i < 3; i++)
        {
            Rectangle card = LineCard(i);
            if (!card.Contains(x, y))
                continue;
            ProductionJob? job = Mod.Production.GetLineJob(lines[i].Id);
            ProductionRecipeDefinition? recipe = job is not null
                ? Mod.Production.FindRecipe(job.RecipeKey)
                : Mod.Recipes.FirstOrDefault(p => string.Equals(p.LineType, lines[i].LineType, StringComparison.OrdinalIgnoreCase));
            if (recipe is not null)
                SelectedRecipeKey = recipe.Key;
            Game1.playSound("smallSelect");
            return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedRecipeKey);
        if (selected is not null)
        {
            if (OneBatchButton().Contains(x, y))
            {
                bool ok = Mod.Production.TryStart(selected.Key, 1, out string message);
                Message = message;
                Game1.playSound(ok ? "Ship" : "cancel");
                return;
            }
            if (MaxBatchButton().Contains(x, y))
            {
                int max = Mod.Production.GetMaxBatches(selected);
                int batches = Math.Clamp(max, 1, 10);
                bool ok = Mod.Production.TryStart(selected.Key, batches, out string message);
                Message = message;
                Game1.playSound(ok ? "Ship" : "cancel");
                return;
            }
        }

        if (AddPlanButton().Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new ProductCatalogMenu(Mod);
            return;
        }

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int index = start + row;
            if (index >= plans.Count)
                break;
            ProductionPlanEntry plan = plans[index];
            if (!PlanRow(row).Contains(x, y))
                continue;

            SelectedRecipeKey = plan.RecipeKey;
            if (PlanUpButton(row).Contains(x, y))
            {
                bool ok = Mod.Production.TryMovePlan(plan.Id, -1, out string message);
                Message = message;
                Game1.playSound(ok ? "shiny4" : "cancel");
            }
            else if (PlanDownButton(row).Contains(x, y))
            {
                bool ok = Mod.Production.TryMovePlan(plan.Id, 1, out string message);
                Message = message;
                Game1.playSound(ok ? "shiny4" : "cancel");
            }
            else if (PlanRemoveButton(row).Contains(x, y))
            {
                bool ok = Mod.Production.TryRemovePlan(plan.Id, out string message);
                Message = message;
                Game1.playSound(ok ? "trashcan" : "cancel");
            }
            else
                Game1.playSound("smallSelect");
            return;
        }
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int index = start + row;
            if (index >= plans.Count || !PlanRow(row).Contains(x, y))
                continue;
            bool ok = Mod.Production.TryRemovePlan(plans[index].Id, out string message);
            Message = message;
            Game1.playSound(ok ? "trashcan" : "cancel");
            return;
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int maxPage = Math.Max(0, (Mod.Production.GetPlans().Count - 1) / 5);
        if (direction < 0 && PlanPage < maxPage)
            PlanPage++;
        else if (direction > 0 && PlanPage > 0)
            PlanPage--;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.64f);
        DrawOuterFrame(b);
        DrawHeader(b);
        DrawStatusCards(b);
        DrawLines(b);
        DrawCurrentProduction(b);
        DrawPlans(b);
        DrawIntermediate(b);
        DrawFinished(b);
        DrawMessage(b);
        drawMouse(b);
    }

    private void DrawOuterFrame(SpriteBatch b)
    {
        Rectangle r = D(0, 0, DesignW, DesignH);
        Fill(b, r, WoodDark);
        Fill(b, Inset(r, S(5)), Wood);
        Fill(b, Inset(r, S(12)), WoodLight);
        Fill(b, Inset(r, S(18)), Paper3);

        // Decorative timber rails.
        Fill(b, D(20, 90, 1360, 7), WoodDark);
        Fill(b, D(20, 785, 1360, 7), WoodDark);
        for (int x = 24; x < 1380; x += 68)
            Fill(b, D(x, 5, 28, 6), new Color(83, 49, 24));
    }

    private void DrawHeader(SpriteBatch b)
    {
        Rectangle company = CompanyButton();
        DrawWoodButton(b, company, false);
        DrawPixelSun(b, D(35, 34, 34, 34));
        DrawText(b, Game1.dialogueFont, SafeCompanyName(), D(82, 25, 150, 50), new Color(250, 226, 155), 0.82f, false);

        Rectangle title = D(400, 13, 630, 75);
        DrawPlaque(b, title, GreenDark);
        string titleText = $"{SafeCompanyName()} · 생산 관리 2.0";
        DrawText(b, Game1.dialogueFont, titleText, title, new Color(252, 222, 139), 1.12f, true);
        DrawLeaf(b, D(372, 30, 28, 35), false);
        DrawLeaf(b, D(1030, 30, 28, 35), true);

        Rectangle close = CloseButton();
        DrawWoodButton(b, close, false);
        Fill(b, D(1344, 34, 24, 5), new Color(129, 39, 25));
        Fill(b, D(1353, 25, 5, 24), new Color(129, 39, 25));
        Fill(b, D(1348, 29, 14, 14), new Color(187, 54, 31));
    }

    private void DrawStatusCards(SpriteBatch b)
    {
        DrawStatCard(b, D(28, 105, 323, 70), 0, "회사 자금", $"{Mod.State.CompanyFunds:N0}G");
        DrawStatCard(b, D(362, 105, 323, 70), 1, "브랜드", Mod.Brand.GetTierName(Mod.State.BrandPoints));
        DrawStatCard(b, D(696, 105, 323, 70), 2, "활성 계약", $"{Mod.State.AcceptedContracts.Count}건");
        DrawStatCard(b, D(1030, 105, 342, 70), 3, "평판", Mod.State.Reputation.ToString("N0"));
    }

    private void DrawStatCard(SpriteBatch b, Rectangle rect, int icon, string label, string value)
    {
        DrawPaperCard(b, rect, Paper);
        Rectangle iconRect = new(rect.X + S(24), rect.Y + S(15), S(42), S(42));
        switch (icon)
        {
            case 0: DrawCoin(b, iconRect); break;
            case 1: DrawShield(b, iconRect); break;
            case 2: DrawScroll(b, iconRect); break;
            default: DrawHeart(b, iconRect); break;
        }
        DrawText(b, Game1.smallFont, label, new Rectangle(rect.X + S(80), rect.Y + S(10), rect.Width - S(92), S(22)), Ink, 0.88f, false);
        DrawText(b, Game1.dialogueFont, value, new Rectangle(rect.X + S(80), rect.Y + S(29), rect.Width - S(92), S(34)), new Color(55, 42, 29), 0.78f, false);
    }

    private void DrawLines(SpriteBatch b)
    {
        Rectangle panel = D(24, 190, 372, 585);
        DrawPaperCard(b, panel, Paper2);
        DrawSectionTitle(b, D(45, 188, 330, 45), "생산 라인");

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < 3; i++)
        {
            ProductionLineState? line = i < lines.Count ? lines[i] : null;
            Rectangle card = LineCard(i);
            DrawPaperCard(b, card, Paper);
            if (line is null)
            {
                DrawText(b, Game1.smallFont, $"라인 {i + 1} · 잠김", D(54, 248 + i * 166, 230, 28), Muted, 0.9f, false);
                continue;
            }

            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            ProductionRecipeDefinition? recipe = job is null ? null : Mod.Production.FindRecipe(job.RecipeKey);
            string lineKind = LineKindName(line.LineType);
            DrawText(b, Game1.smallFont, $"라인 {i + 1} · {lineKind}", D(53, 244 + i * 166, 215, 28), Ink, 0.92f, false);
            DrawStatusPill(b, D(316, 241 + i * 166, 65, 28), job is null ? "대기" : "가동 중", job is null ? Muted : Green);

            DrawMachineIcon(b, D(50, 281 + i * 166, 105, 78), line.LineType, job is not null);
            if (recipe is not null)
                Mod.Icons.DrawRecipeIcon(b, recipe, D(165, 277 + i * 166, 46, 46));
            string product = recipe?.DisplayName ?? "대기 중";
            DrawText(b, Game1.smallFont, product, D(218, 280 + i * 166, 156, 30), Ink, 0.92f, false);

            string stage = job is null ? "작업 없음" : Mod.Production.GetCurrentStageName(job);
            DrawText(b, Game1.smallFont, "현재 단계", D(166, 319 + i * 166, 82, 24), Muted, 0.76f, false);
            DrawText(b, Game1.smallFont, stage, D(248, 319 + i * 166, 126, 24), job is null ? Muted : Green, 0.80f, false);

            float progress = job is null ? 0f : (float)Mod.Production.GetJobProgress(job);
            DrawProgress(b, D(166, 349 + i * 166, 150, 15), progress);
            DrawText(b, Game1.smallFont, $"{Math.Clamp((int)(progress * 100), 0, 100)}%", D(322, 342 + i * 166, 53, 25), Ink, 0.82f, false);

            int efficiency = job?.EfficiencyPercent ?? Mod.Production.GetLineEfficiency(line);
            string remain = job is null ? "-" : ProductionCore.FormatDuration(job.RemainingMinutes);
            DrawClock(b, D(52, 373 + i * 166, 24, 24));
            DrawText(b, Game1.smallFont, $"{remain} 남음", D(82, 371 + i * 166, 125, 24), Ink, 0.74f, false);
            DrawLeaf(b, D(249, 371 + i * 166, 20, 22), false);
            DrawText(b, Game1.smallFont, $"효율 {efficiency}%", D(275, 371 + i * 166, 100, 24), Green, 0.74f, false);
        }

        DrawWoodButton(b, D(42, 730, 338, 38), false);
        DrawGear(b, D(135, 737, 25, 25));
        DrawText(b, Game1.smallFont, "작업 배정", D(167, 734, 120, 30), Ink, 0.88f, false);
    }

    private void DrawCurrentProduction(SpriteBatch b)
    {
        Rectangle panel = D(408, 190, 565, 585);
        DrawPaperCard(b, panel, Paper2);
        DrawSectionTitle(b, D(435, 188, 510, 45), "현재 생산 상세");

        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedRecipeKey) ?? Mod.Recipes.FirstOrDefault();
        if (recipe is null)
            return;
        ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, recipe.Key, StringComparison.OrdinalIgnoreCase));
        ProductionForecast forecast = active is null ? Mod.Quality.GetForecast(recipe, 1) : Mod.Quality.GetForecast(active);

        Mod.Icons.DrawRecipeIcon(b, recipe, D(610, 238, 54, 54));
        DrawText(b, Game1.dialogueFont, recipe.DisplayName, D(675, 238, 240, 46), Ink, 0.82f, false);
        DrawStageFlow(b, recipe, active);

        Rectangle analysis = D(432, 486, 516, 196);
        DrawPaperCard(b, analysis, new Color(247, 229, 185));
        DrawMetricIcon(b, D(448, 510, 30, 30), 0);
        DrawMetricRow(b, "진행률", $"{Math.Clamp((int)((active is null ? 0f : (float)Mod.Production.GetJobProgress(active)) * 100), 0, 100)}%", 500);
        DrawMetricIcon(b, D(448, 550, 30, 30), 1);
        DrawMetricRow(b, "예상 생산량", $"{forecast.MinOutput} ~ {forecast.MaxOutput}{recipe.OutputUnit}", 540);
        DrawMetricIcon(b, D(448, 590, 30, 30), 2);
        DrawMetricRow(b, "예상 등급", forecast.MostLikelyGrade, 580);
        DrawMetricIcon(b, D(448, 630, 30, 30), 3);
        DrawMetricRow(b, "예상 시간", ProductionCore.FormatDuration(active?.RemainingMinutes ?? recipe.DurationMinutes), 620);

        Rectangle quality = D(735, 500, 195, 165);
        DrawPaperCard(b, quality, Paper);
        DrawText(b, Game1.smallFont, "품질 요약", D(760, 508, 145, 28), Ink, 0.90f, true);
        DrawGradeChance(b, "S", forecast.SChance, 542, Gold);
        DrawGradeChance(b, "A", forecast.AChance, 574, GreenLight);
        DrawGradeChance(b, "B", forecast.BChance, 606, Blue);
        DrawGradeChance(b, "C", forecast.CChance, 638, new Color(184, 112, 55));

        DrawWoodButton(b, OneBatchButton(), true);
        DrawPlus(b, D(493, 704, 30, 30), GreenLight);
        DrawText(b, Game1.dialogueFont, "1배치 추가", D(535, 696, 150, 45), new Color(243, 226, 164), 0.70f, false);
        DrawWoodButton(b, MaxBatchButton(), false, Blue);
        DrawGear(b, D(735, 704, 30, 30), Color.White);
        DrawText(b, Game1.dialogueFont, "최대 생산", D(776, 696, 150, 45), Color.White, 0.70f, false);
    }

    private void DrawStageFlow(SpriteBatch b, ProductionRecipeDefinition recipe, ProductionJob? active)
    {
        List<(string Name, int StageIndex)> nodes = new() { ("원재료", -1) };
        for (int i = 0; i < recipe.Stages.Count && i < 5; i++)
            nodes.Add((recipe.Stages[i].DisplayName, i));
        nodes.Add(("완제품", 99));

        int count = nodes.Count;
        float usable = 505f;
        float gap = 8f;
        float nodeW = (usable - gap * (count - 1)) / count;
        int designY = 312;

        for (int i = 0; i < count; i++)
        {
            int dx = 438 + (int)MathF.Round(i * (nodeW + gap));
            Rectangle node = D(dx, designY, (int)nodeW, 145);
            bool current = active is not null && nodes[i].StageIndex >= 0 && nodes[i].StageIndex < 99 && active.CurrentStageIndex == nodes[i].StageIndex;
            if (current)
            {
                Fill(b, D(dx - 5, designY - 6, (int)nodeW + 10, 157), new Color(229, 132, 45));
                Fill(b, D(dx - 1, designY - 2, (int)nodeW + 2, 149), new Color(255, 225, 176));
            }
            else
                Fill(b, node, Paper);

            Rectangle icon = D(dx + Math.Max(2, ((int)nodeW - 48) / 2), designY + 18, 48, 56);
            if (nodes[i].StageIndex == -1 || nodes[i].StageIndex == 99)
                Mod.Icons.DrawRecipeIcon(b, recipe, icon);
            else
                DrawStageIcon(b, icon, nodes[i].Name, current);

            DrawText(b, Game1.smallFont, nodes[i].Name, D(dx + 2, designY + 88, (int)nodeW - 4, 42), current ? GreenDark : Ink, 0.62f, true);
            if (current)
                DrawTriangle(b, D(dx + (int)nodeW / 2 - 8, designY + 132, 16, 11), Orange);

            if (i < count - 1)
            {
                int arrowX = dx + (int)nodeW + 1;
                DrawArrow(b, D(arrowX, designY + 54, (int)gap + 6, 24));
            }
        }
    }

    private void DrawPlans(SpriteBatch b)
    {
        Rectangle panel = D(985, 190, 390, 585);
        DrawPaperCard(b, panel, Paper2);
        DrawSectionTitle(b, D(1012, 188, 335, 45), "생산 계획");

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            Rectangle r = PlanRow(row);
            DrawPaperCard(b, r, Paper);
            int index = start + row;
            int number = index + 1;
            Rectangle numberBox = new(r.X, r.Y, S(52), r.Height);
            Fill(b, numberBox, GreenDark);
            DrawText(b, Game1.dialogueFont, number.ToString(), numberBox, Color.White, 0.82f, true);

            if (index >= plans.Count)
            {
                DrawText(b, Game1.smallFont, "빈 계획", new Rectangle(r.X + S(72), r.Y + S(25), S(190), S(30)), Muted, 0.78f, false);
                continue;
            }

            ProductionPlanEntry plan = plans[index];
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(plan.RecipeKey);
            if (recipe is not null)
                Mod.Icons.DrawRecipeIcon(b, recipe, new Rectangle(r.X + S(67), r.Y + S(15), S(46), S(46)));
            string name = recipe?.DisplayName ?? plan.RecipeKey;
            DrawText(b, Game1.smallFont, $"{name} × {plan.BatchCount}", new Rectangle(r.X + S(122), r.Y + S(23), S(170), S(31)), Ink, 0.78f, false);

            DrawArrowButton(b, PlanUpButton(row), true);
            DrawArrowButton(b, PlanDownButton(row), false);
            Rectangle dot = new(r.Right - S(28), r.Y + S(32), S(13), S(13));
            bool active = Mod.State.ProductionQueue.Any(p => string.Equals(p.RecipeKey, plan.RecipeKey, StringComparison.OrdinalIgnoreCase));
            DrawStatusDot(b, dot, active ? GreenLight : Blue);
            Rectangle remove = PlanRemoveButton(row);
            Fill(b, remove, new Color(166, 76, 50));
            DrawText(b, Game1.smallFont, "×", remove, Color.White, 0.55f, true);
        }

        DrawWoodButton(b, AddPlanButton(), false);
        DrawPlus(b, D(1093, 706, 28, 28), Ink);
        DrawText(b, Game1.dialogueFont, "계획 추가", D(1130, 696, 140, 44), Ink, 0.70f, false);

        Rectangle check = D(1010, 746, 22, 22);
        DrawPaperCard(b, check, Paper);
        Fill(b, Inset(check, S(5)), Green);
        DrawText(b, Game1.smallFont, "✓", check, Color.White, 0.48f, true);
        DrawText(b, Game1.smallFont, "빈 라인 자동 배정", D(1040, 741, 185, 28), Ink, 0.72f, false);
        DrawText(b, Game1.smallFont, $"{PlanPage + 1}/{Math.Max(1, (plans.Count + 4) / 5)}", D(1288, 742, 62, 28), Muted, 0.62f, true);
    }

    private void DrawIntermediate(SpriteBatch b)
    {
        Rectangle panel = D(24, 800, 650, 140);
        DrawPaperCard(b, panel, Paper);
        DrawBottomTitle(b, D(25, 800, 648, 38), "중간재", 0);
        List<IntermediateStockEntry> rows = Mod.Production.GetIntermediateStock().Where(p => p.Quantity > 0).Take(4).ToList();
        if (rows.Count == 0)
        {
            DrawText(b, Game1.smallFont, "보유 중간재가 없습니다.", D(70, 855, 530, 34), Muted, 0.78f, false);
            return;
        }
        for (int i = 0; i < rows.Count; i++)
        {
            IntermediateStockEntry stock = rows[i];
            int y = 842 + i * 24;
            Mod.Icons.DrawProductIcon(b, stock.Key, D(48, y, 22, 22));
            DrawText(b, Game1.smallFont, stock.DisplayName, D(80, y - 2, 260, 25), Ink, 0.68f, false);
            DrawDottedLine(b, D(340, y + 11, 215, 2));
            DrawText(b, Game1.smallFont, stock.Quantity.ToString("N0"), D(565, y - 2, 72, 25), Ink, 0.70f, true);
        }
    }

    private void DrawFinished(SpriteBatch b)
    {
        Rectangle panel = D(687, 800, 688, 140);
        DrawPaperCard(b, panel, Paper);
        DrawBottomTitle(b, D(688, 800, 686, 38), "완제품", 1);
        List<ProductStockEntry> rows = Mod.State.FinishedGoods.Values
            .Where(p => p is not null && p.Quantity > 0)
            .OrderByDescending(p => p.Quality)
            .ThenByDescending(p => p.Quantity)
            .Take(4)
            .ToList();
        if (rows.Count == 0)
        {
            DrawText(b, Game1.smallFont, "보유 완제품이 없습니다.", D(735, 855, 550, 34), Muted, 0.78f, false);
            return;
        }
        for (int i = 0; i < rows.Count; i++)
        {
            ProductStockEntry stock = rows[i];
            int y = 842 + i * 24;
            Mod.Icons.DrawProductIcon(b, stock.ProductKey, D(715, y, 22, 22));
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(stock.ProductKey);
            string name = recipe?.DisplayName ?? stock.ProductKey;
            DrawText(b, Game1.smallFont, name, D(747, y - 2, 275, 25), Ink, 0.68f, false);
            DrawDottedLine(b, D(1015, y + 11, 115, 2));
            DrawGradeBadge(b, D(1142, y - 1, 58, 23), string.IsNullOrWhiteSpace(stock.Grade) ? "C" : stock.Grade);
            DrawText(b, Game1.smallFont, stock.Quantity.ToString("N0"), D(1250, y - 2, 83, 25), Ink, 0.70f, true);
        }
    }

    private void DrawMessage(SpriteBatch b)
    {
        if (string.IsNullOrWhiteSpace(Message))
            return;
        string text = TrimToWidth(Game1.smallFont, Message, S(1160), 0.56f);
        DrawText(b, Game1.smallFont, text, D(115, 944, 1170, 15), new Color(91, 67, 40), 0.56f, true);
    }

    private void DrawMetricRow(SpriteBatch b, string label, string value, int y)
    {
        DrawText(b, Game1.smallFont, label, D(490, y, 140, 28), Ink, 0.72f, false);
        DrawDottedLine(b, D(625, y + 14, 115, 2));
        DrawText(b, Game1.smallFont, value, D(745, y, 180, 28), Ink, 0.78f, false);
    }

    private void DrawGradeChance(SpriteBatch b, string grade, int chance, int y, Color color)
    {
        DrawStar(b, D(751, y, 23, 23), color);
        DrawText(b, Game1.smallFont, $"{grade}급", D(782, y - 1, 55, 25), Ink, 0.67f, false);
        DrawText(b, Game1.smallFont, $"{chance}%", D(855, y - 1, 60, 25), Ink, 0.67f, true);
    }

    private Rectangle CompanyButton() => D(18, 18, 225, 70);
    private Rectangle CloseButton() => D(1328, 18, 53, 53);
    private Rectangle LineCard(int index) => D(39, 235 + index * 166, 342, 155);
    private Rectangle OneBatchButton() => D(455, 695, 220, 60);
    private Rectangle MaxBatchButton() => D(710, 695, 220, 60);
    private Rectangle PlanRow(int row) => D(1002, 235 + row * 88, 352, 80);
    private Rectangle PlanUpButton(int row) => D(1283, 242 + row * 88, 36, 31);
    private Rectangle PlanDownButton(int row) => D(1283, 275 + row * 88, 36, 31);
    private Rectangle PlanRemoveButton(int row) => D(1324, 287 + row * 88, 20, 20);
    private Rectangle AddPlanButton() => D(1013, 690, 330, 57);

    private Rectangle D(int x, int y, int w, int h)
        => new(OriginX + S(x), OriginY + S(y), Math.Max(1, S(w)), Math.Max(1, S(h)));

    private int S(int value) => (int)MathF.Round(value * UiScale);

    private static Rectangle Inset(Rectangle r, int amount)
        => new(r.X + amount, r.Y + amount, Math.Max(1, r.Width - amount * 2), Math.Max(1, r.Height - amount * 2));

    private static void Fill(SpriteBatch b, Rectangle rect, Color color)
        => b.Draw(Game1.fadeToBlackRect, rect, color);

    private void DrawPaperCard(SpriteBatch b, Rectangle rect, Color fill)
    {
        Fill(b, rect, WoodDark);
        Fill(b, Inset(rect, S(3)), WoodLight);
        Fill(b, Inset(rect, S(6)), fill);
        Fill(b, new Rectangle(rect.X + S(10), rect.Y + S(9), Math.Max(1, rect.Width - S(20)), S(2)), new Color(255, 248, 213) * 0.65f);
    }

    private void DrawWoodButton(SpriteBatch b, Rectangle rect, bool green, Color? overrideFill = null)
    {
        Fill(b, rect, WoodDark);
        Fill(b, Inset(rect, S(3)), green ? GreenDark : WoodLight);
        Fill(b, Inset(rect, S(7)), overrideFill ?? (green ? Green : new Color(207, 154, 82)));
    }

    private void DrawPlaque(SpriteBatch b, Rectangle rect, Color color)
    {
        Fill(b, rect, WoodDark);
        Fill(b, Inset(rect, S(4)), Gold);
        Fill(b, Inset(rect, S(8)), color);
        Fill(b, new Rectangle(rect.X + S(20), rect.Y + S(8), rect.Width - S(40), S(3)), new Color(105, 158, 77) * 0.55f);
        DrawCornerStuds(b, rect);
    }

    private void DrawSectionTitle(SpriteBatch b, Rectangle rect, string title)
    {
        DrawPlaque(b, rect, GreenDark);
        DrawText(b, Game1.dialogueFont, title, rect, new Color(247, 220, 145), 0.72f, true);
    }

    private void DrawBottomTitle(SpriteBatch b, Rectangle rect, string title, int icon)
    {
        Fill(b, rect, WoodDark);
        Fill(b, Inset(rect, S(4)), new Color(121, 74, 32));
        Rectangle iconRect = new(rect.X + S(230), rect.Y + S(6), S(27), S(27));
        if (icon == 0) DrawCrate(b, iconRect); else DrawBoxIcon(b, iconRect);
        DrawText(b, Game1.dialogueFont, title, new Rectangle(rect.X + S(265), rect.Y, S(140), rect.Height), new Color(247, 221, 150), 0.65f, false);
    }

    private void DrawStatusPill(SpriteBatch b, Rectangle rect, string text, Color color)
    {
        Fill(b, rect, WoodDark);
        Fill(b, Inset(rect, S(2)), color);
        DrawText(b, Game1.smallFont, text, rect, Color.White, 0.58f, true);
    }

    private void DrawProgress(SpriteBatch b, Rectangle rect, float progress)
    {
        progress = Math.Clamp(progress, 0f, 1f);
        Fill(b, rect, new Color(122, 105, 76));
        Fill(b, Inset(rect, S(2)), new Color(216, 203, 161));
        Rectangle inner = Inset(rect, S(3));
        Rectangle fill = new(inner.X, inner.Y, Math.Max(0, (int)(inner.Width * progress)), inner.Height);
        if (fill.Width > 0)
        {
            Fill(b, fill, Green);
            Fill(b, new Rectangle(fill.X, fill.Y, fill.Width, Math.Max(1, S(3))), GreenLight);
        }
    }

    private void DrawText(SpriteBatch b, SpriteFont font, string text, Rectangle rect, Color color, float relativeScale, bool centered)
    {
        float scale = UiScale * relativeScale;
        string fitted = TrimToWidth(font, text, rect.Width, relativeScale);
        Vector2 size = font.MeasureString(fitted) * scale;
        float x = centered ? rect.X + (rect.Width - size.X) / 2f : rect.X;
        float y = rect.Y + (rect.Height - size.Y) / 2f;
        b.DrawString(font, fitted, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    private string TrimToWidth(SpriteFont font, string text, int actualWidth, float relativeScale)
    {
        float scale = Math.Max(0.05f, UiScale * relativeScale);
        if (font.MeasureString(text).X * scale <= actualWidth)
            return text;
        string value = text;
        while (value.Length > 2 && font.MeasureString(value + "…").X * scale > actualWidth)
            value = value[..^1];
        return value + "…";
    }

    private void DrawArrowButton(SpriteBatch b, Rectangle rect, bool up)
    {
        DrawPaperCard(b, rect, new Color(238, 211, 163));
        int cx = rect.X + rect.Width / 2;
        int cy = rect.Y + rect.Height / 2;
        Color c = new(111, 78, 42);
        if (up)
        {
            Fill(b, new Rectangle(cx - S(2), cy - S(3), S(4), S(10)), c);
            DrawTriangle(b, new Rectangle(cx - S(8), cy - S(9), S(16), S(9)), c);
        }
        else
        {
            Fill(b, new Rectangle(cx - S(2), cy - S(7), S(4), S(10)), c);
            DrawTriangleDown(b, new Rectangle(cx - S(8), cy, S(16), S(9)), c);
        }
    }

    private void DrawArrow(SpriteBatch b, Rectangle rect)
    {
        int cy = rect.Y + rect.Height / 2;
        Fill(b, new Rectangle(rect.X, cy - S(2), Math.Max(1, rect.Width - S(7)), S(4)), Green);
        DrawTriangleRight(b, new Rectangle(rect.Right - S(9), cy - S(7), S(9), S(14)), Green);
    }

    private void DrawStatusDot(SpriteBatch b, Rectangle rect, Color color)
    {
        Fill(b, new Rectangle(rect.X + rect.Width / 3, rect.Y, rect.Width / 3, rect.Height), color);
        Fill(b, new Rectangle(rect.X, rect.Y + rect.Height / 3, rect.Width, rect.Height / 3), color);
        Fill(b, new Rectangle(rect.X + S(2), rect.Y + S(2), Math.Max(1, rect.Width - S(4)), Math.Max(1, rect.Height - S(4))), color);
    }

    private void DrawDottedLine(SpriteBatch b, Rectangle rect)
    {
        int dot = Math.Max(1, S(3));
        int gap = Math.Max(2, S(7));
        for (int x = rect.X; x < rect.Right; x += gap)
            Fill(b, new Rectangle(x, rect.Y, dot, Math.Max(1, rect.Height)), new Color(188, 151, 98));
    }

    private void DrawGradeBadge(SpriteBatch b, Rectangle rect, string grade)
    {
        string g = grade.Trim().ToUpperInvariant();
        Color c = g switch { "S" => Purple, "A" => new Color(112, 153, 62), "B" => new Color(76, 127, 171), _ => new Color(185, 126, 70) };
        Fill(b, rect, WoodDark);
        Fill(b, Inset(rect, S(2)), c);
        Fill(b, Inset(rect, S(5)), new Color(242, 225, 177));
        DrawText(b, Game1.smallFont, $"{g}급", rect, Ink, 0.58f, true);
    }

    private void DrawCornerStuds(SpriteBatch b, Rectangle rect)
    {
        Color stud = new(210, 165, 55);
        int s = Math.Max(2, S(5));
        Fill(b, new Rectangle(rect.X + S(7), rect.Y + S(7), s, s), stud);
        Fill(b, new Rectangle(rect.Right - S(12), rect.Y + S(7), s, s), stud);
        Fill(b, new Rectangle(rect.X + S(7), rect.Bottom - S(12), s, s), stud);
        Fill(b, new Rectangle(rect.Right - S(12), rect.Bottom - S(12), s, s), stud);
    }

    private string SafeCompanyName() => string.IsNullOrWhiteSpace(Mod.State.CompanyName) ? "새별 농업" : Mod.State.CompanyName;

    private static string LineKindName(string type) => type switch
    {
        "Beverage" => "음료",
        "Packaging" => "포장",
        "Fermentation" => "발효",
        _ => type
    };

    // Pixel-art icons -------------------------------------------------------
    private void DrawCoin(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(132, 84, 17));
        Fill(b, Inset(r, S(4)), Gold);
        Fill(b, Inset(r, S(9)), new Color(255, 214, 71));
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y + S(9), S(6), r.Height - S(18)), new Color(170, 104, 21));
        Fill(b, new Rectangle(r.X + S(10), r.Y + r.Height / 2 - S(3), r.Width - S(20), S(6)), new Color(170, 104, 21));
    }

    private void DrawShield(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X + S(5), r.Y, r.Width - S(10), r.Height - S(8)), new Color(169, 120, 30));
        Fill(b, new Rectangle(r.X + S(9), r.Y + S(4), r.Width - S(18), r.Height - S(15)), Green);
        DrawStar(b, new Rectangle(r.X + r.Width / 2 - S(8), r.Y + S(10), S(16), S(16)), new Color(246, 219, 121));
        DrawTriangleDown(b, new Rectangle(r.X + S(9), r.Bottom - S(13), r.Width - S(18), S(13)), new Color(169, 120, 30));
    }

    private void DrawScroll(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X + S(7), r.Y + S(3), r.Width - S(14), r.Height - S(6)), new Color(239, 215, 166));
        Fill(b, new Rectangle(r.X + S(3), r.Y + S(2), r.Width - S(6), S(7)), new Color(142, 88, 35));
        Fill(b, new Rectangle(r.X + S(3), r.Bottom - S(9), r.Width - S(6), S(7)), new Color(142, 88, 35));
        Fill(b, new Rectangle(r.X + S(12), r.Y + S(15), r.Width - S(24), S(3)), new Color(130, 87, 46));
        Fill(b, new Rectangle(r.X + S(12), r.Y + S(24), r.Width - S(20), S(3)), new Color(130, 87, 46));
    }

    private void DrawHeart(SpriteBatch b, Rectangle r)
    {
        Color pink = new(214, 68, 102);
        Fill(b, new Rectangle(r.X + S(5), r.Y + S(8), S(14), S(14)), pink);
        Fill(b, new Rectangle(r.Right - S(19), r.Y + S(8), S(14), S(14)), pink);
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(14), r.Width - S(16), S(16)), pink);
        DrawTriangleDown(b, new Rectangle(r.X + S(8), r.Y + S(28), r.Width - S(16), S(13)), pink);
        Fill(b, new Rectangle(r.X + S(9), r.Y + S(8), S(5), S(5)), new Color(255, 166, 185));
    }

    private void DrawPixelSun(SpriteBatch b, Rectangle r)
    {
        Color c = new(245, 181, 48);
        Fill(b, Inset(r, S(10)), c);
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(2), r.Y, S(4), S(9)), c);
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(2), r.Bottom - S(9), S(4), S(9)), c);
        Fill(b, new Rectangle(r.X, r.Y + r.Height / 2 - S(2), S(9), S(4)), c);
        Fill(b, new Rectangle(r.Right - S(9), r.Y + r.Height / 2 - S(2), S(9), S(4)), c);
    }

    private void DrawLeaf(SpriteBatch b, Rectangle r, bool flip)
    {
        Color c = new(67, 132, 43);
        int x = flip ? r.X + r.Width / 2 : r.X;
        Fill(b, new Rectangle(x, r.Y + S(7), r.Width / 2, r.Height / 2), c);
        Fill(b, new Rectangle(r.X + r.Width / 3, r.Y + r.Height / 2, r.Width / 2, r.Height / 2), new Color(91, 157, 51));
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(2), r.Y + S(6), S(4), r.Height - S(5)), new Color(47, 93, 36));
    }

    private void DrawMachineIcon(SpriteBatch b, Rectangle r, string type, bool active)
    {
        Color metal = active ? new Color(80, 92, 82) : new Color(113, 105, 87);
        Color dark = new(61, 55, 46);
        Color highlight = active ? GreenLight : new Color(151, 139, 111);
        if (type == "Packaging")
        {
            Fill(b, new Rectangle(r.X + S(10), r.Y + S(25), r.Width - S(20), S(16)), dark);
            Fill(b, new Rectangle(r.X + S(18), r.Y + S(17), S(15), S(45)), metal);
            Fill(b, new Rectangle(r.Right - S(33), r.Y + S(17), S(15), S(45)), metal);
            DrawBoxIcon(b, new Rectangle(r.X + r.Width / 2 - S(18), r.Y + S(12), S(36), S(36)));
            Fill(b, new Rectangle(r.X + S(18), r.Bottom - S(12), r.Width - S(36), S(6)), highlight);
        }
        else if (type == "Fermentation")
        {
            DrawVat(b, new Rectangle(r.X + S(6), r.Y + S(15), S(43), r.Height - S(20)), active);
            DrawVat(b, new Rectangle(r.X + S(53), r.Y + S(10), S(43), r.Height - S(15)), active);
            Fill(b, new Rectangle(r.X + S(12), r.Bottom - S(8), r.Width - S(24), S(6)), dark);
        }
        else
        {
            Fill(b, new Rectangle(r.X + S(10), r.Y + S(23), S(30), S(43)), metal);
            Fill(b, new Rectangle(r.X + S(15), r.Y + S(29), S(20), S(11)), highlight);
            DrawVat(b, new Rectangle(r.X + S(42), r.Y + S(14), S(46), S(54)), active);
            Fill(b, new Rectangle(r.X + S(86), r.Y + S(35), S(12), S(29)), dark);
            DrawBottle(b, new Rectangle(r.X + S(90), r.Y + S(22), S(12), S(30)), new Color(64, 122, 64));
        }
    }

    private void DrawStageIcon(SpriteBatch b, Rectangle r, string name, bool active)
    {
        string n = name ?? "";
        if (n.Contains("병입") || n.Contains("음료"))
        {
            DrawBottle(b, new Rectangle(r.X + r.Width / 2 - S(9), r.Y + S(4), S(18), r.Height - S(8)), active ? Green : new Color(74, 111, 69));
            return;
        }
        if (n.Contains("포장") || n.Contains("세트"))
        {
            DrawBoxIcon(b, Inset(r, S(7)));
            return;
        }
        if (n.Contains("살균") || n.Contains("가열"))
        {
            DrawVat(b, Inset(r, S(5)), active);
            DrawFlame(b, new Rectangle(r.X + r.Width / 2 - S(8), r.Bottom - S(15), S(16), S(14)));
            return;
        }
        if (n.Contains("숙성") || n.Contains("발효") || n.Contains("염장"))
        {
            DrawVat(b, Inset(r, S(5)), active);
            Fill(b, new Rectangle(r.X + S(7), r.Y + S(12), r.Width - S(14), S(5)), new Color(112, 75, 36));
            return;
        }
        if (n.Contains("세척"))
        {
            Fill(b, new Rectangle(r.X + S(5), r.Y + S(26), r.Width - S(10), S(18)), new Color(84, 116, 126));
            Fill(b, new Rectangle(r.X + S(8), r.Y + S(30), r.Width - S(16), S(5)), new Color(126, 185, 199));
            Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y + S(5), S(6), S(20)), new Color(96, 103, 99));
            return;
        }
        // press / crushing / generic process machine
        Fill(b, new Rectangle(r.X + S(7), r.Y + S(18), r.Width - S(14), r.Height - S(22)), new Color(82, 87, 76));
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(4), r.Y + S(4), S(8), S(24)), new Color(65, 67, 61));
        Fill(b, new Rectangle(r.X + S(12), r.Y + S(29), r.Width - S(24), S(8)), active ? GreenLight : new Color(127, 127, 102));
    }

    private void DrawVat(SpriteBatch b, Rectangle r, bool active)
    {
        Color dark = new(62, 61, 52);
        Color body = active ? new Color(95, 105, 89) : new Color(114, 108, 92);
        Fill(b, new Rectangle(r.X + S(4), r.Y + S(6), r.Width - S(8), r.Height - S(10)), dark);
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(10), r.Width - S(16), r.Height - S(18)), body);
        Fill(b, new Rectangle(r.X + S(3), r.Y + S(4), r.Width - S(6), S(7)), new Color(48, 48, 43));
        Fill(b, new Rectangle(r.X + S(8), r.Y + S(16), r.Width - S(16), S(4)), active ? GreenLight : new Color(151, 134, 91));
    }

    private void DrawBottle(SpriteBatch b, Rectangle r, Color liquid)
    {
        Fill(b, new Rectangle(r.X + r.Width / 3, r.Y, r.Width / 3, Math.Max(2, r.Height / 5)), new Color(83, 58, 38));
        Fill(b, new Rectangle(r.X + r.Width / 5, r.Y + r.Height / 5, r.Width * 3 / 5, r.Height * 4 / 5), new Color(41, 61, 48));
        Fill(b, new Rectangle(r.X + r.Width / 4, r.Y + r.Height / 2, r.Width / 2, r.Height / 3), liquid);
        Fill(b, new Rectangle(r.X + r.Width / 4, r.Y + r.Height / 3, r.Width / 2, Math.Max(1, S(3))), new Color(234, 218, 171));
    }

    private void DrawBoxIcon(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(169, 105, 29));
        Fill(b, Inset(r, S(4)), new Color(235, 166, 49));
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y, S(6), r.Height), new Color(188, 53, 37));
        Fill(b, new Rectangle(r.X, r.Y + r.Height / 3, r.Width, S(6)), new Color(188, 53, 37));
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(11), r.Y - S(5), S(9), S(12)), new Color(188, 53, 37));
        Fill(b, new Rectangle(r.X + r.Width / 2 + S(2), r.Y - S(5), S(9), S(12)), new Color(188, 53, 37));
    }

    private void DrawCrate(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(110, 67, 28));
        Fill(b, Inset(r, S(3)), new Color(188, 126, 45));
        Fill(b, new Rectangle(r.X + S(5), r.Y + r.Height / 2 - S(2), r.Width - S(10), S(4)), new Color(119, 75, 31));
        Fill(b, new Rectangle(r.X + r.Width / 3, r.Y + S(4), S(3), r.Height - S(8)), new Color(119, 75, 31));
        Fill(b, new Rectangle(r.X + r.Width * 2 / 3, r.Y + S(4), S(3), r.Height - S(8)), new Color(119, 75, 31));
    }

    private void DrawClock(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, new Color(92, 67, 39));
        Fill(b, Inset(r, S(3)), new Color(246, 225, 171));
        int cx = r.X + r.Width / 2;
        int cy = r.Y + r.Height / 2;
        Fill(b, new Rectangle(cx - S(1), cy - S(6), S(2), S(7)), Ink);
        Fill(b, new Rectangle(cx, cy, S(5), S(2)), Ink);
    }

    private void DrawGear(SpriteBatch b, Rectangle r, Color? overrideColor = null)
    {
        Color c = overrideColor ?? new Color(91, 65, 35);
        Fill(b, new Rectangle(r.X + S(6), r.Y + S(3), r.Width - S(12), r.Height - S(6)), c);
        Fill(b, new Rectangle(r.X + S(3), r.Y + S(6), r.Width - S(6), r.Height - S(12)), c);
        Fill(b, Inset(r, S(10)), Paper2);
    }

    private void DrawPlus(SpriteBatch b, Rectangle r, Color color)
    {
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y + S(4), S(6), r.Height - S(8)), color);
        Fill(b, new Rectangle(r.X + S(4), r.Y + r.Height / 2 - S(3), r.Width - S(8), S(6)), color);
    }

    private void DrawMetricIcon(SpriteBatch b, Rectangle r, int type)
    {
        Color c = type switch { 0 => Green, 1 => Orange, 2 => Gold, _ => new Color(131, 83, 39) };
        Fill(b, r, WoodDark);
        Fill(b, Inset(r, S(3)), new Color(241, 218, 167));
        if (type == 0) DrawProgress(b, Inset(r, S(7)), 0.75f);
        else if (type == 1) DrawBottle(b, Inset(r, S(7)), c);
        else if (type == 2) DrawStar(b, Inset(r, S(7)), c);
        else DrawClock(b, Inset(r, S(5)));
    }

    private void DrawStar(SpriteBatch b, Rectangle r, Color color)
    {
        Fill(b, new Rectangle(r.X + r.Width / 2 - S(3), r.Y, S(6), r.Height), color);
        Fill(b, new Rectangle(r.X, r.Y + r.Height / 2 - S(3), r.Width, S(6)), color);
        Fill(b, new Rectangle(r.X + r.Width / 4, r.Y + r.Height / 4, r.Width / 2, r.Height / 2), color);
    }

    private void DrawFlame(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X + r.Width / 3, r.Y, r.Width / 3, r.Height), new Color(224, 84, 31));
        DrawTriangle(b, new Rectangle(r.X, r.Y + r.Height / 3, r.Width, r.Height * 2 / 3), new Color(244, 157, 43));
    }

    private static void DrawTriangle(SpriteBatch b, Rectangle r, Color c)
    {
        int rows = Math.Max(1, r.Height);
        for (int i = 0; i < rows; i++)
        {
            float t = (i + 1f) / rows;
            int w = Math.Max(1, (int)(r.Width * t));
            Fill(b, new Rectangle(r.X + (r.Width - w) / 2, r.Bottom - i - 1, w, 1), c);
        }
    }

    private static void DrawTriangleDown(SpriteBatch b, Rectangle r, Color c)
    {
        int rows = Math.Max(1, r.Height);
        for (int i = 0; i < rows; i++)
        {
            float t = 1f - i / (float)rows;
            int w = Math.Max(1, (int)(r.Width * t));
            Fill(b, new Rectangle(r.X + (r.Width - w) / 2, r.Y + i, w, 1), c);
        }
    }

    private static void DrawTriangleRight(SpriteBatch b, Rectangle r, Color c)
    {
        int cols = Math.Max(1, r.Width);
        for (int i = 0; i < cols; i++)
        {
            float t = (i + 1f) / cols;
            int h = Math.Max(1, (int)(r.Height * t));
            Fill(b, new Rectangle(r.X + i, r.Y + (r.Height - h) / 2, 1, h), c);
        }
    }
}
