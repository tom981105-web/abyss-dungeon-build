using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal static class WorkshopUi
{
    internal static readonly Color Ink = new(74, 51, 32);
    internal static readonly Color Muted = new(116, 96, 71);
    internal static readonly Color Green = new(48, 104, 63);
    internal static readonly Color GreenDark = new(31, 72, 44);
    internal static readonly Color Gold = new(202, 143, 48);
    internal static readonly Color Red = new(165, 69, 52);
    internal static readonly Color Blue = new(63, 104, 143);
    internal static readonly Color Paper = new(248, 231, 190);

    internal static int FitWidth(int desired) => Math.Max(720, Math.Min(desired, Game1.uiViewport.Width - 80));
    internal static int FitHeight(int desired) => Math.Max(560, Math.Min(desired, Game1.uiViewport.Height - 70));
    internal static int CenterX(int desired) => Math.Max(0, (Game1.uiViewport.Width - FitWidth(desired)) / 2);
    internal static int CenterY(int desired) => Math.Max(0, (Game1.uiViewport.Height - FitHeight(desired)) / 2);

    internal static void BeginBook(SpriteBatch b, IClickableMenu menu, string title, string subtitle = "")
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.52f);
        IClickableMenu.drawTextureBox(b, menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height, Color.White);
        Rectangle titleBar = new(menu.xPositionOnScreen + 26, menu.yPositionOnScreen + 22, menu.width - 52, 86);
        IClickableMenu.drawTextureBox(b, titleBar.X, titleBar.Y, titleBar.Width, titleBar.Height, Color.White);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(titleBar.X + 8, titleBar.Y + 8, titleBar.Width - 16, titleBar.Height - 16), GreenDark);
        DrawCentered(b, Game1.dialogueFont, title, new Rectangle(titleBar.X + 10, titleBar.Y + 6, titleBar.Width - 20, 48), Color.White, 1.0f);
        if (!string.IsNullOrWhiteSpace(subtitle))
            DrawCentered(b, Game1.smallFont, subtitle, new Rectangle(titleBar.X + 12, titleBar.Y + 51, titleBar.Width - 24, 26), new Color(232, 241, 225), 1.25f);
    }

    internal static void Panel(SpriteBatch b, Rectangle r, bool selected = false)
    {
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
        Color tint = selected ? new Color(255, 244, 208) : Paper;
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14), tint * 0.42f);
        if (selected)
            Border(b, r, Gold, 3);
    }

    internal static void Border(SpriteBatch b, Rectangle r, Color c, int thickness = 2)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
    }

    internal static void Button(SpriteBatch b, Rectangle r, string text, bool enabled = true, bool selected = false)
    {
        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
        Color fill = !enabled ? new Color(150, 145, 126) : selected ? Green : new Color(172, 120, 53);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(r.X + 7, r.Y + 7, r.Width - 14, r.Height - 14), fill * 0.82f);
        DrawCentered(b, Game1.dialogueFont, text, new Rectangle(r.X + 8, r.Y + 7, r.Width - 16, r.Height - 14), enabled ? Color.White : new Color(225, 222, 207), 0.76f);
    }

    internal static void Progress(SpriteBatch b, Rectangle r, float value, Color? fill = null)
    {
        value = Math.Clamp(value, 0f, 1f);
        b.Draw(Game1.fadeToBlackRect, r, new Color(94, 77, 55));
        Rectangle inner = new(r.X + 3, r.Y + 3, Math.Max(0, r.Width - 6), Math.Max(0, r.Height - 6));
        b.Draw(Game1.fadeToBlackRect, inner, new Color(224, 208, 164));
        Rectangle bar = new(inner.X, inner.Y, (int)Math.Round(inner.Width * value), inner.Height);
        b.Draw(Game1.fadeToBlackRect, bar, fill ?? Green);
    }

    internal static void Badge(SpriteBatch b, Rectangle r, string text, Color fill)
    {
        b.Draw(Game1.fadeToBlackRect, r, fill);
        Border(b, r, new Color(87, 63, 34), 2);
        DrawCentered(b, Game1.smallFont, text, r, Color.White, 1.1f);
    }

    internal static void Text(SpriteBatch b, string text, Vector2 pos, Color color, float scale = 1.2f)
        => b.DrawString(Game1.smallFont, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);

    internal static void Heading(SpriteBatch b, string text, Vector2 pos, Color color, float scale = 0.9f)
        => b.DrawString(Game1.dialogueFont, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);

    internal static void DrawCentered(SpriteBatch b, SpriteFont font, string text, Rectangle r, Color color, float maxScale = 1f)
    {
        Vector2 size = font.MeasureString(text);
        float scale = Math.Min(maxScale, Math.Min((r.Width - 8) / Math.Max(1f, size.X), (r.Height - 4) / Math.Max(1f, size.Y)));
        Vector2 pos = new(r.Center.X - size.X * scale / 2f, r.Center.Y - size.Y * scale / 2f);
        b.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
    }

    internal static string TimeText(int minutes)
    {
        minutes = Math.Max(0, minutes);
        int h = minutes / 60;
        int m = minutes % 60;
        return h > 0 ? $"{h}시간 {m}분" : $"{m}분";
    }

    internal static string LineShortName(ProductionLineState line)
    {
        if (line.LineType.Equals("Beverage", StringComparison.OrdinalIgnoreCase)) return "음료 라인";
        if (line.LineType.Equals("Fermentation", StringComparison.OrdinalIgnoreCase)) return "발효 라인";
        if (line.LineType.Equals("Packaging", StringComparison.OrdinalIgnoreCase)) return "포장 라인";
        return line.DisplayName;
    }

    internal static string LineTypeName(string lineType)
    {
        if (lineType.Equals("Beverage", StringComparison.OrdinalIgnoreCase)) return "음료 라인";
        if (lineType.Equals("Fermentation", StringComparison.OrdinalIgnoreCase)) return "발효 라인";
        if (lineType.Equals("Packaging", StringComparison.OrdinalIgnoreCase)) return "포장 라인";
        return lineType;
    }
}

