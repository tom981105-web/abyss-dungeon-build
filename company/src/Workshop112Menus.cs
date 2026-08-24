using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

/// <summary>0.11.2 replacement for the cramped six-card product book.</summary>
internal sealed class ProductBook112Menu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly string LineFilter;
    private readonly List<(ProductionRecipeDefinition Recipe, Rectangle Bounds)> Cards = new();
    private readonly Rectangle AllTab;
    private readonly Rectangle IntermediateTab;
    private readonly Rectangle FinishedTab;
    private readonly Rectangle PrevButton;
    private readonly Rectangle NextButton;
    private readonly Rectangle BatchButton;
    private readonly Rectangle MaxButton;
    private readonly Rectangle BackButton;
    private string KindFilter = "All";
    private int Page;
    private string SelectedKey = "";
    private string Message = "제품을 선택하면 오른쪽에서 생산 조건을 확인할 수 있습니다.";

    private static readonly FieldInfo? LegacyReturnMenu = typeof(ProductBookMenu).GetField("ReturnMenu", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? LegacyLineFilter = typeof(ProductBookMenu).GetField("LineFilter", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static ProductBook112Menu FromLegacy(ModEntry mod, ProductBookMenu legacy)
    {
        IClickableMenu returnMenu = LegacyReturnMenu?.GetValue(legacy) as IClickableMenu ?? new CompanyWorkshopMenu(mod);
        string lineFilter = LegacyLineFilter?.GetValue(legacy) as string ?? "";
        return new ProductBook112Menu(mod, returnMenu, lineFilter);
    }

    internal ProductBook112Menu(ModEntry mod, IClickableMenu returnMenu, string lineFilter = "")
        : base(WorkshopUi.CenterX(1160), WorkshopUi.CenterY(730), WorkshopUi.FitWidth(1160), WorkshopUi.FitHeight(730), false)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        LineFilter = lineFilter ?? "";

        int top = yPositionOnScreen + 118;
        AllTab = new Rectangle(xPositionOnScreen + 52, top, 148, 52);
        IntermediateTab = new Rectangle(xPositionOnScreen + 212, top, 148, 52);
        FinishedTab = new Rectangle(xPositionOnScreen + 372, top, 148, 52);

        int bottomY = yPositionOnScreen + height - 64;
        PrevButton = new Rectangle(xPositionOnScreen + 52, bottomY, 138, 52);
        NextButton = new Rectangle(xPositionOnScreen + 438, bottomY, 138, 52);
        BatchButton = new Rectangle(xPositionOnScreen + width - 484, bottomY - 70, 190, 58);
        MaxButton = new Rectangle(xPositionOnScreen + width - 278, bottomY - 70, 190, 58);
        BackButton = new Rectangle(xPositionOnScreen + width - 278, bottomY, 190, 52);
        RefreshSelection();
    }

    private List<ProductionRecipeDefinition> GetFiltered()
    {
        IEnumerable<ProductionRecipeDefinition> query = Mod.Production.GetCatalogRecipes(true);
        if (!string.IsNullOrWhiteSpace(LineFilter))
            query = query.Where(p => string.Equals(p.LineType, LineFilter, StringComparison.OrdinalIgnoreCase));
        if (KindFilter == "Intermediate")
            query = query.Where(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        else if (KindFilter == "Finished")
            query = query.Where(p => !string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        return query.ToList();
    }

    private void RefreshSelection()
    {
        List<ProductionRecipeDefinition> list = GetFiltered();
        int maxPage = Math.Max(0, (list.Count - 1) / 4);
        Page = Math.Clamp(Page, 0, maxPage);
        if (list.Count == 0)
        {
            SelectedKey = "";
            return;
        }
        int first = Math.Min(Page * 4, list.Count - 1);
        if (string.IsNullOrWhiteSpace(SelectedKey) || list.All(p => !string.Equals(p.Key, SelectedKey, StringComparison.OrdinalIgnoreCase)))
            SelectedKey = list[first].Key;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (AllTab.Contains(x, y)) { KindFilter = "All"; Page = 0; RefreshSelection(); Game1.playSound("smallSelect"); return; }
        if (IntermediateTab.Contains(x, y)) { KindFilter = "Intermediate"; Page = 0; RefreshSelection(); Game1.playSound("smallSelect"); return; }
        if (FinishedTab.Contains(x, y)) { KindFilter = "Finished"; Page = 0; RefreshSelection(); Game1.playSound("smallSelect"); return; }

        foreach ((ProductionRecipeDefinition recipe, Rectangle bounds) in Cards)
        {
            if (!bounds.Contains(x, y)) continue;
            SelectedKey = recipe.Key;
            Message = $"{recipe.DisplayName} 선택";
            Game1.playSound("smallSelect");
            return;
        }

        if (PrevButton.Contains(x, y) && Page > 0) { Page--; RefreshSelection(); Game1.playSound("shwip"); return; }
        List<ProductionRecipeDefinition> list = GetFiltered();
        int maxPage = Math.Max(0, (list.Count - 1) / 4);
        if (NextButton.Contains(x, y) && Page < maxPage) { Page++; RefreshSelection(); Game1.playSound("shwip"); return; }
        if (BatchButton.Contains(x, y)) { StartSelected(false); return; }
        if (MaxButton.Contains(x, y)) { StartSelected(true); return; }
        if (BackButton.Contains(x, y)) { Game1.activeClickableMenu = ReturnMenu; Game1.playSound("bigDeSelect"); return; }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<ProductionRecipeDefinition> list = GetFiltered();
        int maxPage = Math.Max(0, (list.Count - 1) / 4);
        if (direction < 0 && Page < maxPage) { Page++; RefreshSelection(); Game1.playSound("shwip"); }
        else if (direction > 0 && Page > 0) { Page--; RefreshSelection(); Game1.playSound("shwip"); }
    }

    private void StartSelected(bool max)
    {
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedKey);
        if (recipe is null) return;
        if (!Mod.Production.IsRecipeUnlocked(recipe, out string reason))
        {
            Message = reason;
            Game1.playSound("cancel");
            return;
        }

        int batches = max ? Mod.Production.GetMaxBatches(recipe) : 1;
        if (batches <= 0)
        {
            Message = "생산 가능한 원재료가 없습니다.";
            Game1.playSound("cancel");
            return;
        }

        bool ok = Mod.Production.TryStart(recipe.Key, batches, out Message);
        Game1.playSound(ok ? "coin" : "cancel");
    }

    public override void draw(SpriteBatch b)
    {
        Mod.Production.EnsureState();
        string subtitle = string.IsNullOrWhiteSpace(LineFilter)
            ? "제품 4개씩 크게 보고, 재료·시간·수율·등급을 확인합니다."
            : $"{WorkshopUi.LineTypeName(LineFilter)}에서 만들 수 있는 제품만 표시합니다.";
        WorkshopUi.BeginBook(b, this, "제품책", subtitle);

        WorkshopUi.Button(b, AllTab, "전체", true, KindFilter == "All");
        WorkshopUi.Button(b, IntermediateTab, "중간재", true, KindFilter == "Intermediate");
        WorkshopUi.Button(b, FinishedTab, "완제품", true, KindFilter == "Finished");

        List<ProductionRecipeDefinition> list = GetFiltered();
        DrawCards(b, list);
        DrawDetailPanel(b);

        int maxPage = Math.Max(0, (list.Count - 1) / 4);
        WorkshopUi.Button(b, PrevButton, "이전", Page > 0);
        WorkshopUi.Button(b, NextButton, "다음", Page < maxPage);
        WorkshopUi.DrawCentered(b, Game1.dialogueFont, $"{list.Count}개 제품  ·  {Page + 1}/{maxPage + 1}",
            new Rectangle(PrevButton.Right + 10, PrevButton.Y, NextButton.X - PrevButton.Right - 20, PrevButton.Height), WorkshopUi.Muted, 0.66f);
        WorkshopUi.Button(b, BatchButton, "+ 1배치");
        WorkshopUi.Button(b, MaxButton, "최대 생산");
        WorkshopUi.Button(b, BackButton, "뒤로");

        Rectangle messageBox = new(xPositionOnScreen + width - 500, BatchButton.Y - 52, 412, 42);
        b.Draw(Game1.fadeToBlackRect, messageBox, new Color(244, 226, 183) * 0.92f);
        WorkshopUi.Border(b, messageBox, new Color(194, 145, 73), 2);
        WorkshopUi.DrawCentered(b, Game1.smallFont, Message, new Rectangle(messageBox.X + 8, messageBox.Y + 4, messageBox.Width - 16, messageBox.Height - 8), WorkshopUi.Muted, 1.08f);
        drawMouse(b);
    }

    private void DrawCards(SpriteBatch b, List<ProductionRecipeDefinition> list)
    {
        Cards.Clear();
        int leftX = xPositionOnScreen + 52;
        int topY = yPositionOnScreen + 184;
        int detailX = xPositionOnScreen + width - 500;
        int leftW = detailX - leftX - 22;
        int contentBottom = yPositionOnScreen + height - 132;
        int availableH = contentBottom - topY;
        int gap = 14;
        int cardW = (leftW - gap) / 2;
        int cardH = (availableH - gap) / 2;
        int start = Page * 4;

        for (int i = 0; i < 4 && start + i < list.Count; i++)
        {
            ProductionRecipeDefinition recipe = list[start + i];
            int col = i % 2;
            int row = i / 2;
            Rectangle card = new(leftX + col * (cardW + gap), topY + row * (cardH + gap), cardW, cardH);
            DrawRecipeCard(b, recipe, card);
            Cards.Add((recipe, card));
        }
    }

    private void DrawRecipeCard(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle r)
    {
        bool selected = string.Equals(recipe.Key, SelectedKey, StringComparison.OrdinalIgnoreCase);
        bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason);
        WorkshopUi.Panel(b, r, selected);

        int iconSize = Math.Min(104, r.Height - 34);
        Rectangle icon = new(r.X + 14, r.Y + (r.Height - iconSize) / 2, iconSize, iconSize);
        Mod.Icons.DrawRecipeIcon(b, recipe, icon, unlocked ? 1f : 0.33f);

        int tx = icon.Right + 14;
        int tw = r.Right - tx - 12;
        Color main = unlocked ? WorkshopUi.Ink : new Color(145, 128, 105);
        WorkshopUi.DrawCentered(b, Game1.dialogueFont, recipe.DisplayName, new Rectangle(tx, r.Y + 12, tw, 38), main, 0.68f);
        WorkshopUi.Text(b, string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품", new Vector2(tx, r.Y + 54), unlocked ? WorkshopUi.Blue : WorkshopUi.Red, 1.08f);
        WorkshopUi.Text(b, WorkshopUi.LineTypeName(recipe.LineType), new Vector2(tx, r.Y + 78), WorkshopUi.Green, 1.02f);
        WorkshopUi.Text(b, $"재료  {Mod.Production.GetIngredientDisplayName(recipe)} ×{recipe.InputQuantity}", new Vector2(tx, r.Y + 102), main, 1.02f);
        if (!unlocked)
            WorkshopUi.DrawCentered(b, Game1.smallFont, reason, new Rectangle(tx, r.Bottom - 38, tw, 28), WorkshopUi.Red, 1.02f);
    }

    private void DrawDetailPanel(SpriteBatch b)
    {
        Rectangle detail = new(xPositionOnScreen + width - 500, yPositionOnScreen + 118, 448, height - 262);
        WorkshopUi.Panel(b, detail, true);
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedKey);
        if (recipe is null)
        {
            WorkshopUi.DrawCentered(b, Game1.dialogueFont, "표시할 제품이 없습니다.", detail, WorkshopUi.Muted, 0.78f);
            return;
        }

        bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason);
        Rectangle icon = new(detail.X + 24, detail.Y + 26, 112, 112);
        Mod.Icons.DrawRecipeIcon(b, recipe, icon, unlocked ? 1f : 0.33f);
        WorkshopUi.DrawCentered(b, Game1.dialogueFont, recipe.DisplayName, new Rectangle(icon.Right + 16, detail.Y + 28, detail.Width - 180, 42), WorkshopUi.Ink, 0.76f);
        WorkshopUi.DrawCentered(b, Game1.smallFont, unlocked ? "생산 가능" : reason, new Rectangle(icon.Right + 16, detail.Y + 76, detail.Width - 180, 32), unlocked ? WorkshopUi.Green : WorkshopUi.Red, 1.14f);
        WorkshopUi.DrawCentered(b, Game1.smallFont, string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품", new Rectangle(icon.Right + 16, detail.Y + 110, detail.Width - 180, 28), WorkshopUi.Blue, 1.05f);

        ProductionForecast forecast = Mod.Quality.GetForecast(recipe, 1);
        string[] labels = { "필요 재료", "현재 재고", "생산 라인", "예상 시간", "예상 생산", "예상 등급" };
        string[] values =
        {
            $"{Mod.Production.GetIngredientDisplayName(recipe)} ×{recipe.InputQuantity}",
            Mod.Production.GetIngredientQuantity(recipe).ToString(),
            WorkshopUi.LineTypeName(recipe.LineType),
            WorkshopUi.TimeText(Mod.Production.GetRecipeTotalMinutes(recipe)),
            $"{forecast.MinOutput}~{forecast.MaxOutput}{recipe.OutputUnit}",
            forecast.MostLikelyGrade
        };

        int y = detail.Y + 156;
        for (int i = 0; i < labels.Length; i++)
        {
            WorkshopUi.Text(b, labels[i], new Vector2(detail.X + 28, y + i * 34), WorkshopUi.Muted, 1.04f);
            WorkshopUi.DrawCentered(b, Game1.smallFont, values[i], new Rectangle(detail.X + 170, y - 3 + i * 34, detail.Width - 196, 30), WorkshopUi.Ink, 1.12f);
        }

        int qualityY = y + labels.Length * 34 + 14;
        WorkshopUi.Text(b, "품질 확률", new Vector2(detail.X + 28, qualityY), WorkshopUi.Ink, 1.12f);
        int boxY = qualityY + 28;
        int qGap = 8;
        int qW = (detail.Width - 56 - qGap * 3) / 4;
        DrawQualityBox(b, new Rectangle(detail.X + 28, boxY, qW, 52), "S", forecast.SChance, new Color(221, 163, 45));
        DrawQualityBox(b, new Rectangle(detail.X + 28 + (qW + qGap), boxY, qW, 52), "A", forecast.AChance, new Color(83, 155, 92));
        DrawQualityBox(b, new Rectangle(detail.X + 28 + (qW + qGap) * 2, boxY, qW, 52), "B", forecast.BChance, new Color(62, 118, 172));
        DrawQualityBox(b, new Rectangle(detail.X + 28 + (qW + qGap) * 3, boxY, qW, 52), "C", forecast.CChance, new Color(183, 103, 55));
    }

    private static void DrawQualityBox(SpriteBatch b, Rectangle r, string grade, int chance, Color fill)
    {
        b.Draw(Game1.fadeToBlackRect, r, fill * 0.9f);
        WorkshopUi.Border(b, r, new Color(93, 66, 35), 2);
        WorkshopUi.DrawCentered(b, Game1.dialogueFont, grade, new Rectangle(r.X, r.Y + 1, r.Width, 26), Color.White, 0.55f);
        WorkshopUi.DrawCentered(b, Game1.smallFont, $"{chance}%", new Rectangle(r.X, r.Y + 27, r.Width, 22), Color.White, 1.02f);
    }
}

