using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed class Production2Ui
{
    private readonly ModEntry Mod;

    internal Production2Ui(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || e.Button != SButton.MouseLeft || Game1.activeClickableMenu is not CompanyMenu companyMenu)
            return;

        Rectangle productionTab = new(companyMenu.xPositionOnScreen + 18, companyMenu.yPositionOnScreen + 154, 190, 40);
        if (!productionTab.Contains(Game1.getMouseX(), Game1.getMouseY()))
            return;

        Mod.Helper.Input.Suppress(e.Button);
        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new Production2Menu(Mod);
    }
}

internal sealed class Production2Menu : IClickableMenu
{
    private readonly ModEntry Mod;
    private string SelectedRecipeKey;
    private int PlanPage;
    private string Message = "생산계획을 세우면 호환되는 빈 라인에서 자동으로 작업을 시작합니다.";

    private Rectangle Panel;
    private Rectangle Header;
    private Rectangle StatusBar;
    private Rectangle LeftPanel;
    private Rectangle CenterPanel;
    private Rectangle RightPanel;
    private Rectangle IntermediatePanel;
    private Rectangle FinishedPanel;
    private Rectangle Footer;

    private static readonly Color Wood = new(83, 53, 30);
    private static readonly Color WoodDark = new(55, 37, 24);
    private static readonly Color Paper = new(248, 235, 199);
    private static readonly Color PaperAlt = new(238, 221, 180);
    private static readonly Color Green = new(50, 91, 49);
    private static readonly Color Green2 = new(73, 118, 65);
    private static readonly Color GreenSoft = new(204, 226, 182);
    private static readonly Color Gold = new(190, 139, 43);
    private static readonly Color Muted = new(104, 84, 59);
    private static readonly Color Blue = new(48, 92, 126);
    private static readonly Color Red = new(143, 61, 51);
    private static readonly Color Disabled = new(151, 139, 112);