internal sealed class CompanyWorkshopMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly List<(string Name, string Desc, Rectangle Bounds, int Tab)> Entries = new();
    private readonly Rectangle ProductionBounds;
    private readonly Rectangle CloseBounds;
    private static readonly FieldInfo? SelectedTabField = typeof(CompanyMenu).GetField("SelectedTab", BindingFlags.Instance | BindingFlags.NonPublic);

    internal CompanyWorkshopMenu(ModEntry mod)
        : base(WorkshopUi.CenterX(1080), WorkshopUi.CenterY(700), WorkshopUi.FitWidth(1080), WorkshopUi.FitHeight(700), false)
    {
        Mod = mod;
        Mod.Company.EnsureState();
        Mod.Production.EnsureState();
        Mod.Contracts.EnsureState();

        int contentW = width - 120;
        int halfW = (contentW - 24) / 2;
        CloseBounds = new Rectangle(xPositionOnScreen + width - 64, yPositionOnScreen + 38, 40, 40);
        ProductionBounds = new Rectangle(xPositionOnScreen + 60, yPositionOnScreen + 150, halfW, 148);
        Entries.Add(("창고", "원물 입출고와 공용 재고 관리", new Rectangle(ProductionBounds.Right + 24, ProductionBounds.Y, halfW, 148), 2));

        int rowY = yPositionOnScreen + 320;
        int thirdW = (contentW - 32) / 3;
        Entries.Add(("계약", "진행 계약과 오늘의 납품 확인", new Rectangle(xPositionOnScreen + 60, rowY, thirdW, 126), 3));
        Entries.Add(("거래처", "거래처 신뢰와 파트너 단계", new Rectangle(xPositionOnScreen + 60 + thirdW + 16, rowY, thirdW, 126), 4));
        Entries.Add(("브랜드", "브랜드 성장과 제품 인지도", new Rectangle(xPositionOnScreen + 60 + (thirdW + 16) * 2, rowY, thirdW, 126), 6));
        Entries.Add(("재무", "회사 자금과 누적 매출", new Rectangle(xPositionOnScreen + 60, yPositionOnScreen + 470, contentW, 110), 8));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (CloseBounds.Contains(x, y)) { exitThisMenu(); return; }
        if (ProductionBounds.Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new ProductionLineSelectMenu(Mod, this);
            return;
        }
        foreach (var entry in Entries)
        {
            if (!entry.Bounds.Contains(x, y)) continue;
            OpenLegacyTab(entry.Tab);
            return;
        }
    }

    private void OpenLegacyTab(int tab)
    {
        CompanyMenu menu = new(Mod);
        SelectedTabField?.SetValue(menu, tab);
        if (tab == 2)
            Mod.Multiplayer.RequestWarehouseControl();
        Game1.playSound("smallSelect");
        Game1.activeClickableMenu = menu;
    }

    public override void draw(SpriteBatch b)
    {
        WorkshopUi.BeginBook(b, this, Mod.State.CompanyName, $"Lv.{Mod.State.Level} {CompanyCore.GetStageName(Mod.State.Level)} · 평판 {Mod.State.Reputation:N0}");
        WorkshopUi.Button(b, CloseBounds, "X");

        WorkshopUi.Panel(b, ProductionBounds, true);
        WorkshopUi.Heading(b, "생산", new Vector2(ProductionBounds.X + 24, ProductionBounds.Y + 18), WorkshopUi.Ink);
        WorkshopUi.Text(b, $"생산라인 3개 · 가동 {Mod.State.ProductionQueue.Count}개 · 대기계획 {Mod.State.ProductionPlans.Count}개", new Vector2(ProductionBounds.X + 26, ProductionBounds.Y + 78), WorkshopUi.Green, 1.22f);
        WorkshopUi.Text(b, "라인 상태 · 생산계획 · 제품책", new Vector2(ProductionBounds.X + 26, ProductionBounds.Y + 108), WorkshopUi.Muted, 1.18f);

        foreach (var entry in Entries)
        {
            WorkshopUi.Panel(b, entry.Bounds);
            WorkshopUi.Heading(b, entry.Name, new Vector2(entry.Bounds.X + 20, entry.Bounds.Y + 16), WorkshopUi.Ink, 0.82f);
            WorkshopUi.Text(b, entry.Desc, new Vector2(entry.Bounds.X + 22, entry.Bounds.Y + 72), WorkshopUi.Muted, 1.15f);
        }

        Rectangle finance = Entries.Last().Bounds;
        WorkshopUi.Text(b, $"회사 자금 {Mod.State.CompanyFunds:N0}G · 누적 매출 {Mod.State.LifetimeRevenue:N0}G", new Vector2(finance.X + 22, finance.Y + 72), WorkshopUi.Green, 1.22f);
        string role = Context.IsMultiplayer ? "공동 경영 · 모든 플레이어 동등 권한" : "싱글플레이";
        WorkshopUi.Text(b, role, new Vector2(xPositionOnScreen + 66, yPositionOnScreen + height - 72), WorkshopUi.Muted, 1.12f);
        drawMouse(b);
    }
}