/// <summary>0.11.2 production plan book with larger rows and action buttons.</summary>
internal sealed class ProductionPlanBook112Menu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle AddButton;
    private readonly Rectangle BackButton;
    private readonly List<(ProductionPlanEntry Plan, Rectangle Up, Rectangle Down, Rectangle Remove)> Actions = new();
    private string Message = "계획은 위에서부터 빈 생산라인에 자동 배정됩니다.";

    private static readonly FieldInfo? LegacyReturnMenu = typeof(ProductionPlanBookMenu).GetField("ReturnMenu", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static ProductionPlanBook112Menu FromLegacy(ModEntry mod, ProductionPlanBookMenu legacy)
    {
        IClickableMenu returnMenu = LegacyReturnMenu?.GetValue(legacy) as IClickableMenu ?? new ProductionLineSelectMenu(mod, new CompanyWorkshopMenu(mod));
        return new ProductionPlanBook112Menu(mod, returnMenu);
    }

    internal ProductionPlanBook112Menu(ModEntry mod, IClickableMenu returnMenu)
        : base(WorkshopUi.CenterX(1080), WorkshopUi.CenterY(720), WorkshopUi.FitWidth(1080), WorkshopUi.FitHeight(720), false)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        AddButton = new Rectangle(xPositionOnScreen + 54, yPositionOnScreen + height - 66, 260, 54);
        BackButton = new Rectangle(xPositionOnScreen + width - 314, yPositionOnScreen + height - 66, 260, 54);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach ((ProductionPlanEntry plan, Rectangle up, Rectangle down, Rectangle remove) in Actions)
        {
            if (up.Contains(x, y)) { Mod.Production.TryMovePlan(plan.Id, -1, out Message); Game1.playSound("smallSelect"); return; }
            if (down.Contains(x, y)) { Mod.Production.TryMovePlan(plan.Id, 1, out Message); Game1.playSound("smallSelect"); return; }
            if (remove.Contains(x, y)) { Mod.Production.TryRemovePlan(plan.Id, out Message); Game1.playSound("trashcan"); return; }
        }
        if (AddButton.Contains(x, y)) { Game1.activeClickableMenu = new ProductBook112Menu(Mod, this); return; }
        if (BackButton.Contains(x, y)) { Game1.activeClickableMenu = ReturnMenu; return; }
    }

    public override void draw(SpriteBatch b)
    {
        Mod.Production.EnsureState();
        WorkshopUi.BeginBook(b, this, "생산계획표", "제품명과 재료상태를 크게 확인하고 생산 우선순위를 조정합니다.");
        Actions.Clear();
        IReadOnlyList<ProductionPlanEntry> plans = Mod.Production.GetPlans();

        int listTop = yPositionOnScreen + 126;
        int listBottom = yPositionOnScreen + height - 132;
        int maxRows = Math.Min(6, plans.Count);
        int rowGap = 8;
        int rowH = Math.Min(72, (listBottom - listTop - rowGap * 5) / 6);

        for (int i = 0; i < maxRows; i++)
        {
            ProductionPlanEntry plan = plans[i];
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(plan.RecipeKey);
            Rectangle row = new(xPositionOnScreen + 54, listTop + i * (rowH + rowGap), width - 108, rowH);
            WorkshopUi.Panel(b, row, i == 0);
            WorkshopUi.Badge(b, new Rectangle(row.X + 10, row.Y + 10, 40, row.Height - 20), (i + 1).ToString(), i == 0 ? WorkshopUi.Green : new Color(114, 94, 67));

            string name = recipe?.DisplayName ?? plan.RecipeKey;
            int have = recipe is null ? 0 : Mod.Production.GetIngredientQuantity(recipe);
            int need = recipe is null ? 0 : recipe.InputQuantity * plan.BatchCount;
            bool ready = recipe is not null && have >= need;
            string line = recipe is null ? "-" : WorkshopUi.LineTypeName(recipe.LineType);
            string status = recipe is null ? "레시피 없음" : ready ? $"재료 준비 {have}/{need}" : $"재료 부족 {have}/{need}";

            WorkshopUi.Heading(b, $"{name}  ×{plan.BatchCount}배치", new Vector2(row.X + 66, row.Y + 9), WorkshopUi.Ink, 0.66f);
            WorkshopUi.Text(b, $"{line}  ·  {status}", new Vector2(row.X + 68, row.Y + row.Height - 29), ready ? WorkshopUi.Green : WorkshopUi.Red, 1.05f);

            int actionSize = Math.Min(48, row.Height - 16);
            Rectangle up = new(row.Right - actionSize * 3 - 28, row.Y + (row.Height - actionSize) / 2, actionSize, actionSize);
            Rectangle down = new(up.Right + 8, up.Y, actionSize, actionSize);
            Rectangle remove = new(down.Right + 8, up.Y, actionSize, actionSize);
            WorkshopUi.Button(b, up, "▲", i > 0);
            WorkshopUi.Button(b, down, "▼", i < plans.Count - 1);
            WorkshopUi.Button(b, remove, "X");
            Actions.Add((plan, up, down, remove));
        }

        if (plans.Count == 0)
        {
            Rectangle empty = new(xPositionOnScreen + 54, listTop + 22, width - 108, 230);
            WorkshopUi.Panel(b, empty);
            WorkshopUi.DrawCentered(b, Game1.dialogueFont, "등록된 생산계획이 없습니다.", empty, WorkshopUi.Muted, 0.82f);
        }

        Rectangle message = new(xPositionOnScreen + 328, yPositionOnScreen + height - 64, width - 656, 50);
        WorkshopUi.DrawCentered(b, Game1.smallFont, Message, message, WorkshopUi.Muted, 1.12f);
        WorkshopUi.Button(b, AddButton, "+ 제품 추가");
        WorkshopUi.Button(b, BackButton, "뒤로");
        drawMouse(b);
    }
}
