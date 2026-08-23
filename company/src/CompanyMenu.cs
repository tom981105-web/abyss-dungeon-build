using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed class CompanyMenu : IClickableMenu
{
    private readonly ModEntry Mod;
    private readonly List<(string Name, Rectangle Bounds)> Tabs = new();
    private int SelectedTab;
    private int WarehousePage;
    private int ContractPage;
    private int ClientPage;
    private string WarehouseMessage = "창고 탭에 들어오면 공동 창고 관리권을 자동으로 요청합니다.";
    private string ProductionMessage = "회사 창고의 원물을 사용해 완제품 생산을 시작할 수 있습니다.";
    private string ContractMessage = "오늘의 계약을 수락하거나 진행 중인 계약에 완제품을 납품할 수 있습니다.";

    private const int WarehouseRowsPerPage = 6;
    private const int ContractRowsPerPage = 5;
    private const int ClientRowsPerPage = 5;

    private static readonly Color Green = new(48, 78, 58);
    private static readonly Color Green2 = new(78, 118, 84);
    private static readonly Color Accent = new(90, 128, 76);
    private static readonly Color Muted = new(105, 99, 82);
    private static readonly Color Soft = new(235, 239, 228);
    private static readonly Color Button = new(68, 103, 73);
    private static readonly Color ButtonAlt = new(112, 99, 70);
    private static readonly Color Disabled = new(160, 160, 150);

    internal bool IsWarehouseTabOpen => SelectedTab == 2;

    internal CompanyMenu(ModEntry mod)
        : base(Game1.viewport.Width / 2 - 540, Game1.viewport.Height / 2 - 330, 1080, 660, true)
    {
        Mod = mod;
        Mod.Company.EnsureState();
        Mod.Production.EnsureState();
        Mod.Clients.EnsureState();
        Mod.Contracts.EnsureState();

        string[] names = { "대시보드", "생산", "창고", "계약", "거래처", "연구개발", "브랜드", "직원", "재무" };
        for (int i = 0; i < names.Length; i++)
            Tabs.Add((names[i], new Rectangle(xPositionOnScreen + 18, yPositionOnScreen + 104 + i * 50, 190, 40)));

        behaviorBeforeCleanup = _ => Mod.Multiplayer.ReleaseWarehouseControl();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            Mod.Multiplayer.ReleaseWarehouseControl();
            exitThisMenu();
            return;
        }

        for (int i = 0; i < Tabs.Count; i++)
        {
            if (!Tabs[i].Bounds.Contains(x, y))
                continue;

            int previous = SelectedTab;
            if (previous == 2 && i != 2)
                Mod.Multiplayer.ReleaseWarehouseControl();

            SelectedTab = i;
            if (i == 2 && previous != 2)
            {
                WarehouseMessage = "공동 창고 관리권을 확인하고 있습니다.";
                Mod.Multiplayer.RequestWarehouseControl();
            }

            Game1.playSound("smallSelect");
            return;
        }

        if (SelectedTab == 1)
            HandleProductionClick(x, y);
        else if (SelectedTab == 2)
            HandleWarehouseClick(x, y);
        else if (SelectedTab == 3)
            HandleContractClick(x, y);
        else if (SelectedTab == 4)
            HandleClientClick(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (SelectedTab == 2)
        {
            int maxPage = GetWarehouseMaxPage();
            if (direction < 0 && WarehousePage < maxPage)
                WarehousePage++;
            else if (direction > 0 && WarehousePage > 0)
                WarehousePage--;
            return;
        }

        if (SelectedTab == 3)
        {
            int maxPage = GetContractMaxPage();
            if (direction < 0 && ContractPage < maxPage)
                ContractPage++;
            else if (direction > 0 && ContractPage > 0)
                ContractPage--;
            return;
        }

        if (SelectedTab == 4)
        {
            int maxPage = GetClientMaxPage();
            if (direction < 0 && ClientPage < maxPage)
                ClientPage++;
            else if (direction > 0 && ClientPage > 0)
                ClientPage--;
        }
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.55f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        Rectangle side = new(xPositionOnScreen + 8, yPositionOnScreen + 8, 215, height - 16);
        b.Draw(Game1.fadeToBlackRect, side, Green);
        DrawSidebar(b);

        if (SelectedTab == 0)
            DrawDashboard(b);
        else if (SelectedTab == 1)
            DrawProduction(b);
        else if (SelectedTab == 2)
            DrawWarehouse(b);
        else if (SelectedTab == 3)
            DrawContracts(b);
        else if (SelectedTab == 4)
            DrawClients(b);
        else
            DrawComingSoon(b, Tabs[SelectedTab].Name);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawSidebar(SpriteBatch b)
    {
        b.DrawString(Game1.dialogueFont, "농업회사", new Vector2(xPositionOnScreen + 25, yPositionOnScreen + 23), Color.White);
        b.DrawString(Game1.smallFont, "COMPANY 0.5", new Vector2(xPositionOnScreen + 27, yPositionOnScreen + 67), new Color(215, 228, 210));

        for (int i = 0; i < Tabs.Count; i++)
        {
            var tab = Tabs[i];
            if (i == SelectedTab)
                b.Draw(Game1.fadeToBlackRect, tab.Bounds, Green2);
            b.DrawString(Game1.smallFont, tab.Name, new Vector2(tab.Bounds.X + 16, tab.Bounds.Y + 8), Color.White);
        }

        string role = Context.IsMultiplayer ? "공동 경영 · 동등 권한" : "싱글플레이";
        b.DrawString(Game1.smallFont, role, new Vector2(xPositionOnScreen + 27, yPositionOnScreen + height - 74), new Color(215, 228, 210));
        b.DrawString(Game1.smallFont, "F7 회사 관리", new Vector2(xPositionOnScreen + 27, yPositionOnScreen + height - 48), new Color(215, 228, 210));
    }

    private void DrawDashboard(SpriteBatch b)
    {
        CompanySaveData c = Mod.State;
        int x = xPositionOnScreen + 250;
        int y = yPositionOnScreen + 28;
        int w = width - 285;

        b.DrawString(Game1.dialogueFont, c.CompanyName, new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, $"Lv.{c.Level} · {CompanyCore.GetStageName(c.Level)} · 평판 {c.Reputation:N0}", new Vector2(x, y + 47), Accent);
        DrawXp(b, x, y + 77, w - 35);

        int cardY = y + 120;
        int gap = 12;
        int cardW = (w - gap * 3) / 4;
        DrawCard(b, x, cardY, cardW, "회사 자금", $"{c.CompanyFunds:N0}G");
        DrawCard(b, x + (cardW + gap), cardY, cardW, "원물 재고", $"{Mod.Company.GetWarehouseUsed():N0}");
        DrawCard(b, x + (cardW + gap) * 2, cardY, cardW, "완제품", $"{Mod.Production.GetFinishedGoodsTotal():N0}");
        DrawCard(b, x + (cardW + gap) * 3, cardY, cardW, "진행 계약", $"{c.AcceptedContracts.Count}/{Mod.Contracts.GetActiveCapacity()}");

        int sectionY = cardY + 125;
        b.DrawString(Game1.dialogueFont, "회사 운영 흐름", new Vector2(x, sectionY), Game1.textColor);
        b.DrawString(Game1.smallFont, "납품을 반복하면 거래처 신뢰가 쌓이고 정기·우선·핵심 파트너 계약으로 성장합니다.", new Vector2(x, sectionY + 39), Muted);

        int rowY = sectionY + 78;
        int rowW = (w - 36) / 4;
        DrawStatCard(b, x, rowY, rowW, "이번 계절 매출", $"{c.SeasonRevenue:N0}G");
        DrawStatCard(b, x + rowW + 12, rowY, rowW, "누적 매출", $"{c.LifetimeRevenue:N0}G");
        DrawStatCard(b, x + (rowW + 12) * 2, rowY, rowW, "완료 계약", $"{c.ContractsCompleted:N0}건");
        DrawStatCard(b, x + (rowW + 12) * 3, rowY, rowW, "단골 이상", $"{Mod.ClientProfiles.Count(p => Mod.Clients.GetRelationship(p.Key).Trust >= 20):N0}곳");

        string status = !Context.IsMultiplayer
            ? "싱글플레이 · 회사 데이터 저장 중"
            : Mod.Multiplayer.IsSynchronized
                ? "멀티플레이 · 거래처 관계 포함 공동 회사 데이터 동기화 완료"
                : "멀티플레이 · 공동 경영 데이터 동기화 중";

        int noteY = rowY + 145;
        drawTextureBox(b, x, noteY, w, 82, Color.White);
        b.DrawString(Game1.smallFont, "Agricultural Company 0.5 · Client Relationship", new Vector2(x + 18, noteY + 14), Accent);
        b.DrawString(Game1.smallFont, "농사 → 생산 → 계약 → 납품 → 거래처 신뢰 → 더 큰 계약", new Vector2(x + 18, noteY + 39), Game1.textColor);
        b.DrawString(Game1.smallFont, status, new Vector2(x + 18, noteY + 60), Muted);
    }

    private void DrawProduction(SpriteBatch b)
    {
        Mod.Company.EnsureState();
        Mod.Production.EnsureState();
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 24;
        int w = width - 280;

        b.DrawString(Game1.dialogueFont, "생산 관리", new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, "모든 공동 경영자가 동일하게 생산을 시작할 수 있으며 공용 재고에서 안전하게 처리됩니다.", new Vector2(x, y + 43), Muted);

        int queueCap = Mod.Production.GetQueueCapacity();
        DrawMiniInfo(b, new Rectangle(x, y + 72, 185, 55), "가동 라인", $"{Mod.State.ProductionQueue.Count} / {queueCap}");
        DrawMiniInfo(b, new Rectangle(x + 196, y + 72, 185, 55), "완제품 재고", $"{Mod.Production.GetFinishedGoodsTotal():N0}");
        DrawMiniInfo(b, new Rectangle(x + 392, y + 72, 185, 55), "누적 배치", $"{Mod.State.LifetimeProductionBatches:N0}");
        DrawMiniInfo(b, new Rectangle(x + 588, y + 72, w - 608, 55), "생산품 누적", $"{Mod.State.LifetimeFinishedGoods:N0}");

        int headerY = y + 143;
        b.DrawString(Game1.smallFont, "제품 / 원재료", new Vector2(x + 10, headerY), Muted);
        b.DrawString(Game1.smallFont, "재고", new Vector2(x + 312, headerY), Muted);
        b.DrawString(Game1.smallFont, "시간", new Vector2(x + 393, headerY), Muted);
        b.DrawString(Game1.smallFont, "완제품", new Vector2(x + 484, headerY), Muted);
        b.DrawString(Game1.smallFont, "생산 시작", new Vector2(x + 610, headerY), Muted);

        int recipeY = headerY + 28;
        for (int i = 0; i < Mod.Recipes.Count && i < 4; i++)
        {
            ProductionRecipeDefinition recipe = Mod.Recipes[i];
            Rectangle row = new(x, recipeY + i * 72, w - 20, 64);
            bool unlocked = Mod.State.Level >= recipe.RequiredCompanyLevel;
            b.Draw(Game1.fadeToBlackRect, row, i % 2 == 0 ? Soft : new Color(246, 246, 239));

            int ingredient = Mod.Production.GetIngredientQuantity(recipe);
            int finished = Mod.Production.GetFinishedQuantity(recipe.Key);
            string ingredientName = GetIngredientName(recipe);
            string title = unlocked ? recipe.DisplayName : $"{recipe.DisplayName}  [Lv.{recipe.RequiredCompanyLevel}]";

            b.DrawString(Game1.smallFont, title, new Vector2(row.X + 12, row.Y + 7), unlocked ? Game1.textColor : Disabled);
            b.DrawString(Game1.smallFont, $"{ingredientName} {recipe.InputQuantity} → {recipe.OutputQuantity}", new Vector2(row.X + 12, row.Y + 34), Muted);
            b.DrawString(Game1.smallFont, ingredient.ToString("N0"), new Vector2(row.X + 315, row.Y + 21), ingredient >= recipe.InputQuantity ? Accent : Color.DarkRed);
            b.DrawString(Game1.smallFont, ProductionCore.FormatDuration(recipe.DurationMinutes), new Vector2(row.X + 392, row.Y + 21), Muted);
            b.DrawString(Game1.smallFont, finished.ToString("N0"), new Vector2(row.X + 487, row.Y + 21), Accent);

            bool synced = !Context.IsMultiplayer || Mod.Multiplayer.IsSynchronized;
            bool canStart = synced && unlocked && ingredient >= recipe.InputQuantity && Mod.State.ProductionQueue.Count < queueCap;
            DrawSmallButton(b, ProductionButtonRect(i, 0), "1배치", canStart ? Button : Disabled);
            DrawSmallButton(b, ProductionButtonRect(i, 1), "최대", canStart ? Button : Disabled);
        }

        int queueY = recipeY + 4 * 72 + 8;
        b.DrawString(Game1.smallFont, "현재 생산 큐", new Vector2(x + 5, queueY), Game1.textColor);
        int jobY = queueY + 28;
        if (Mod.State.ProductionQueue.Count == 0)
        {
            b.DrawString(Game1.smallFont, "가동 중인 생산라인이 없습니다.", new Vector2(x + 10, jobY + 12), Muted);
        }
        else
        {
            int slotW = (w - 34) / Math.Max(1, queueCap);
            for (int i = 0; i < Mod.State.ProductionQueue.Count; i++)
            {
                ProductionJob job = Mod.State.ProductionQueue[i];
                ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(job.RecipeKey);
                Rectangle slot = new(x + i * (slotW + 8), jobY, slotW, 75);
                drawTextureBox(b, slot.X, slot.Y, slot.Width, slot.Height, Color.White);
                string name = recipe?.DisplayName ?? job.RecipeKey;
                b.DrawString(Game1.smallFont, $"{name} ×{job.BatchCount}", new Vector2(slot.X + 12, slot.Y + 9), Game1.textColor);
                float progress = job.TotalMinutes <= 0 ? 1f : Math.Clamp(1f - job.RemainingMinutes / (float)job.TotalMinutes, 0f, 1f);
                Rectangle back = new(slot.X + 12, slot.Y + 38, slot.Width - 24, 10);
                b.Draw(Game1.fadeToBlackRect, back, new Color(215, 211, 195));
                b.Draw(Game1.fadeToBlackRect, new Rectangle(back.X, back.Y, (int)(back.Width * progress), back.Height), Accent);
                b.DrawString(Game1.smallFont, $"남은 시간 {ProductionCore.FormatDuration(job.RemainingMinutes)}", new Vector2(slot.X + 12, slot.Y + 51), Muted);
            }
        }

        Rectangle msg = new(x, y + 588, w - 20, 45);
        b.Draw(Game1.fadeToBlackRect, msg, Soft);
        b.DrawString(Game1.smallFont, ProductionMessage, new Vector2(msg.X + 12, msg.Y + 12), Muted);
    }

    private void HandleProductionClick(int x, int y)
    {
        for (int i = 0; i < Mod.Recipes.Count && i < 4; i++)
        {
            for (int action = 0; action < 2; action++)
            {
                if (!ProductionButtonRect(i, action).Contains(x, y))
                    continue;

                ProductionRecipeDefinition recipe = Mod.Recipes[i];
                int batches = action == 0 ? 1 : Math.Min(10, Mod.Production.GetMaxBatches(recipe));
                if (Mod.Production.TryStart(recipe.Key, Math.Max(1, batches), out string message))
                {
                    ProductionMessage = message;
                    Game1.playSound(Context.IsMultiplayer && !Context.IsMainPlayer ? "smallSelect" : "Ship");
                }
                else
                {
                    ProductionMessage = message;
                    Game1.playSound("cancel");
                }
                return;
            }
        }
    }

    private Rectangle ProductionButtonRect(int row, int action)
    {
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 24;
        int recipeY = y + 143 + 28 + row * 72;
        int baseX = x + 585;
        return new Rectangle(baseX + action * 92, recipeY + 13, 84, 38);
    }

    private string GetIngredientName(ProductionRecipeDefinition recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientItemId))
            return Mod.Crops.FirstOrDefault(p => string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? "지정 원물";
        return Mod.Crops.FirstOrDefault(p => string.Equals(p.Family, recipe.IngredientFamily, StringComparison.OrdinalIgnoreCase))?.FamilyDisplayName ?? recipe.IngredientFamily;
    }

    private void DrawWarehouse(SpriteBatch b)
    {
        Mod.Company.EnsureState();
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 26;
        int w = width - 280;

        bool canManage = !Context.IsMultiplayer || Mod.Multiplayer.LocalHasWarehouseControl;
        string controlStatus = Mod.Multiplayer.GetWarehouseControlStatus();

        b.DrawString(Game1.dialogueFont, "회사 창고", new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, "공동 창고 · 먼저 들어온 공동 경영자가 관리하고 다른 사람은 열람합니다.", new Vector2(x, y + 42), Muted);
        b.DrawString(Game1.smallFont, controlStatus, new Vector2(x, y + 66), canManage ? Accent : Color.DarkRed);

        int used = Mod.Company.GetWarehouseUsed();
        int capacity = Mod.Company.GetWarehouseCapacity();
        float ratio = capacity <= 0 ? 0f : Math.Clamp(used / (float)capacity, 0f, 1f);
        Rectangle capacityBack = new(x, y + 95, w - 20, 18);
        b.Draw(Game1.fadeToBlackRect, capacityBack, new Color(214, 211, 195));
        b.Draw(Game1.fadeToBlackRect, new Rectangle(capacityBack.X, capacityBack.Y, (int)(capacityBack.Width * ratio), capacityBack.Height), Accent);
        b.DrawString(Game1.smallFont, $"{used:N0} / {capacity:N0} 칸 사용", new Vector2(x, y + 119), ratio >= 0.9f ? Color.DarkRed : Muted);

        int headerY = y + 145;
        List<TrackedCropDefinition> crops = GetSortedCrops();
        int start = WarehousePage * WarehouseRowsPerPage;
        int rowY = headerY + 30;

        for (int row = 0; row < WarehouseRowsPerPage; row++)
        {
            int index = start + row;
            if (index >= crops.Count)
                break;

            TrackedCropDefinition crop = crops[index];
            Rectangle rowRect = new(x, rowY + row * 58, w - 20, 50);
            b.Draw(Game1.fadeToBlackRect, rowRect, row % 2 == 0 ? Soft : new Color(246, 246, 239));
            int player = Mod.Company.GetPlayerQuantity(crop.ItemId);
            int stock = Mod.Company.GetWarehouseQuantity(crop.ItemId);
            string quality = GetQualitySummary(crop.ItemId);

            b.DrawString(Game1.smallFont, crop.DisplayName, new Vector2(rowRect.X + 12, rowRect.Y + 6), Game1.textColor);
            if (!string.IsNullOrEmpty(quality))
                b.DrawString(Game1.smallFont, quality, new Vector2(rowRect.X + 12, rowRect.Y + 27), Muted);
            b.DrawString(Game1.smallFont, $"소지 {player:N0}", new Vector2(rowRect.X + 205, rowRect.Y + 14), Muted);
            b.DrawString(Game1.smallFont, $"창고 {stock:N0}", new Vector2(rowRect.X + 285, rowRect.Y + 14), Accent);

            DrawSmallButton(b, WarehouseButtonRect(row, 0), "+1", canManage ? Button : Disabled);
            DrawSmallButton(b, WarehouseButtonRect(row, 1), "전부", canManage ? Button : Disabled);
            DrawSmallButton(b, WarehouseButtonRect(row, 2), "-1", canManage ? ButtonAlt : Disabled);
            DrawSmallButton(b, WarehouseButtonRect(row, 3), "전부", canManage ? ButtonAlt : Disabled);
        }

        int footerY = y + 528;
        Rectangle messageBox = new(x, footerY, w - 20, 55);
        drawTextureBox(b, messageBox.X, messageBox.Y, messageBox.Width, messageBox.Height, Color.White);
        b.DrawString(Game1.smallFont, WarehouseMessage, new Vector2(messageBox.X + 15, messageBox.Y + 17), Muted);

        int maxPage = GetWarehouseMaxPage();
        DrawSmallButton(b, PrevPageRect(), "< 이전", WarehousePage > 0 ? Button : Disabled);
        DrawSmallButton(b, NextPageRect(), "다음 >", WarehousePage < maxPage ? Button : Disabled);
        b.DrawString(Game1.smallFont, $"{WarehousePage + 1} / {maxPage + 1}", new Vector2(x + w - 165, footerY + 72), Muted);
    }

    private void HandleWarehouseClick(int x, int y)
    {
        if (PrevPageRect().Contains(x, y) && WarehousePage > 0)
        {
            WarehousePage--;
            Game1.playSound("shiny4");
            return;
        }
        if (NextPageRect().Contains(x, y) && WarehousePage < GetWarehouseMaxPage())
        {
            WarehousePage++;
            Game1.playSound("shiny4");
            return;
        }

        if (Context.IsMultiplayer && !Mod.Multiplayer.LocalHasWarehouseControl)
        {
            WarehouseMessage = Mod.Multiplayer.GetWarehouseControlStatus();
            Game1.playSound("cancel");
            return;
        }

        List<TrackedCropDefinition> crops = GetSortedCrops();
        int start = WarehousePage * WarehouseRowsPerPage;
        for (int row = 0; row < WarehouseRowsPerPage; row++)
        {
            int index = start + row;
            if (index >= crops.Count)
                break;
            TrackedCropDefinition crop = crops[index];

            for (int action = 0; action < 4; action++)
            {
                if (!WarehouseButtonRect(row, action).Contains(x, y))
                    continue;

                int moved = action switch
                {
                    0 => Mod.Company.DepositFromPlayer(crop.ItemId, 1),
                    1 => Mod.Company.DepositAllFromPlayer(crop.ItemId),
                    2 => Mod.Company.WithdrawToPlayer(crop.ItemId, 1),
                    3 => Mod.Company.WithdrawAllToPlayer(crop.ItemId),
                    _ => 0
                };

                if (moved == -2)
                    WarehouseMessage = Mod.Multiplayer.GetWarehouseControlStatus();
                else if (moved == -1)
                    WarehouseMessage = $"{crop.DisplayName} 처리를 공동 회사 데이터에 반영 중입니다.";
                else if (moved > 0)
                    WarehouseMessage = $"{crop.DisplayName} {moved:N0}개 {(action <= 1 ? "입고" : "출고")} 완료.";
                else
                    WarehouseMessage = action <= 1 ? $"{crop.DisplayName}을(를) 입고할 수 없습니다." : $"{crop.DisplayName}을(를) 출고할 수 없습니다.";

                Game1.playSound(moved > 0 ? "Ship" : moved == -1 ? "smallSelect" : "cancel");
                return;
            }
        }
    }

    private void DrawContracts(SpriteBatch b)
    {
        Mod.Contracts.EnsureState();
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 24;
        int w = width - 280;
        CompanySaveData c = Mod.State;

        b.DrawString(Game1.dialogueFont, "계약 · 납품", new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, "완제품을 납품해 회사 자금과 평판, 거래처 신뢰를 함께 올립니다.", new Vector2(x, y + 43), Muted);

        int infoY = y + 72;
        DrawMiniInfo(b, new Rectangle(x, infoY, 185, 55), "회사 자금", $"{c.CompanyFunds:N0}G");
        DrawMiniInfo(b, new Rectangle(x + 196, infoY, 185, 55), "평판", c.Reputation.ToString("N0"));
        DrawMiniInfo(b, new Rectangle(x + 392, infoY, 185, 55), "진행 계약", $"{c.AcceptedContracts.Count}/{Mod.Contracts.GetActiveCapacity()}");
        DrawMiniInfo(b, new Rectangle(x + 588, infoY, w - 608, 55), "완료", c.ContractsCompleted.ToString("N0"));

        List<(CompanyContract Contract, bool Active)> rows = GetContractRows();
        int start = ContractPage * ContractRowsPerPage;
        int listY = y + 143;

        if (rows.Count == 0)
        {
            drawTextureBox(b, x, listY, w - 20, 250, Color.White);
            b.DrawString(Game1.dialogueFont, "현재 표시할 계약이 없습니다.", new Vector2(x + 30, listY + 70), Muted);
            b.DrawString(Game1.smallFont, "계약 게시판은 게임 날짜가 바뀔 때 새로 갱신됩니다.", new Vector2(x + 32, listY + 125), Muted);
        }
        else
        {
            for (int row = 0; row < ContractRowsPerPage; row++)
            {
                int index = start + row;
                if (index >= rows.Count)
                    break;

                CompanyContract contract = rows[index].Contract;
                bool active = rows[index].Active;
                Rectangle rect = new(x, listY + row * 82, w - 20, 74);
                b.Draw(Game1.fadeToBlackRect, rect, row % 2 == 0 ? Soft : new Color(246, 246, 239));

                string product = Mod.Contracts.GetProductName(contract.ProductKey);
                string state = active ? "진행" : "게시";
                string quantity = active ? $"{contract.DeliveredQuantity}/{contract.RequiredQuantity}" : $"{contract.RequiredQuantity}개";
                int qualifying = Mod.Contracts.GetQualifyingFinishedQuantity(contract.ProductKey, contract.MinimumQuality);
                int days = Mod.Contracts.GetDaysRemaining(contract);
                ClientRelationship relation = Mod.Clients.GetRelationship(contract.ClientKey);

                b.DrawString(Game1.smallFont, $"[{state}/{contract.ContractKind}] {contract.ClientName} · {product}", new Vector2(rect.X + 10, rect.Y + 7), Game1.textColor);
                b.DrawString(Game1.smallFont, $"수량 {quantity} · {ContractCore.QualityRequirementText(contract.MinimumQuality)} · 납기 {days}일", new Vector2(rect.X + 10, rect.Y + 33), Muted);
                b.DrawString(Game1.smallFont, $"재고 {qualifying:N0} · {contract.RewardGold:N0}G · 신뢰 {relation.Trust}/100", new Vector2(rect.X + 350, rect.Y + 33), Accent);

                bool synced = !Context.IsMultiplayer || Mod.Multiplayer.IsSynchronized;
                bool canAction = synced && (active ? qualifying > 0 : c.AcceptedContracts.Count < Mod.Contracts.GetActiveCapacity());
                DrawSmallButton(b, ContractButtonRect(row), active ? "납품" : "수락", canAction ? (active ? ButtonAlt : Button) : Disabled);
            }
        }

        Rectangle msg = new(x, y + 558, w - 20, 44);
        b.Draw(Game1.fadeToBlackRect, msg, Soft);
        b.DrawString(Game1.smallFont, ContractMessage, new Vector2(msg.X + 12, msg.Y + 11), Muted);

        int maxPage = GetContractMaxPage();
        DrawSmallButton(b, ContractPrevRect(), "< 이전", ContractPage > 0 ? Button : Disabled);
        DrawSmallButton(b, ContractNextRect(), "다음 >", ContractPage < maxPage ? Button : Disabled);
        b.DrawString(Game1.smallFont, $"{ContractPage + 1} / {maxPage + 1}", new Vector2(x + w - 160, y + 617), Muted);
    }

    private void HandleContractClick(int x, int y)
    {
        if (ContractPrevRect().Contains(x, y) && ContractPage > 0)
        {
            ContractPage--;
            Game1.playSound("shiny4");
            return;
        }
        if (ContractNextRect().Contains(x, y) && ContractPage < GetContractMaxPage())
        {
            ContractPage++;
            Game1.playSound("shiny4");
            return;
        }

        List<(CompanyContract Contract, bool Active)> rows = GetContractRows();
        int start = ContractPage * ContractRowsPerPage;
        for (int row = 0; row < ContractRowsPerPage; row++)
        {
            int index = start + row;
            if (index >= rows.Count || !ContractButtonRect(row).Contains(x, y))
                continue;

            CompanyContract contract = rows[index].Contract;
            bool active = rows[index].Active;
            bool ok = active
                ? Mod.Contracts.TryDeliver(contract.Id, out string message)
                : Mod.Contracts.TryAccept(contract.Id, out message);
            ContractMessage = message;
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }
    }

    private void DrawClients(SpriteBatch b)
    {
        Mod.Clients.EnsureState();
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 24;
        int w = width - 280;

        IReadOnlyList<ClientProfileDefinition> clients = Mod.Clients.GetVisibleClients();
        List<ClientProfileDefinition> unlocked = clients.Where(p => p.RequiredCompanyLevel <= Mod.State.Level).ToList();
        int regular = unlocked.Count(p => Mod.Clients.GetRelationship(p.Key).Trust >= 20);
        int core = unlocked.Count(p => Mod.Clients.GetRelationship(p.Key).Trust >= 80);
        int averageTrust = unlocked.Count == 0 ? 0 : (int)Math.Round(unlocked.Average(p => Mod.Clients.GetRelationship(p.Key).Trust));

        b.DrawString(Game1.dialogueFont, "거래처 관계", new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, "계약 실적이 거래처별로 누적됩니다. 신뢰가 오르면 정기·우선·핵심 계약과 보상 보너스가 열립니다.", new Vector2(x, y + 43), Muted);

        int infoY = y + 72;
        DrawMiniInfo(b, new Rectangle(x, infoY, 185, 55), "거래 가능", $"{unlocked.Count}/{clients.Count}");
        DrawMiniInfo(b, new Rectangle(x + 196, infoY, 185, 55), "평균 신뢰", $"{averageTrust}/100");
        DrawMiniInfo(b, new Rectangle(x + 392, infoY, 185, 55), "단골 이상", $"{regular}곳");
        DrawMiniInfo(b, new Rectangle(x + 588, infoY, w - 608, 55), "핵심 파트너", $"{core}곳");

        int start = ClientPage * ClientRowsPerPage;
        int listY = y + 143;
        for (int row = 0; row < ClientRowsPerPage; row++)
        {
            int index = start + row;
            if (index >= clients.Count)
                break;

            ClientProfileDefinition profile = clients[index];
            ClientRelationship relation = Mod.Clients.GetRelationship(profile.Key);
            bool levelUnlocked = profile.RequiredCompanyLevel <= Mod.State.Level;
            bool productAvailable = Mod.Contracts.IsProductAvailable(profile.PreferredProductKey);
            bool available = levelUnlocked && productAvailable;
            Rectangle rect = new(x, listY + row * 82, w - 20, 74);
            b.Draw(Game1.fadeToBlackRect, rect, row % 2 == 0 ? Soft : new Color(246, 246, 239));

            string tier = Mod.Clients.GetTierName(relation.Trust);
            string product = Mod.Contracts.GetProductName(profile.PreferredProductKey);
            string availability = available ? tier : !levelUnlocked ? $"회사 Lv.{profile.RequiredCompanyLevel} 필요" : "연동 작물 모드 필요";
            Color main = available ? Game1.textColor : Disabled;

            b.DrawString(Game1.smallFont, $"{profile.DisplayName} · {profile.Category}", new Vector2(rect.X + 10, rect.Y + 6), main);
            b.DrawString(Game1.smallFont, availability, new Vector2(rect.X + 10, rect.Y + 32), available ? Accent : Disabled);

            Rectangle trustBack = new(rect.X + 210, rect.Y + 13, 145, 12);
            b.Draw(Game1.fadeToBlackRect, trustBack, new Color(215, 211, 195));
            b.Draw(Game1.fadeToBlackRect, new Rectangle(trustBack.X, trustBack.Y, (int)(trustBack.Width * Math.Clamp(relation.Trust / 100f, 0f, 1f)), trustBack.Height), available ? Accent : Disabled);
            b.DrawString(Game1.smallFont, $"신뢰 {relation.Trust}/100", new Vector2(rect.X + 210, rect.Y + 32), Muted);

            int rewardBonus = Mod.Clients.GetRewardBonusPercent(profile.Key);
            int quantityBonus = Mod.Clients.GetQuantityBonusPercent(profile.Key);
            b.DrawString(Game1.smallFont, $"선호 {product}", new Vector2(rect.X + 375, rect.Y + 7), main);
            b.DrawString(Game1.smallFont, $"완료 {relation.CompletedContracts} · 실패 {relation.FailedContracts} · 정시 {relation.OnTimeDeliveries}", new Vector2(rect.X + 375, rect.Y + 31), Muted);
            b.DrawString(Game1.smallFont, $"누적 {relation.LifetimeRevenue:N0}G · 보상 +{rewardBonus}% · 물량 +{quantityBonus}%", new Vector2(rect.X + 570, rect.Y + 31), available ? Accent : Disabled);
        }

        Rectangle note = new(x, y + 558, w - 20, 44);
        b.Draw(Game1.fadeToBlackRect, note, Soft);
        b.DrawString(Game1.smallFont, "신뢰 20: 정기계약 · 50: 우선계약 · 80: 핵심 파트너 · 멀티에서는 전원이 같은 관계 기록을 공유합니다.", new Vector2(note.X + 12, note.Y + 11), Muted);

        int maxPage = GetClientMaxPage();
        DrawSmallButton(b, ClientPrevRect(), "< 이전", ClientPage > 0 ? Button : Disabled);
        DrawSmallButton(b, ClientNextRect(), "다음 >", ClientPage < maxPage ? Button : Disabled);
        b.DrawString(Game1.smallFont, $"{ClientPage + 1} / {maxPage + 1}", new Vector2(x + w - 160, y + 617), Muted);
    }

    private void HandleClientClick(int x, int y)
    {
        if (ClientPrevRect().Contains(x, y) && ClientPage > 0)
        {
            ClientPage--;
            Game1.playSound("shiny4");
            return;
        }

        if (ClientNextRect().Contains(x, y) && ClientPage < GetClientMaxPage())
        {
            ClientPage++;
            Game1.playSound("shiny4");
        }
    }

    private List<(CompanyContract Contract, bool Active)> GetContractRows()
    {
        List<(CompanyContract Contract, bool Active)> result = new();
        result.AddRange(Mod.State.AcceptedContracts
            .OrderBy(p => p.DeadlineDayNumber)
            .Select(p => (p, true)));
        result.AddRange(Mod.State.AvailableContracts
            .OrderBy(p => p.DeadlineDayNumber)
            .Select(p => (p, false)));
        return result;
    }

    private Rectangle ContractButtonRect(int row)
        => new(xPositionOnScreen + width - 150, yPositionOnScreen + 24 + 143 + row * 82 + 17, 88, 38);

    private Rectangle ContractPrevRect()
        => new(xPositionOnScreen + width - 360, yPositionOnScreen + height - 45, 85, 32);

    private Rectangle ContractNextRect()
        => new(xPositionOnScreen + width - 265, yPositionOnScreen + height - 45, 85, 32);

    private int GetContractMaxPage()
        => Math.Max(0, (GetContractRows().Count - 1) / ContractRowsPerPage);

    private Rectangle ClientPrevRect()
        => new(xPositionOnScreen + width - 360, yPositionOnScreen + height - 45, 85, 32);

    private Rectangle ClientNextRect()
        => new(xPositionOnScreen + width - 265, yPositionOnScreen + height - 45, 85, 32);

    private int GetClientMaxPage()
        => Math.Max(0, (Mod.Clients.GetVisibleClients().Count - 1) / ClientRowsPerPage);

    private Rectangle WarehouseButtonRect(int row, int action)
    {
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 26;
        int rowY = y + 145 + 30 + row * 58;
        int baseX = x + 355;
        int buttonW = 76;
        int gap = 6;
        return new Rectangle(baseX + action * (buttonW + gap), rowY + 7, buttonW, 36);
    }

    private Rectangle PrevPageRect()
        => new(xPositionOnScreen + width - 360, yPositionOnScreen + height - 55, 85, 34);

    private Rectangle NextPageRect()
        => new(xPositionOnScreen + width - 265, yPositionOnScreen + height - 55, 85, 34);

    private int GetWarehouseMaxPage()
        => Math.Max(0, (GetSortedCrops().Count - 1) / WarehouseRowsPerPage);

    private List<TrackedCropDefinition> GetSortedCrops()
        => Mod.Crops.OrderBy(p => FamilyOrder(p.Family)).ThenBy(p => p.DisplayName, StringComparer.CurrentCulture).ToList();

    private static int FamilyOrder(string family) => family switch
    {
        "Vanilla" => 0,
        "Watermelon" => 1,
        "KoreanMelon" => 2,
        "NapaCabbage" => 3,
        _ => 9
    };

    private string GetQualitySummary(string itemId)
    {
        IReadOnlyList<(int Quality, int Quantity)> values = Mod.Company.GetQualityBreakdown(itemId);
        return values.Count == 0 ? "" : string.Join(" · ", values.Select(p => $"{CompanyCore.QualityName(p.Quality)} {p.Quantity:N0}"));
    }

    private void DrawXp(SpriteBatch b, int x, int y, int w)
    {
        CompanySaveData c = Mod.State;
        int start = CompanyCore.GetLevelStartXp(c.Level);
        int next = CompanyCore.GetNextLevelXp(c.Level);
        float p = c.Level >= 5 ? 1f : Math.Clamp((c.Experience - start) / (float)Math.Max(1, next - start), 0f, 1f);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, w, 16), new Color(215, 211, 195));
        b.Draw(Game1.fadeToBlackRect, new Rectangle(x, y, (int)(w * p), 16), Accent);
        string text = c.Level >= 5 ? $"회사 경험치 {c.Experience:N0} · 최고 단계" : $"회사 경험치 {c.Experience:N0} / {next:N0}";
        b.DrawString(Game1.smallFont, text, new Vector2(x + w - Game1.smallFont.MeasureString(text).X, y + 19), Muted);
    }

    private static void DrawCard(SpriteBatch b, int x, int y, int w, string label, string value)
    {
        drawTextureBox(b, x, y, w, 95, Color.White);
        b.DrawString(Game1.smallFont, label, new Vector2(x + 14, y + 17), Muted);
        b.DrawString(Game1.dialogueFont, value, new Vector2(x + 13, y + 43), Game1.textColor);
    }

    private static void DrawStatCard(SpriteBatch b, int x, int y, int w, string label, string value)
    {
        drawTextureBox(b, x, y, w, 120, Color.White);
        b.DrawString(Game1.smallFont, label, new Vector2(x + 14, y + 20), Muted);
        b.DrawString(Game1.dialogueFont, value, new Vector2(x + 14, y + 56), Game1.textColor);
    }

    private static void DrawMiniInfo(SpriteBatch b, Rectangle rect, string label, string value)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, Color.White);
        b.DrawString(Game1.smallFont, label, new Vector2(rect.X + 10, rect.Y + 8), Muted);
        Vector2 valueSize = Game1.smallFont.MeasureString(value);
        b.DrawString(Game1.smallFont, value, new Vector2(rect.Right - valueSize.X - 10, rect.Y + 27), Accent);
    }

    private static void DrawSmallButton(SpriteBatch b, Rectangle rect, string text, Color color)
    {
        b.Draw(Game1.fadeToBlackRect, rect, color);
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(rect.X + rect.Width / 2 - size.X / 2, rect.Y + rect.Height / 2 - size.Y / 2), Color.White);
    }

    private void DrawComingSoon(SpriteBatch b, string tab)
    {
        int x = xPositionOnScreen + 285;
        int y = yPositionOnScreen + 155;
        int w = width - 350;
        drawTextureBox(b, x, y, w, 300, Color.White);
        b.DrawString(Game1.dialogueFont, tab, new Vector2(x, y - 65), Game1.textColor);
        b.DrawString(Game1.dialogueFont, $"{tab} 시스템 준비 중", new Vector2(x + 40, y + 95), Accent);
        b.DrawString(Game1.smallFont, "후속 업데이트에서 실제 회사 기능으로 연결됩니다.", new Vector2(x + 42, y + 155), Muted);
    }
}