    internal Production2Menu(ModEntry mod)
        : base(0, 0, Game1.viewport.Width, Game1.viewport.Height, true)
    {
        Mod = mod;
        Mod.Production.EnsureState();
        SelectedRecipeKey = Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, "WatermelonJuice", StringComparison.OrdinalIgnoreCase))?.Key
            ?? Mod.Recipes.FirstOrDefault()?.Key
            ?? "";
        RecalculateLayout();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        RecalculateLayout();
    }

    private void RecalculateLayout()
    {
        int w = Math.Min(1440, Math.Max(960, Game1.viewport.Width - 28));
        int h = Math.Min(930, Math.Max(690, Game1.viewport.Height - 28));
        int x = Game1.viewport.Width / 2 - w / 2;
        int y = Game1.viewport.Height / 2 - h / 2;
        Panel = new Rectangle(x, y, w, h);
        Header = new Rectangle(x + 8, y + 8, w - 16, 66);
        StatusBar = new Rectangle(x + 8, y + 78, w - 16, 50);

        int bodyTop = y + 136;
        int bodyHeight = h - 136 - 230;
        int gap = 10;
        int leftW = (int)((w - 16 - gap * 2) * 0.30f);
        int centerW = (int)((w - 16 - gap * 2) * 0.41f);
        int rightW = w - 16 - gap * 2 - leftW - centerW;
        LeftPanel = new Rectangle(x + 8, bodyTop, leftW, bodyHeight);
        CenterPanel = new Rectangle(LeftPanel.Right + gap, bodyTop, centerW, bodyHeight);
        RightPanel = new Rectangle(CenterPanel.Right + gap, bodyTop, rightW, bodyHeight);

        int bottomTop = bodyTop + bodyHeight + 10;
        int bottomHeight = h - (bottomTop - y) - 48;
        int bottomW = (w - 26) / 2;
        IntermediatePanel = new Rectangle(x + 8, bottomTop, bottomW, bottomHeight);
        FinishedPanel = new Rectangle(IntermediatePanel.Right + 10, bottomTop, w - 18 - bottomW, bottomHeight);
        Footer = new Rectangle(x + 8, y + h - 39, w - 16, 31);
        initializeUpperRightCloseButton();
        if (upperRightCloseButton is not null)
        {
            upperRightCloseButton.bounds.X = Panel.Right - upperRightCloseButton.bounds.Width - 14;
            upperRightCloseButton.bounds.Y = Panel.Y + 14;
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            exitThisMenu();
            return;
        }

        if (BackButton().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.activeClickableMenu = new CompanyMenu(Mod);
            return;
        }

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < lines.Count && i < 3; i++)
        {
            if (!LineCard(i).Contains(x, y))
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
                int batches = Math.Clamp(Mod.Production.GetMaxBatches(selected), 1, 10);
                bool ok = Mod.Production.TryStart(selected.Key, batches, out string message);
                Message = message;
                Game1.playSound(ok ? "Ship" : "cancel");
                return;
            }
            if (AddPlanButton().Contains(x, y))
            {
                bool ok = Mod.Production.TryStart(selected.Key, 1, out string message);
                Message = message;
                Game1.playSound(ok ? "Ship" : "cancel");
                return;
            }
        }

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 4;
        for (int row = 0; row < 4; row++)
        {
            int index = start + row;
            if (index >= plans.Count)
                break;
            ProductionPlanEntry plan = plans[index];
            if (PlanRow(row).Contains(x, y))
            {
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
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int maxPage = Math.Max(0, (Mod.Production.GetPlans().Count - 1) / 4);
        if (direction < 0 && PlanPage < maxPage)
            PlanPage++;
        else if (direction > 0 && PlanPage > 0)
            PlanPage--;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.72f);
        DrawFrame(b, Panel, WoodDark);
        DrawHeader(b);
        DrawStatus(b);
        DrawLines(b);
        DrawProductionDetail(b);
        DrawPlans(b);
        DrawIntermediate(b);
        DrawFinishedGoods(b);
        DrawFooter(b);
        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawHeader(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Header, Wood);
        Rectangle title = new(Header.X + Header.Width / 2 - 235, Header.Y + 7, 470, 52);
        DrawFrame(b, title, Green);
        CenterText(b, Game1.dialogueFont, "생 산  관 리", title, new Color(244, 224, 154), -1);

        Rectangle company = new(Header.X + 12, Header.Y + 10, 230, 44);
        DrawFrame(b, company, new Color(102, 65, 32));
        b.DrawString(Game1.dialogueFont, string.IsNullOrWhiteSpace(Mod.State.CompanyName) ? "새별 농업" : Mod.State.CompanyName,
            new Vector2(company.X + 14, company.Y + 7), Color.White);

        Rectangle back = BackButton();
        b.Draw(Game1.fadeToBlackRect, back, Green2);
        CenterText(b, Game1.smallFont, "회사 관리", back, Color.White, 0);
    }

    private void DrawStatus(SpriteBatch b)
    {
        DrawFrame(b, StatusBar, Paper);
        int blockW = StatusBar.Width / 4;
        DrawStatusBlock(b, new Rectangle(StatusBar.X, StatusBar.Y, blockW, StatusBar.Height), "회사 자금", $"{Mod.State.CompanyFunds:N0}G");
        DrawStatusBlock(b, new Rectangle(StatusBar.X + blockW, StatusBar.Y, blockW, StatusBar.Height), "브랜드", Mod.Brand.GetTierName(Mod.State.BrandPoints));
        DrawStatusBlock(b, new Rectangle(StatusBar.X + blockW * 2, StatusBar.Y, blockW, StatusBar.Height), "활성 계약", $"{Mod.State.AcceptedContracts.Count}건");
        DrawStatusBlock(b, new Rectangle(StatusBar.X + blockW * 3, StatusBar.Y, StatusBar.Width - blockW * 3, StatusBar.Height), "평판", Mod.State.Reputation.ToString("N0"));
    }

    private void DrawStatusBlock(SpriteBatch b, Rectangle rect, string label, string value)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.Right - 1, rect.Y + 5, 1, rect.Height - 10), new Color(183, 158, 112));
        b.DrawString(Game1.smallFont, label, new Vector2(rect.X + 14, rect.Y + 7), Muted);
        Vector2 size = Game1.smallFont.MeasureString(value);
        b.DrawString(Game1.smallFont, value, new Vector2(rect.Right - size.X - 14, rect.Y + 25), Green);
    }

    private void DrawLines(SpriteBatch b)
    {
        DrawPanelTitle(b, LeftPanel, "생산라인");
        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < lines.Count && i < 3; i++)
        {
            ProductionLineState line = lines[i];
            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            Rectangle card = LineCard(i);
            DrawFrame(b, card, job is null ? PaperAlt : new Color(235, 242, 211));
            string status = job is null ? "대기" : "가동 중";
            DrawPill(b, new Rectangle(card.Right - 87, card.Y + 8, 70, 25), status, job is null ? Disabled : Green2);
            b.DrawString(Game1.smallFont, line.DisplayName, new Vector2(card.X + 13, card.Y + 9), WoodDark);

            if (job is null)
            {
                b.DrawString(Game1.smallFont, "작업 없음", new Vector2(card.X + 20, card.Y + 54), Muted);
                b.DrawString(Game1.smallFont, $"효율 {Mod.Production.GetLineEfficiency(line)}%", new Vector2(card.X + 20, card.Bottom - 31), Green);
                continue;
            }

            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(job.RecipeKey);
            string name = recipe?.DisplayName ?? job.RecipeKey;
            b.DrawString(Game1.dialogueFont, name, new Vector2(card.X + 18, card.Y + 38), WoodDark);
            b.DrawString(Game1.smallFont, $"{Mod.Production.GetCurrentStageName(job)} 공정", new Vector2(card.X + 20, card.Y + 76), Muted);
            Rectangle progress = new(card.X + 20, card.Y + 104, card.Width - 95, 14);
            DrawProgress(b, progress, Mod.Production.GetJobProgress(job));
            b.DrawString(Game1.smallFont, $"{(int)(Mod.Production.GetJobProgress(job) * 100)}%", new Vector2(progress.Right + 8, progress.Y - 5), WoodDark);
            b.DrawString(Game1.smallFont, $"{ProductionCore.FormatDuration(job.RemainingMinutes)} 남음", new Vector2(card.X + 20, card.Bottom - 30), Muted);
            b.DrawString(Game1.smallFont, $"효율 {job.EfficiencyPercent}%", new Vector2(card.Right - 106, card.Bottom - 30), Green);
        }
    }

    private void DrawProductionDetail(SpriteBatch b)
    {
        DrawPanelTitle(b, CenterPanel, "현재 생산 상세");
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedRecipeKey) ?? Mod.Recipes.FirstOrDefault();
        if (recipe is null)
            return;

        int contentTop = CenterPanel.Y + 45;
        b.DrawString(Game1.dialogueFont, recipe.DisplayName, new Vector2(CenterPanel.X + 18, contentTop), WoodDark);
        b.DrawString(Game1.smallFont, recipe.Description, new Vector2(CenterPanel.X + 20, contentTop + 40), Muted);

        ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, recipe.Key, StringComparison.OrdinalIgnoreCase));
        DrawStageFlow(b, recipe, active, contentTop + 78);

        int infoY = contentTop + 180;
        Rectangle info = new(CenterPanel.X + 15, infoY, CenterPanel.Width - 30, Math.Max(165, CenterPanel.Bottom - infoY - 72));
        DrawFrame(b, info, PaperAlt);
        string ingredientName = GetIngredientName(recipe);
        int batches = 1;
        int lineEfficiency = Mod.Production.GetLineEfficiency(Mod.State.ProductionLines.FirstOrDefault(p => string.Equals(p.LineType, recipe.LineType, StringComparison.OrdinalIgnoreCase)));
        int output = Mod.Production.EstimateOutputQuantity(recipe, batches, lineEfficiency);
        string grade = Mod.Production.EstimateGrade(recipe);
        string qualityMix = GetQualityMix(recipe);

        DrawInfoRow(b, info, 0, "1배치 재료", $"{ingredientName} {recipe.InputQuantity}개");
        DrawInfoRow(b, info, 1, "예상 생산량", $"{output}{recipe.OutputUnit}");
        DrawInfoRow(b, info, 2, "예상 등급", $"{grade}급");
        DrawInfoRow(b, info, 3, "예상 시간", ProductionCore.FormatDuration(Mod.Production.GetRecipeTotalMinutes(recipe)));
        DrawInfoRow(b, info, 4, "원재료 품질 영향", qualityMix);

        DrawButton(b, OneBatchButton(), "+ 1배치 추가", Green2, Color.White);
        DrawButton(b, MaxBatchButton(), "최대 생산", Blue, Color.White);
    }

    private void DrawStageFlow(SpriteBatch b, ProductionRecipeDefinition recipe, ProductionJob? active, int y)
    {
        int left = CenterPanel.X + 18;
        int usable = CenterPanel.Width - 36;
        int count = Math.Max(1, recipe.Stages.Count + 2);
        int boxW = Math.Max(50, usable / count - 8);
        int gap = Math.Max(4, (usable - boxW * count) / Math.Max(1, count - 1));

        Rectangle raw = new(left, y, boxW, 58);
        DrawStageBox(b, raw, GetIngredientName(recipe), false, false);
        int x = raw.Right + gap;
        for (int i = 0; i < recipe.Stages.Count; i++)
        {
            DrawArrow(b, x - gap + 1, y + 28, gap - 2);
            Rectangle stage = new(x, y, boxW, 58);
            bool current = active is not null && active.CurrentStageIndex == i;
            bool done = active is not null && active.CurrentStageIndex > i;
            DrawStageBox(b, stage, recipe.Stages[i].DisplayName, current, done);
            x = stage.Right + gap;
        }
        DrawArrow(b, x - gap + 1, y + 28, gap - 2);
        DrawStageBox(b, new Rectangle(x, y, boxW, 58), recipe.DisplayName, false, active is not null && active.RemainingMinutes <= 0);
    }

    private void DrawStageBox(SpriteBatch b, Rectangle rect, string text, bool current, bool done)
    {
        Color fill = current ? new Color(247, 199, 116) : done ? GreenSoft : PaperAlt;
        DrawFrame(b, rect, fill);
        CenterText(b, Game1.smallFont, text, rect, current ? Red : WoodDark, 0);
        if (current)
            b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X + 4, rect.Bottom - 5, rect.Width - 8, 3), Gold);
    }

    private void DrawArrow(SpriteBatch b, int x, int y, int width)
    {
        if (width <= 2) return;
        b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, width, 3), Green);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(x + width - 4, y - 3, 4, 9), Green);
    }

    private void DrawPlans(SpriteBatch b)
    {
        DrawPanelTitle(b, RightPanel, "생산 계획");
        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 4;
        for (int row = 0; row < 4; row++)
        {
            Rectangle rect = PlanRow(row);
            DrawFrame(b, rect, row % 2 == 0 ? Paper : PaperAlt);
            int index = start + row;
            if (index >= plans.Count)
            {
                b.DrawString(Game1.smallFont, "대기 계획 없음", new Vector2(rect.X + 18, rect.Y + 25), Disabled);
                continue;
            }

            ProductionPlanEntry plan = plans[index];
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(plan.RecipeKey);
            Rectangle num = new(rect.X + 8, rect.Y + 8, 38, rect.Height - 16);
            b.Draw(Game1.fadeToBlackRect, num, Green);
            CenterText(b, Game1.dialogueFont, (index + 1).ToString(), num, Color.White, 0);
            b.DrawString(Game1.smallFont, $"{recipe?.DisplayName ?? plan.RecipeKey} ×{plan.BatchCount}", new Vector2(rect.X + 57, rect.Y + 16), WoodDark);
            b.DrawString(Game1.smallFont, recipe?.LineType switch { "Packaging" => "포장 라인", "Fermentation" => "발효 라인", _ => "음료 라인" }, new Vector2(rect.X + 57, rect.Y + 42), Muted);
            DrawTinyButton(b, PlanUpButton(row), "▲");
            DrawTinyButton(b, PlanDownButton(row), "▼");
            DrawTinyButton(b, PlanRemoveButton(row), "×", Red);
        }

        DrawButton(b, AddPlanButton(), "+ 계획 추가", new Color(175, 132, 66), Color.White);
        int maxPage = Math.Max(0, (plans.Count - 1) / 4);
        b.DrawString(Game1.smallFont, $"계획 {plans.Count}/{Mod.Production.GetPlanLimit()} · {PlanPage + 1}/{maxPage + 1}", new Vector2(RightPanel.X + 16, RightPanel.Bottom - 27), Muted);
    }

    private void DrawIntermediate(SpriteBatch b)
    {
        DrawBottomTitle(b, IntermediatePanel, "중간재 창고");
        IReadOnlyList<IntermediateStockEntry> rows = Mod.Production.GetIntermediateStock();
        int y = IntermediatePanel.Y + 39;
        if (rows.Count == 0)
        {
            b.DrawString(Game1.smallFont, "현재 공정 사이에 보관 중인 중간재가 없습니다.", new Vector2(IntermediatePanel.X + 18, y + 24), Muted);
            return;
        }
        foreach (IntermediateStockEntry stock in rows.Take(4))
        {
            DrawInventoryRow(b, new Rectangle(IntermediatePanel.X + 10, y, IntermediatePanel.Width - 20, 32), stock.DisplayName, $"{stock.Grade}급", stock.Quantity.ToString("N0"));
            y += 34;
        }
    }

    private void DrawFinishedGoods(SpriteBatch b)
    {
        DrawBottomTitle(b, FinishedPanel, "완제품 재고");
        List<ProductStockEntry> rows = Mod.State.FinishedGoods.Values
            .Where(p => p is not null && p.Quantity > 0)
            .OrderByDescending(p => p.Quality)
            .ThenByDescending(p => p.Quantity)
            .Take(4)
            .ToList();
        int y = FinishedPanel.Y + 39;
        if (rows.Count == 0)
        {
            b.DrawString(Game1.smallFont, "생산 완료된 제품이 없습니다.", new Vector2(FinishedPanel.X + 18, y + 24), Muted);
            return;
        }
        foreach (ProductStockEntry stock in rows)
        {
            string name = Mod.Production.FindRecipe(stock.ProductKey)?.DisplayName ?? stock.ProductKey;
            string grade = string.IsNullOrWhiteSpace(stock.Grade) ? ProductionCore.GradeFromQuality(stock.Quality) : stock.Grade;
            DrawInventoryRow(b, new Rectangle(FinishedPanel.X + 10, y, FinishedPanel.Width - 20, 32), name, $"{grade}급", stock.Quantity.ToString("N0"));
            y += 34;
        }
    }

    private void DrawInventoryRow(SpriteBatch b, Rectangle rect, string name, string grade, string quantity)
    {
        b.Draw(Game1.fadeToBlackRect, rect, PaperAlt);
        b.DrawString(Game1.smallFont, name, new Vector2(rect.X + 10, rect.Y + 7), WoodDark);
        DrawPill(b, new Rectangle(rect.Right - 145, rect.Y + 4, 55, 24), grade, grade.StartsWith("S") ? new Color(132, 92, 153) : grade.StartsWith("A") ? Green2 : Blue);
        Vector2 size = Game1.smallFont.MeasureString(quantity);
        b.DrawString(Game1.smallFont, quantity, new Vector2(rect.Right - size.X - 12, rect.Y + 7), WoodDark);
    }

    private void DrawFooter(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Footer, WoodDark);
        b.DrawString(Game1.smallFont, "원재료 → 중간재 → 완제품 흐름으로 생산계획을 세우고 각 라인을 운영하세요.", new Vector2(Footer.X + 14, Footer.Y + 6), new Color(241, 219, 157));
        Vector2 msgSize = Game1.smallFont.MeasureString(Message);
        if (msgSize.X < Footer.Width * 0.42f)
            b.DrawString(Game1.smallFont, Message, new Vector2(Footer.Right - msgSize.X - 14, Footer.Y + 6), Color.White);
    }

    private string GetIngredientName(ProductionRecipeDefinition recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientItemId))
            return Mod.Crops.FirstOrDefault(p => string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? "원재료";
        return Mod.Crops.FirstOrDefault(p => string.Equals(p.Family, recipe.IngredientFamily, StringComparison.OrdinalIgnoreCase))?.FamilyDisplayName?.Replace(" 계열", "") ?? recipe.IngredientFamily;
    }

    private string GetQualityMix(ProductionRecipeDefinition recipe)
    {
        HashSet<string>? familyIds = null;
        if (string.IsNullOrWhiteSpace(recipe.IngredientItemId) && !string.IsNullOrWhiteSpace(recipe.IngredientFamily))
            familyIds = Mod.Crops.Where(p => string.Equals(p.Family, recipe.IngredientFamily, StringComparison.OrdinalIgnoreCase)).Select(p => p.ItemId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<WarehouseStockEntry> entries = Mod.State.Warehouse.Values
            .Where(p => p is not null && p.Quantity > 0)
            .Where(p => !string.IsNullOrWhiteSpace(recipe.IngredientItemId)
                ? string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase)
                : familyIds is not null && familyIds.Contains(p.ItemId))
            .ToList();
        int total = entries.Sum(p => p.Quantity);
        if (total <= 0)
            return "재고 없음";
        int gold = entries.Where(p => p.Quality == 2).Sum(p => p.Quantity) * 100 / total;
        int silver = entries.Where(p => p.Quality == 1).Sum(p => p.Quantity) * 100 / total;
        int iridium = entries.Where(p => p.Quality == 4).Sum(p => p.Quantity) * 100 / total;
        if (iridium > 0) return $"이리듐 {iridium}% / 금 {gold}%";
        if (gold > 0 || silver > 0) return $"금 {gold}% / 은 {silver}%";
        return "일반 100%";
    }

    private void DrawInfoRow(SpriteBatch b, Rectangle info, int row, string label, string value)
    {
        int y = info.Y + 15 + row * 29;
        b.DrawString(Game1.smallFont, label, new Vector2(info.X + 14, y), Muted);
        Vector2 size = Game1.smallFont.MeasureString(value);
        b.DrawString(Game1.smallFont, value, new Vector2(info.Right - size.X - 14, y), row == 2 ? Gold : WoodDark);
    }

    private void DrawPanelTitle(SpriteBatch b, Rectangle panel, string title)
    {
        DrawFrame(b, panel, Paper);
        Rectangle titleRect = new(panel.X + 35, panel.Y + 8, panel.Width - 70, 33);
        b.Draw(Game1.fadeToBlackRect, titleRect, Green);
        CenterText(b, Game1.smallFont, title, titleRect, new Color(246, 226, 166), 0);
    }

    private void DrawBottomTitle(SpriteBatch b, Rectangle panel, string title)
    {
        DrawFrame(b, panel, Paper);
        Rectangle titleRect = new(panel.X, panel.Y, panel.Width, 34);
        b.Draw(Game1.fadeToBlackRect, titleRect, Wood);
        CenterText(b, Game1.smallFont, title, titleRect, new Color(246, 226, 166), 0);
    }

    private static void DrawFrame(SpriteBatch b, Rectangle rect, Color fill)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, fill);
    }

    private static void DrawProgress(SpriteBatch b, Rectangle rect, float progress)
    {
        b.Draw(Game1.fadeToBlackRect, rect, new Color(76, 72, 52));
        b.Draw(Game1.fadeToBlackRect, new Rectangle(rect.X + 2, rect.Y + 2, (int)((rect.Width - 4) * Math.Clamp(progress, 0f, 1f)), rect.Height - 4), Green2);
    }

    private static void DrawPill(SpriteBatch b, Rectangle rect, string text, Color fill)
    {
        b.Draw(Game1.fadeToBlackRect, rect, fill);
        CenterText(b, Game1.smallFont, text, rect, Color.White, 0);
    }

    private static void DrawButton(SpriteBatch b, Rectangle rect, string text, Color fill, Color textColor)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, fill);
        CenterText(b, Game1.smallFont, text, rect, textColor, 0);
    }

    private static void DrawTinyButton(SpriteBatch b, Rectangle rect, string text, Color? fill = null)
    {
        b.Draw(Game1.fadeToBlackRect, rect, fill ?? new Color(155, 119, 65));
        CenterText(b, Game1.smallFont, text, rect, Color.White, 0);
    }

    private static void CenterText(SpriteBatch b, SpriteFont font, string text, Rectangle rect, Color color, int yOffset)
    {
        Vector2 size = font.MeasureString(text);
        Vector2 pos = new(rect.X + rect.Width / 2f - size.X / 2f, rect.Y + rect.Height / 2f - size.Y / 2f + yOffset);
        b.DrawString(font, text, pos, color);
    }

    private Rectangle BackButton() => new(Header.X + Header.Width - 210, Header.Y + 16, 120, 34);
    private Rectangle LineCard(int index)
    {
        int top = LeftPanel.Y + 49;
        int h = Math.Max(112, (LeftPanel.Height - 62) / 3 - 7);
        return new Rectangle(LeftPanel.X + 10, top + index * (h + 7), LeftPanel.Width - 20, h);
    }
    private Rectangle OneBatchButton() => new(CenterPanel.X + 22, CenterPanel.Bottom - 58, (CenterPanel.Width - 54) / 2, 42);
    private Rectangle MaxBatchButton() => new(OneBatchButton().Right + 10, CenterPanel.Bottom - 58, (CenterPanel.Width - 54) / 2, 42);
    private Rectangle PlanRow(int row)
    {
        int top = RightPanel.Y + 50;
        int h = Math.Max(78, (RightPanel.Height - 118) / 4 - 5);
        return new Rectangle(RightPanel.X + 9, top + row * (h + 6), RightPanel.Width - 18, h);
    }
    private Rectangle PlanUpButton(int row) => new(PlanRow(row).Right - 72, PlanRow(row).Y + 8, 28, 26);
    private Rectangle PlanDownButton(int row) => new(PlanRow(row).Right - 72, PlanRow(row).Y + 38, 28, 26);
    private Rectangle PlanRemoveButton(int row) => new(PlanRow(row).Right - 37, PlanRow(row).Y + 23, 26, 26);
    private Rectangle AddPlanButton() => new(RightPanel.X + 22, RightPanel.Bottom - 65, RightPanel.Width - 44, 38);
}