internal sealed class ProductionLineSelectMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly List<(ProductionLineState Line, Rectangle Bounds)> LineCards = new();
    private readonly Rectangle PlanButton;
    private readonly Rectangle BookButton;
    private readonly Rectangle BackButton;

    internal ProductionLineSelectMenu(ModEntry mod, IClickableMenu returnMenu)
        : base(WorkshopUi.CenterX(1080), WorkshopUi.CenterY(700), WorkshopUi.FitWidth(1080), WorkshopUi.FitHeight(700), false)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        Mod.Production.EnsureState();

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        int cardX = xPositionOnScreen + 62;
        int cardW = width - 124;
        for (int i = 0; i < Math.Min(3, lines.Count); i++)
            LineCards.Add((lines[i], new Rectangle(cardX, yPositionOnScreen + 142 + i * 145, cardW, 128)));

        int buttonY = yPositionOnScreen + height - 80;
        int gap = 16;
        int buttonW = (width - 120 - gap * 2) / 3;
        PlanButton = new Rectangle(xPositionOnScreen + 60, buttonY, buttonW, 58);
        BookButton = new Rectangle(PlanButton.Right + gap, buttonY, buttonW, 58);
        BackButton = new Rectangle(BookButton.Right + gap, buttonY, buttonW, 58);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (var card in LineCards)
        {
            if (!card.Bounds.Contains(x, y)) continue;
            Game1.playSound("smallSelect");
            Game1.activeClickableMenu = new ProductionLineDetailMenu(Mod, card.Line, this);
            return;
        }
        if (PlanButton.Contains(x, y)) { Game1.activeClickableMenu = new ProductionPlanBookMenu(Mod, this); return; }
        if (BookButton.Contains(x, y)) { Game1.activeClickableMenu = new ProductBookMenu(Mod, this); return; }
        if (BackButton.Contains(x, y)) { Game1.activeClickableMenu = ReturnMenu; return; }
    }

    public override void draw(SpriteBatch b)
    {
        WorkshopUi.BeginBook(b, this, "생산 작업장", "라인을 선택하면 해당 라인만 크게 관리합니다.");
        foreach (var card in LineCards)
            DrawLineCard(b, card.Line, card.Bounds);
        WorkshopUi.Button(b, PlanButton, $"생산계획표 ({Mod.State.ProductionPlans.Count})");
        WorkshopUi.Button(b, BookButton, "제품책");
        WorkshopUi.Button(b, BackButton, "회사 관리로 돌아가기");
        drawMouse(b);
    }

    private void DrawLineCard(SpriteBatch b, ProductionLineState line, Rectangle r)
    {
        ProductionJob? job = Mod.Production.GetLineJob(line.Id);
        WorkshopUi.Panel(b, r, job is not null);
        string lineName = WorkshopUi.LineShortName(line);
        WorkshopUi.Heading(b, lineName, new Vector2(r.X + 22, r.Y + 14), WorkshopUi.Ink, 0.78f);

        int efficiency = Mod.Production.GetLineEfficiency(line);
        WorkshopUi.Badge(b, new Rectangle(r.Right - 110, r.Y + 16, 86, 34), job is null ? "대기" : "가동", job is null ? new Color(115, 101, 73) : WorkshopUi.Green);

        if (job is null)
        {
            WorkshopUi.Text(b, "현재 작업 없음", new Vector2(r.X + 24, r.Y + 72), WorkshopUi.Muted, 1.18f);
            WorkshopUi.Progress(b, new Rectangle(r.X + 250, r.Y + 76, r.Width - 450, 22), 0f);
            WorkshopUi.Text(b, $"효율 {efficiency}%", new Vector2(r.Right - 170, r.Y + 70), WorkshopUi.Green, 1.18f);
            return;
        }

        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(job.RecipeKey);
        float progress = Mod.Production.GetJobProgress(job);
        string product = recipe?.DisplayName ?? job.RecipeKey;
        WorkshopUi.Text(b, $"{product} · {Mod.Production.GetCurrentStageName(job)}", new Vector2(r.X + 24, r.Y + 65), WorkshopUi.Ink, 1.16f);
        WorkshopUi.Progress(b, new Rectangle(r.X + 250, r.Y + 76, r.Width - 450, 22), progress);
        WorkshopUi.Text(b, $"{progress * 100:0}%", new Vector2(r.Right - 205, r.Y + 69), WorkshopUi.Ink, 1.16f);
        WorkshopUi.Text(b, $"{WorkshopUi.TimeText(job.RemainingMinutes)} 남음 · 효율 {efficiency}%", new Vector2(r.X + 24, r.Y + 98), WorkshopUi.Green, 1.12f);
    }
}

