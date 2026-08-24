using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class Production100Menu : LiveProductionUi100Base
{
    private string SelectedRecipeKey = "";
    private string SelectedLineId = "";
    private int PlanPage;
    private string Message = "생산 계획과 생산라인 상태가 실시간으로 표시됩니다.";
    private double MessageUntil;

    internal Production100Menu(ModEntry mod)
        : base(mod, "assets/ui_100_production_base.png")
    {
        Mod.Production.EnsureState();
        ProductionJob? job = Mod.State.ProductionQueue.FirstOrDefault();
        SelectedRecipeKey = job?.RecipeKey
            ?? Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, "TomatoJuice", StringComparison.OrdinalIgnoreCase))?.Key
            ?? Mod.Recipes.FirstOrDefault()?.Key ?? "";
        SelectedLineId = job?.LineId ?? Mod.Production.GetLines().FirstOrDefault()?.Id ?? "";
        Show("Live Production UI 0.10.0");
    }

    private void Show(string message, double seconds = 3.0)
    {
        Message = message;
        MessageUntil = Game1.currentGameTime.TotalGameTime.TotalSeconds + seconds;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y)) { Game1.playSound("bigDeSelect"); exitThisMenu(); return; }
        if (Company().Contains(x, y)) { Game1.playSound("bigDeSelect"); Game1.activeClickableMenu = new CompanyMenu(Mod); return; }
        if (Catalog().Contains(x, y) || PlanAdd().Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new ProductCatalog100Menu(Mod, SelectedRecipeKey);
            return;
        }

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < Math.Min(3, lines.Count); i++)
        {
            if (!LineCard(i).Contains(x, y)) continue;
            ProductionLineState line = lines[i];
            SelectedLineId = line.Id;
            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            ProductionRecipeDefinition? recipe = job is null
                ? Mod.Recipes.FirstOrDefault(p => string.Equals(p.LineType, line.LineType, StringComparison.OrdinalIgnoreCase))
                : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is not null) SelectedRecipeKey = recipe.Key;
            Show(job is null ? $"{line.DisplayName}: 현재 대기 중입니다." : $"{line.DisplayName}: {recipe?.DisplayName} 생산 중");
            Game1.playSound("smallSelect");
            return;
        }

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int idx = start + row;
            if (idx >= plans.Count) continue;
            ProductionPlanEntry plan = plans[idx];
            if (PlanUp(row).Contains(x, y))
            {
                bool ok = Mod.Production.TryMovePlan(plan.Id, -1, out string msg); Show(msg); Game1.playSound(ok ? "shiny4" : "cancel"); return;
            }
            if (PlanDown(row).Contains(x, y))
            {
                bool ok = Mod.Production.TryMovePlan(plan.Id, 1, out string msg); Show(msg); Game1.playSound(ok ? "shiny4" : "cancel"); return;
            }
            if (PlanRemove(row).Contains(x, y))
            {
                bool ok = Mod.Production.TryRemovePlan(plan.Id, out string msg); Show(msg); Game1.playSound(ok ? "trashcan" : "cancel"); return;
            }
            if (PlanRow(row).Contains(x, y))
            {
                SelectedRecipeKey = plan.RecipeKey;
                Show($"생산계획 {idx + 1}: {Mod.Production.FindRecipe(plan.RecipeKey)?.DisplayName ?? plan.RecipeKey} × {plan.BatchCount}");
                Game1.playSound("smallSelect");
                return;
            }
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedRecipeKey);
        if (selected is not null && OneBatch().Contains(x, y))
        {
            bool ok = Mod.Production.TryStart(selected.Key, 1, out string msg);
            Show(msg); Game1.playSound(ok ? "Ship" : "cancel"); return;
        }
        if (selected is not null && MaxBatch().Contains(x, y))
        {
            int max = Math.Min(10, Mod.Production.GetMaxBatches(selected));
            if (max <= 0) { Show($"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다."); Game1.playSound("cancel"); return; }
            bool ok = Mod.Production.TryStart(selected.Key, max, out string msg);
            Show(msg); Game1.playSound(ok ? "Ship" : "cancel");
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
            bool ok = Mod.Production.TryRemovePlan(plans[idx].Id, out string msg);
            Show(msg); Game1.playSound(ok ? "trashcan" : "cancel"); return;
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int max = Math.Max(0, (Mod.Production.GetPlans().Count - 1) / 5);
        if (direction < 0 && PlanPage < max) { PlanPage++; Show($"생산계획 {PlanPage + 1}/{max + 1} 페이지"); }
        else if (direction > 0 && PlanPage > 0) { PlanPage--; Show($"생산계획 {PlanPage + 1}/{max + 1} 페이지"); }
    }

    public override void draw(SpriteBatch b)
    {
        DrawBackground(b);
        DrawStats(b);
        DrawLines(b);
        DrawCurrent(b);
        DrawPlans(b);
        DrawStocks(b);
        DrawMessage(b);
        drawMouse(b);
    }

    private void DrawStats(SpriteBatch b)
    {
        Text(b, Game1.smallFont, "회사 자금", 170, 102, 0.82f);
        Text(b, Game1.dialogueFont, $"{Mod.State.CompanyFunds:N0}G", 170, 126, 0.73f);
        Text(b, Game1.smallFont, "브랜드", 560, 102, 0.82f);
        Text(b, Game1.dialogueFont, Mod.Brand.GetTierName(Mod.State.BrandPoints), 560, 126, 0.66f);
        Text(b, Game1.smallFont, "활성 계약", 930, 102, 0.82f);
        Text(b, Game1.dialogueFont, $"{Mod.State.AcceptedContracts.Count}건", 930, 126, 0.73f);
        Text(b, Game1.smallFont, "평판", 1330, 102, 0.82f);
        Text(b, Game1.dialogueFont, Mod.State.Reputation.ToString("N0"), 1330, 126, 0.73f);
    }

    private void DrawLines(SpriteBatch b)
    {
        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < 3; i++)
        {
            Rectangle card = LineCardImage(i);
            if (i >= lines.Count)
            {
                Text(b, Game1.dialogueFont, $"라인 {i + 1} · 잠김", card.X + 18, card.Y + 12, 0.64f, Muted);
                continue;
            }

            ProductionLineState line = lines[i];
            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            ProductionRecipeDefinition? recipe = job is null ? null : Mod.Production.FindRecipe(job.RecipeKey);
            bool active = job is not null;
            bool selected = string.Equals(line.Id, SelectedLineId, StringComparison.OrdinalIgnoreCase);
            if (selected) Outline(b, H(card.X + 2, card.Y + 2, card.Width - 4, card.Height - 4), Gold, 4);

            Text(b, Game1.dialogueFont, $"라인 {i + 1} · {LineName(line.LineType)}", card.X + 16, card.Y + 10, 0.58f);
            DrawSmallButton(b, new Rectangle(card.Right - 90, card.Y + 9, 68, 28), active ? "가동" : "대기", active ? Green : new Color(102, 74, 45));
            DrawMachineAnimated(b, line.LineType, new Rectangle(card.X + 10, card.Y + 42, 192, 112), active);

            Text(b, Game1.smallFont, recipe?.DisplayName ?? "작업 없음", card.X + 212, card.Y + 52, 0.68f, active ? Ink : Muted);
            Text(b, Game1.smallFont, $"현재 단계  {(job is null ? "대기" : Mod.Production.GetCurrentStageName(job))}", card.X + 212, card.Y + 82, 0.58f, active ? DeepGreen : Muted);
            float progress = job is null ? 0f : Mod.Production.GetJobProgress(job);
            Progress(b, new Rectangle(card.X + 212, card.Y + 111, 176, 16), progress);
            Text(b, Game1.smallFont, $"{(int)Math.Round(progress * 100)}%", card.X + 394, card.Y + 104, 0.54f);
            int efficiency = job?.EfficiencyPercent ?? Mod.Production.GetLineEfficiency(line);
            string remain = job is null ? "-" : ProductionCore.FormatDuration(job.RemainingMinutes);
            Text(b, Game1.smallFont, $"남은 시간 {remain}", card.X + 212, card.Y + 137, 0.52f);
            Text(b, Game1.smallFont, $"효율 {efficiency}%", card.X + 338, card.Y + 137, 0.52f, Green);
        }
    }

    private void DrawCurrent(SpriteBatch b)
    {
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedRecipeKey) ?? Mod.Recipes.FirstOrDefault();
        if (recipe is null) return;
        ProductionJob? job = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, recipe.Key, StringComparison.OrdinalIgnoreCase));
        ProductionForecast forecast = job is null ? Mod.Quality.GetForecast(recipe, 1) : Mod.Quality.GetForecast(job);

        DrawProduct(b, recipe, new Rectangle(694, 232, 90, 90));
        Text(b, Game1.dialogueFont, recipe.DisplayName, 805, 242, 0.78f);
        Text(b, Game1.smallFont, KindName(recipe), 808, 286, 0.67f, Green);

        List<(string Name, int Sprite, int Stage)> nodes = new() { ("원재료", 3, -1) };
        foreach ((ProductionStageDefinition stage, int idx) in recipe.Stages.Take(4).Select((s, i) => (s, i)))
            nodes.Add((stage.DisplayName, ProcessSprite(stage.DisplayName), idx));
        nodes.Add(("완제품", ProductSprite(recipe), 99));
        int n = nodes.Count;
        int totalWidth = 620;
        int nodeW = Math.Max(74, (totalWidth - (n - 1) * 12) / n);
        int startX = 515;
        double time = Game1.currentGameTime.TotalGameTime.TotalSeconds;
        for (int i = 0; i < n; i++)
        {
            int x = startX + i * (nodeW + 12);
            bool current = job is not null && nodes[i].Stage >= 0 && nodes[i].Stage < 99 && job.CurrentStageIndex == nodes[i].Stage;
            if (current)
            {
                float pulse = 0.55f + 0.35f * (float)((Math.Sin(time * 5d) + 1d) / 2d);
                Fill(b, H(x - 3, 333, nodeW + 6, 135), Orange * pulse);
            }
            Fill(b, H(x, 336, nodeW, 128), new Color(255, 233, 190, 235));
            Outline(b, H(x, 336, nodeW, 128), current ? Orange : new Color(153, 106, 54), current ? 4 : 2);
            if (nodes[i].Stage == 99) DrawProduct(b, recipe, new Rectangle(x + 10, 346, nodeW - 20, 77));
            else DrawAtlas(b, nodes[i].Sprite, new Rectangle(x + 10, 346, nodeW - 20, 77));
            TextCentered(b, Game1.smallFont, nodes[i].Name, new Rectangle(x + 2, 425, nodeW - 4, 32), 0.55f, current ? DeepGreen : Ink);
            if (i < n - 1)
            {
                int ax = x + nodeW + 2;
                Fill(b, H(ax, 387, 18, 7), Green);
                Fill(b, H(ax + 14, 382, 7, 17), Green);
            }
        }

        float progress = job is null ? 0f : Mod.Production.GetJobProgress(job);
        Text(b, Game1.smallFont, "진행률", 530, 500, 0.61f);
        Progress(b, new Rectangle(635, 505, 190, 18), progress);
        Text(b, Game1.smallFont, $"{(int)Math.Round(progress * 100)}%", 837, 499, 0.58f);
        Text(b, Game1.smallFont, "예상 생산량", 530, 542, 0.61f);
        Text(b, Game1.dialogueFont, $"{forecast.MinOutput} ~ {forecast.MaxOutput}{recipe.OutputUnit}", 720, 536, 0.58f);
        Text(b, Game1.smallFont, "예상 등급", 530, 580, 0.61f);
        Text(b, Game1.dialogueFont, forecast.MostLikelyGrade, 790, 574, 0.62f);
        Text(b, Game1.smallFont, "예상 시간", 530, 618, 0.61f);
        Text(b, Game1.dialogueFont, ProductionCore.FormatDuration(job?.RemainingMinutes ?? recipe.DurationMinutes), 720, 612, 0.58f);

        Text(b, Game1.smallFont, "품질 요약", 914, 498, 0.65f);
        Grade(b, "S", forecast.SChance, 920, 538, Gold);
        Grade(b, "A", forecast.AChance, 920, 575, new Color(86, 153, 73));
        Grade(b, "B", forecast.BChance, 1042, 538, Blue);
        Grade(b, "C", forecast.CChance, 1042, 575, new Color(190, 111, 53));
    }

    private void Grade(SpriteBatch b, string grade, int chance, int x, int y, Color color)
    {
        Fill(b, H(x, y, 22, 22), color);
        TextCentered(b, Game1.smallFont, grade, new Rectangle(x, y, 22, 22), 0.48f, Color.White);
        Text(b, Game1.smallFont, $"{chance}%", x + 31, y + 1, 0.56f);
    }

    private void DrawPlans(SpriteBatch b)
    {
        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int maxPage = Math.Max(0, (plans.Count - 1) / 5);
        if (PlanPage > maxPage) PlanPage = maxPage;
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            Rectangle r = PlanRowImage(row);
            int idx = start + row;
            if (idx >= plans.Count)
            {
                Text(b, Game1.dialogueFont, "빈 계획", r.X + 28, r.Y + 29, 0.55f, Muted);
                continue;
            }
            ProductionPlanEntry plan = plans[idx];
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(plan.RecipeKey);
            if (recipe is not null) DrawProduct(b, recipe, new Rectangle(r.X + 12, r.Y + 11, 64, 64));
            Text(b, Game1.smallFont, recipe?.DisplayName ?? plan.RecipeKey, r.X + 86, r.Y + 14, 0.60f);
            Text(b, Game1.dialogueFont, $"× {plan.BatchCount}", r.X + 86, r.Y + 44, 0.52f, DeepGreen);
            DrawSmallButton(b, new Rectangle(r.Right - 92, r.Y + 8, 27, 27), "▲", Green);
            DrawSmallButton(b, new Rectangle(r.Right - 60, r.Y + 8, 27, 27), "▼", new Color(120, 85, 44));
            DrawSmallButton(b, new Rectangle(r.Right - 44, r.Y + 47, 30, 27), "X", Red);
        }
        TextCentered(b, Game1.smallFont, $"자동 배정 ON · {PlanPage + 1}/{maxPage + 1}", new Rectangle(1240, 744, 330, 25), 0.52f, DeepGreen);
    }

    private void DrawStocks(SpriteBatch b)
    {
        IReadOnlyList<IntermediateStockEntry> intermediate = Mod.Production.GetIntermediateStock();
        for (int i = 0; i < 7; i++)
        {
            int x = 90 + i * 101;
            if (i >= intermediate.Count)
            {
                TextCentered(b, Game1.smallFont, "빈 슬롯", new Rectangle(x, 825, 91, 66), 0.42f, Muted);
                continue;
            }
            IntermediateStockEntry stock = intermediate[i];
            ProductionRecipeDefinition? recipe = Mod.Recipes.FirstOrDefault(p => string.Equals(p.OutputIntermediateKey, stock.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Key, stock.Key, StringComparison.OrdinalIgnoreCase));
            if (recipe is not null) DrawProduct(b, recipe, new Rectangle(x + 24, 817, 45, 45));
            TextCentered(b, Game1.smallFont, stock.DisplayName, new Rectangle(x + 4, 861, 83, 18), 0.36f);
            TextCentered(b, Game1.smallFont, $"{stock.Grade} · {stock.Quantity}", new Rectangle(x + 4, 879, 83, 18), 0.39f, DeepGreen);
        }

        List<(string Key, string Grade, int Quantity)> finished = Mod.State.FinishedGoods.Values
            .Where(p => p is not null && p.Quantity > 0)
            .GroupBy(p => new { p.ProductKey, p.Grade })
            .Select(g => (g.Key.ProductKey, g.Key.Grade, g.Sum(x => x.Quantity)))
            .OrderByDescending(p => p.Item3)
            .Take(7)
            .ToList();
        for (int i = 0; i < 7; i++)
        {
            int x = 872 + i * 101;
            if (i >= finished.Count)
            {
                TextCentered(b, Game1.smallFont, "빈 슬롯", new Rectangle(x, 825, 91, 66), 0.42f, Muted);
                continue;
            }
            var stock = finished[i];
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(stock.Key);
            if (recipe is not null) DrawProduct(b, recipe, new Rectangle(x + 24, 817, 45, 45));
            TextCentered(b, Game1.smallFont, recipe?.DisplayName ?? stock.Key, new Rectangle(x + 4, 861, 83, 18), 0.36f);
            TextCentered(b, Game1.smallFont, $"{stock.Grade} · {stock.Quantity}", new Rectangle(x + 4, 879, 83, 18), 0.39f, DeepGreen);
        }
    }

    private void DrawMessage(SpriteBatch b)
    {
        if (string.IsNullOrWhiteSpace(Message) || Game1.currentGameTime.TotalGameTime.TotalSeconds > MessageUntil) return;
        Rectangle box = H(520, 904, 632, 28);
        Fill(b, box, new Color(42, 73, 37) * 0.94f);
        Outline(b, box, Gold, 2);
        TextCentered(b, Game1.smallFont, Message, new Rectangle(520, 904, 632, 28), 0.52f, Color.White);
    }

    private Rectangle Company() => H(30, 10, 260, 72);
    private Rectangle Close() => H(1560, 12, 74, 72);
    private Rectangle Catalog() => H(951, 688, 223, 80);
    private Rectangle OneBatch() => H(490, 688, 210, 80);
    private Rectangle MaxBatch() => H(724, 688, 210, 80);
    private Rectangle PlanAdd() => H(1224, 697, 386, 72);
    private Rectangle LineCard(int i) => H(LineCardImage(i).X, LineCardImage(i).Y, LineCardImage(i).Width, LineCardImage(i).Height);
    private Rectangle LineCardImage(int i) => i switch
    {
        0 => new Rectangle(46, 220, 427, 188),
        1 => new Rectangle(46, 409, 427, 186),
        _ => new Rectangle(46, 596, 427, 183)
    };
    private Rectangle PlanRow(int row) => H(PlanRowImage(row).X, PlanRowImage(row).Y, PlanRowImage(row).Width, PlanRowImage(row).Height);
    private Rectangle PlanRowImage(int row) => new(1278, 225 + row * 107, 312, 97);
    private Rectangle PlanUp(int row) { Rectangle r = PlanRowImage(row); return H(r.Right - 92, r.Y + 8, 27, 27); }
    private Rectangle PlanDown(int row) { Rectangle r = PlanRowImage(row); return H(r.Right - 60, r.Y + 8, 27, 27); }
    private Rectangle PlanRemove(int row) { Rectangle r = PlanRowImage(row); return H(r.Right - 44, r.Y + 47, 30, 27); }
}