internal sealed class ProductionLineDetailMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly ProductionLineState Line;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle ProductButton;
    private readonly Rectangle PlanButton;
    private readonly Rectangle BackButton;

    internal ProductionLineDetailMenu(ModEntry mod, ProductionLineState line, IClickableMenu returnMenu)
        : base(WorkshopUi.CenterX(1040), WorkshopUi.CenterY(700), WorkshopUi.FitWidth(1040), WorkshopUi.FitHeight(700), false)
    {
        Mod = mod;
        Line = line;
        ReturnMenu = returnMenu;

        int buttonY = yPositionOnScreen + height - 82;
        int gap = 16;
        int buttonW = (width - 120 - gap * 2) / 3;
        ProductButton = new Rectangle(xPositionOnScreen + 60, buttonY, buttonW, 58);
        PlanButton = new Rectangle(ProductButton.Right + gap, buttonY, buttonW, 58);
        BackButton = new Rectangle(PlanButton.Right + gap, buttonY, buttonW, 58);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (ProductButton.Contains(x, y)) { Game1.activeClickableMenu = new ProductBookMenu(Mod, this, Line.LineType); return; }
        if (PlanButton.Contains(x, y)) { Game1.activeClickableMenu = new ProductionPlanBookMenu(Mod, this); return; }
        if (BackButton.Contains(x, y)) { Game1.activeClickableMenu = ReturnMenu; return; }
    }

    public override void draw(SpriteBatch b)
    {
        Mod.Production.EnsureState();
        ProductionJob? job = Mod.Production.GetLineJob(Line.Id);
        WorkshopUi.BeginBook(b, this, WorkshopUi.LineShortName(Line), $"라인 Lv.{Line.Level} · 효율 {Mod.Production.GetLineEfficiency(Line)}%");

        Rectangle main = new(xPositionOnScreen + 60, yPositionOnScreen + 132, width - 120, height - 238);
        WorkshopUi.Panel(b, main, job is not null);
        if (job is null) DrawIdle(b, main); else DrawActive(b, main, job);

        WorkshopUi.Button(b, ProductButton, "이 라인 제품 선택");
        WorkshopUi.Button(b, PlanButton, "생산계획표");
        WorkshopUi.Button(b, BackButton, "라인 목록으로");
        drawMouse(b);
    }

    private void DrawIdle(SpriteBatch b, Rectangle r)
    {
        WorkshopUi.Badge(b, new Rectangle(r.X + 26, r.Y + 24, 104, 36), "대기 중", new Color(112, 96, 72));
        WorkshopUi.Heading(b, "현재 진행 중인 작업이 없습니다.", new Vector2(r.X + 28, r.Y + 92), WorkshopUi.Ink, 0.78f);
        WorkshopUi.Text(b, "제품을 선택하거나 생산계획표에 작업을 추가하면 빈 라인에 자동 배정됩니다.", new Vector2(r.X + 30, r.Y + 160), WorkshopUi.Muted, 1.18f);
        Rectangle empty = new(r.X + 30, r.Y + 240, r.Width - 60, 112);
        WorkshopUi.Panel(b, empty);
        WorkshopUi.DrawCentered(b, Game1.dialogueFont, "작업 준비 완료", empty, WorkshopUi.Green, 0.8f);
    }

    private void DrawActive(SpriteBatch b, Rectangle r, ProductionJob job)
    {
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(job.RecipeKey);
        if (recipe is null) return;

        float progress = Mod.Production.GetJobProgress(job);
        double time = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 1000.0;
        float pulse = 0.55f + (float)(Math.Sin(time * 4.0) + 1.0) * 0.2f;

        WorkshopUi.Badge(b, new Rectangle(r.X + 26, r.Y + 22, 104, 36), "가동 중", WorkshopUi.Green);
        WorkshopUi.Heading(b, recipe.DisplayName, new Vector2(r.X + 150, r.Y + 18), WorkshopUi.Ink, 0.82f);
        WorkshopUi.Text(b, $"현재 공정 · {Mod.Production.GetCurrentStageName(job)}", new Vector2(r.X + 30, r.Y + 86), WorkshopUi.Green, 1.22f);
        WorkshopUi.Progress(b, new Rectangle(r.X + 30, r.Y + 126, r.Width - 60, 26), progress, WorkshopUi.Green);
        WorkshopUi.Text(b, $"전체 진행률 {progress * 100:0}%   ·   남은 시간 {WorkshopUi.TimeText(job.RemainingMinutes)}   ·   예상 {job.EstimatedOutputQuantity}{recipe.OutputUnit}   ·   {job.OutputGrade}급", new Vector2(r.X + 30, r.Y + 170), WorkshopUi.Ink, 1.12f);

        int count = Math.Min(5, recipe.Stages.Count);
        int gap = 12;
        int sw = (r.Width - 60 - gap * Math.Max(0, count - 1)) / Math.Max(1, count);
        int sy = r.Y + 230;
        for (int i = 0; i < count; i++)
        {
            Rectangle stage = new(r.X + 30 + i * (sw + gap), sy, sw, 104);
            WorkshopUi.Panel(b, stage, i == job.CurrentStageIndex);
            Color c = i < job.CurrentStageIndex ? WorkshopUi.Green : i == job.CurrentStageIndex ? WorkshopUi.Gold * pulse : WorkshopUi.Muted;
            WorkshopUi.Badge(b, new Rectangle(stage.X + 10, stage.Y + 10, 38, 30), (i + 1).ToString(), c);
            WorkshopUi.DrawCentered(b, Game1.smallFont, recipe.Stages[i].DisplayName, new Rectangle(stage.X + 8, stage.Y + 50, stage.Width - 16, 42), WorkshopUi.Ink, 1.18f);
        }

        Rectangle activity = new(r.X + 30, r.Bottom - 52, r.Width - 60, 26);
        b.Draw(Game1.fadeToBlackRect, activity, new Color(74, 61, 42));
        int dotWidth = 14;
        int travel = Math.Max(1, activity.Width - dotWidth - 8);
        int dotX = activity.X + 4 + (int)((time * 70) % travel);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(dotX, activity.Y + 6, dotWidth, 14), WorkshopUi.Gold);
    }
}

internal sealed class ProductionPlanBookMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly IClickableMenu ReturnMenu;
    private readonly Rectangle AddButton;
    private readonly Rectangle BackButton;
    private readonly List<(ProductionPlanEntry Plan, Rectangle Up, Rectangle Down, Rectangle Remove)> Actions = new();
    private string Message = "계획은 위에서부터 빈 생산라인에 자동 배정됩니다.";

    internal ProductionPlanBookMenu(ModEntry mod, IClickableMenu returnMenu)
        : base(WorkshopUi.CenterX(1040), WorkshopUi.CenterY(720), WorkshopUi.FitWidth(1040), WorkshopUi.FitHeight(720), false)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        int buttonY = yPositionOnScreen + height - 82;
        AddButton = new Rectangle(xPositionOnScreen + 60, buttonY, 330, 58);
        BackButton = new Rectangle(xPositionOnScreen + width - 390, buttonY, 330, 58);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (var action in Actions)
        {
            if (action.Up.Contains(x, y)) { Mod.Production.TryMovePlan(action.Plan.Id, -1, out Message); Game1.playSound("smallSelect"); return; }
            if (action.Down.Contains(x, y)) { Mod.Production.TryMovePlan(action.Plan.Id, 1, out Message); Game1.playSound("smallSelect"); return; }
            if (action.Remove.Contains(x, y)) { Mod.Production.TryRemovePlan(action.Plan.Id, out Message); Game1.playSound("trashcan"); return; }
        }
        if (AddButton.Contains(x, y)) { Game1.activeClickableMenu = new ProductBookMenu(Mod, this); return; }
        if (BackButton.Contains(x, y)) { Game1.activeClickableMenu = ReturnMenu; return; }
    }

    public override void draw(SpriteBatch b)
    {
        Mod.Production.EnsureState();
        WorkshopUi.BeginBook(b, this, "생산계획표", "작업 순서를 정리하고 필요한 제품을 추가합니다.");
        Actions.Clear();

        IReadOnlyList<ProductionPlanEntry> plans = Mod.Production.GetPlans();
        int y = yPositionOnScreen + 132;
        int rowH = 58;
        int max = Math.Min(8, plans.Count);
        for (int i = 0; i < max; i++)
        {
            ProductionPlanEntry plan = plans[i];
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(plan.RecipeKey);
            Rectangle row = new(xPositionOnScreen + 60, y + i * (rowH + 4), width - 120, rowH);
            WorkshopUi.Panel(b, row, i == 0);
            WorkshopUi.Badge(b, new Rectangle(row.X + 10, row.Y + 10, 38, 36), (i + 1).ToString(), i == 0 ? WorkshopUi.Green : new Color(114, 94, 67));

            string name = recipe?.DisplayName ?? plan.RecipeKey;
            string line = recipe is null ? "-" : WorkshopUi.LineTypeName(recipe.LineType);
            int have = recipe is null ? 0 : Mod.Production.GetIngredientQuantity(recipe);
            int need = recipe is null ? 0 : recipe.InputQuantity * plan.BatchCount;
            string status = recipe is null ? "레시피 없음" : have >= need ? "재료 준비" : $"재료 부족 {have}/{need}";

            WorkshopUi.Text(b, $"{name} ×{plan.BatchCount}배치", new Vector2(row.X + 60, row.Y + 14), WorkshopUi.Ink, 1.14f);
            WorkshopUi.Text(b, $"{line} · {status}", new Vector2(row.X + 400, row.Y + 14), have >= need ? WorkshopUi.Green : WorkshopUi.Red, 1.08f);

            Rectangle up = new(row.Right - 138, row.Y + 8, 38, 40);
            Rectangle down = new(row.Right - 94, row.Y + 8, 38, 40);
            Rectangle remove = new(row.Right - 50, row.Y + 8, 38, 40);
            WorkshopUi.Button(b, up, "▲", i > 0);
            WorkshopUi.Button(b, down, "▼", i < plans.Count - 1);
            WorkshopUi.Button(b, remove, "X");
            Actions.Add((plan, up, down, remove));
        }

        if (plans.Count == 0)
        {
            Rectangle empty = new(xPositionOnScreen + 60, yPositionOnScreen + 170, width - 120, 270);
            WorkshopUi.Panel(b, empty);
            WorkshopUi.DrawCentered(b, Game1.dialogueFont, "등록된 생산계획이 없습니다.", empty, WorkshopUi.Muted, 0.85f);
        }

        WorkshopUi.Text(b, Message, new Vector2(xPositionOnScreen + 64, yPositionOnScreen + height - 122), WorkshopUi.Muted, 1.08f);
        WorkshopUi.Button(b, AddButton, "+ 제품 추가");
        WorkshopUi.Button(b, BackButton, "뒤로");
        drawMouse(b);
    }
}

internal sealed class ProductBookMenu : IClickableMenu
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

    internal ProductBookMenu(ModEntry mod, IClickableMenu returnMenu, string lineFilter = "")
        : base(WorkshopUi.CenterX(1180), WorkshopUi.CenterY(760), WorkshopUi.FitWidth(1180), WorkshopUi.FitHeight(760), false)
    {
        Mod = mod;
        ReturnMenu = returnMenu;
        LineFilter = lineFilter ?? "";

        AllTab = new Rectangle(xPositionOnScreen + 54, yPositionOnScreen + 118, 150, 50);
        IntermediateTab = new Rectangle(xPositionOnScreen + 214, yPositionOnScreen + 118, 150, 50);
        FinishedTab = new Rectangle(xPositionOnScreen + 374, yPositionOnScreen + 118, 150, 50);

        int bottomY = yPositionOnScreen + height - 72;
        PrevButton = new Rectangle(xPositionOnScreen + 54, bottomY, 130, 48);
        NextButton = new Rectangle(xPositionOnScreen + 454, bottomY, 130, 48);

        BatchButton = new Rectangle(xPositionOnScreen + width - 500, yPositionOnScreen + height - 145, 190, 58);
        MaxButton = new Rectangle(xPositionOnScreen + width - 296, yPositionOnScreen + height - 145, 190, 58);
        BackButton = new Rectangle(xPositionOnScreen + width - 296, bottomY, 190, 48);

        RefreshSelection();
    }

    private List<ProductionRecipeDefinition> GetFiltered()
    {
        IEnumerable<ProductionRecipeDefinition> q = Mod.Production.GetCatalogRecipes(true);
        if (!string.IsNullOrWhiteSpace(LineFilter)) q = q.Where(p => string.Equals(p.LineType, LineFilter, StringComparison.OrdinalIgnoreCase));
        if (KindFilter == "Intermediate") q = q.Where(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        if (KindFilter == "Finished") q = q.Where(p => !string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    private void RefreshSelection()
    {
        List<ProductionRecipeDefinition> list = GetFiltered();
        int maxPage = Math.Max(0, (list.Count - 1) / 6);
        Page = Math.Clamp(Page, 0, maxPage);
        if (list.Count == 0) { SelectedKey = ""; return; }
        if (string.IsNullOrWhiteSpace(SelectedKey) || list.All(p => p.Key != SelectedKey)) SelectedKey = list[Math.Min(Page * 6, list.Count - 1)].Key;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (AllTab.Contains(x, y)) { KindFilter = "All"; Page = 0; RefreshSelection(); return; }
        if (IntermediateTab.Contains(x, y)) { KindFilter = "Intermediate"; Page = 0; RefreshSelection(); return; }
        if (FinishedTab.Contains(x, y)) { KindFilter = "Finished"; Page = 0; RefreshSelection(); return; }
        foreach (var card in Cards)
        {
            if (!card.Bounds.Contains(x, y)) continue;
            SelectedKey = card.Recipe.Key;
            Game1.playSound("smallSelect");
            return;
        }
        if (PrevButton.Contains(x, y) && Page > 0) { Page--; RefreshSelection(); return; }
        List<ProductionRecipeDefinition> list = GetFiltered();
        int maxPage = Math.Max(0, (list.Count - 1) / 6);
        if (NextButton.Contains(x, y) && Page < maxPage) { Page++; RefreshSelection(); return; }
        if (BatchButton.Contains(x, y)) { StartSelected(false); return; }
        if (MaxButton.Contains(x, y)) { StartSelected(true); return; }
        if (BackButton.Contains(x, y)) { Game1.activeClickableMenu = ReturnMenu; return; }
    }

    private void StartSelected(bool max)
    {
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedKey);
        if (recipe is null) return;
        int batches = max ? Mod.Production.GetMaxBatches(recipe) : 1;
        if (batches <= 0) { Message = "생산 가능한 원재료가 없습니다."; Game1.playSound("cancel"); return; }
        bool ok = Mod.Production.TryStart(recipe.Key, batches, out Message);
        Game1.playSound(ok ? "coin" : "cancel");
    }

    public override void draw(SpriteBatch b)
    {
        Mod.Production.EnsureState();
        string subtitle = string.IsNullOrWhiteSpace(LineFilter) ? "제품을 고르고 생산계획에 추가합니다." : $"{WorkshopUi.LineTypeName(LineFilter)}에서 만들 수 있는 제품";
        WorkshopUi.BeginBook(b, this, "제품책", subtitle);
        WorkshopUi.Button(b, AllTab, "전체", true, KindFilter == "All");
        WorkshopUi.Button(b, IntermediateTab, "중간재", true, KindFilter == "Intermediate");
        WorkshopUi.Button(b, FinishedTab, "완제품", true, KindFilter == "Finished");

        Cards.Clear();
        List<ProductionRecipeDefinition> list = GetFiltered();
        int start = Page * 6;

        int leftX = xPositionOnScreen + 54;
        int cardW = Math.Min(310, (width - 650) / 2);
        int cardH = 132;
        int cardGap = 14;

        for (int i = 0; i < 6 && start + i < list.Count; i++)
        {
            ProductionRecipeDefinition recipe = list[start + i];
            int col = i % 2;
            int row = i / 2;
            Rectangle card = new(leftX + col * (cardW + 14), yPositionOnScreen + 188 + row * (cardH + cardGap), cardW, cardH);
            DrawRecipeCard(b, recipe, card);
            Cards.Add((recipe, card));
        }

        Rectangle detail = new(xPositionOnScreen + width - 500, yPositionOnScreen + 118, 440, height - 290);
        WorkshopUi.Panel(b, detail, true);
        DrawDetail(b, detail);

        int maxPage = Math.Max(0, (list.Count - 1) / 6);
        WorkshopUi.Button(b, PrevButton, "이전", Page > 0);
        WorkshopUi.Button(b, NextButton, "다음", Page < maxPage);
        WorkshopUi.DrawCentered(b, Game1.smallFont, $"{list.Count}개 제품 · {Page + 1}/{maxPage + 1}", new Rectangle(xPositionOnScreen + 198, yPositionOnScreen + height - 72, 240, 48), WorkshopUi.Muted, 1.15f);
        WorkshopUi.Button(b, BatchButton, "1배치 추가");
        WorkshopUi.Button(b, MaxButton, "최대 생산");
        WorkshopUi.Button(b, BackButton, "뒤로");

        WorkshopUi.Text(b, Message, new Vector2(detail.X + 12, detail.Bottom + 8), WorkshopUi.Muted, 1.04f);
        drawMouse(b);
    }

    private void DrawRecipeCard(SpriteBatch b, ProductionRecipeDefinition recipe, Rectangle r)
    {
        bool selected = recipe.Key == SelectedKey;
        bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason);
        WorkshopUi.Panel(b, r, selected);
        Rectangle icon = new(r.X + 14, r.Y + 20, 84, 84);
        Mod.Icons.DrawRecipeIcon(b, recipe, icon, unlocked ? 1f : 0.35f);
        Color color = unlocked ? WorkshopUi.Ink : new Color(145, 128, 105);
        WorkshopUi.Text(b, recipe.DisplayName, new Vector2(r.X + 110, r.Y + 16), color, 1.08f);
        WorkshopUi.Text(b, string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? "중간재" : "완제품", new Vector2(r.X + 110, r.Y + 48), unlocked ? WorkshopUi.Blue : WorkshopUi.Red, 1.08f);
        WorkshopUi.Text(b, $"{Mod.Production.GetIngredientDisplayName(recipe)} ×{recipe.InputQuantity}", new Vector2(r.X + 110, r.Y + 80), color, 1.05f);
        if (!unlocked) WorkshopUi.Text(b, reason, new Vector2(r.X + 110, r.Y + 106), WorkshopUi.Red, 0.96f);
    }

    private void DrawDetail(SpriteBatch b, Rectangle r)
    {
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedKey);
        if (recipe is null)
        {
            WorkshopUi.DrawCentered(b, Game1.dialogueFont, "표시할 제품이 없습니다.", r, WorkshopUi.Muted, 0.8f);
            return;
        }
        bool unlocked = Mod.Production.IsRecipeUnlocked(recipe, out string reason);
        Rectangle icon = new(r.X + 22, r.Y + 22, 110, 110);
        Mod.Icons.DrawRecipeIcon(b, recipe, icon, unlocked ? 1f : 0.35f);
        WorkshopUi.Heading(b, recipe.DisplayName, new Vector2(r.X + 150, r.Y + 24), WorkshopUi.Ink, 0.78f);
        WorkshopUi.Text(b, unlocked ? "생산 가능" : reason, new Vector2(r.X + 152, r.Y + 82), unlocked ? WorkshopUi.Green : WorkshopUi.Red, 1.12f);

        ProductionForecast forecast = Mod.Quality.GetForecast(recipe, 1);
        string[] lines =
        {
            $"필요 재료   {Mod.Production.GetIngredientDisplayName(recipe)} ×{recipe.InputQuantity}",
            $"현재 재고   {Mod.Production.GetIngredientQuantity(recipe)}",
            $"생산 라인   {WorkshopUi.LineTypeName(recipe.LineType)}",
            $"예상 시간   {WorkshopUi.TimeText(Mod.Production.GetRecipeTotalMinutes(recipe))}",
            $"예상 생산   {forecast.MinOutput}~{forecast.MaxOutput}{recipe.OutputUnit}",
            $"예상 등급   {forecast.MostLikelyGrade}",
            $"품질 확률   S {forecast.SChance}% · A {forecast.AChance}% · B {forecast.BChance}% · C {forecast.CChance}%"
        };
        for (int i = 0; i < lines.Length; i++) WorkshopUi.Text(b, lines[i], new Vector2(r.X + 28, r.Y + 158 + i * 38), WorkshopUi.Ink, 1.08f);
    }
}
