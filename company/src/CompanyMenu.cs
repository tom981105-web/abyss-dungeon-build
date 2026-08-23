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
    private string WarehouseMessage = "작물을 선택해 회사 창고로 입고하거나 다시 꺼낼 수 있습니다.";
    private string ProductionMessage = "회사 창고의 원물을 사용해 완제품 생산을 시작할 수 있습니다.";

    private const int WarehouseRowsPerPage = 6;

    private static readonly Color Green = new(48, 78, 58);
    private static readonly Color Green2 = new(78, 118, 84);
    private static readonly Color Accent = new(90, 128, 76);
    private static readonly Color Muted = new(105, 99, 82);
    private static readonly Color Soft = new(235, 239, 228);
    private static readonly Color Button = new(68, 103, 73);
    private static readonly Color ButtonAlt = new(112, 99, 70);
    private static readonly Color Disabled = new(160, 160, 150);

    internal CompanyMenu(ModEntry mod)
        : base(Game1.viewport.Width / 2 - 540, Game1.viewport.Height / 2 - 330, 1080, 660, true)
    {
        Mod = mod;
        Mod.Company.EnsureState();
        Mod.Production.EnsureState();
        string[] names = { "대시보드", "생산", "창고", "계약", "거래처", "연구개발", "브랜드", "직원", "재무" };
        for (int i = 0; i < names.Length; i++)
            Tabs.Add((names[i], new Rectangle(xPositionOnScreen + 18, yPositionOnScreen + 104 + i * 50, 190, 40)));
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            exitThisMenu();
            return;
        }

        for (int i = 0; i < Tabs.Count; i++)
        {
            if (!Tabs[i].Bounds.Contains(x, y))
                continue;
            SelectedTab = i;
            Game1.playSound("smallSelect");
            return;
        }

        if (SelectedTab == 1)
            HandleProductionClick(x, y);
        else if (SelectedTab == 2)
            HandleWarehouseClick(x, y);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (SelectedTab != 2)
            return;

        int maxPage = GetWarehouseMaxPage();
        if (direction < 0 && WarehousePage < maxPage)
        {
            WarehousePage++;
            Game1.playSound("shiny4");
        }
        else if (direction > 0 && WarehousePage > 0)
        {
            WarehousePage--;
            Game1.playSound("shiny4");
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
        else
            DrawComingSoon(b, Tabs[SelectedTab].Name);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawSidebar(SpriteBatch b)
    {
        b.DrawString(Game1.dialogueFont, "농업회사", new Vector2(xPositionOnScreen + 25, yPositionOnScreen + 23), Color.White);
        b.DrawString(Game1.smallFont, "MULTIPLAYER 0.3.1", new Vector2(xPositionOnScreen + 27, yPositionOnScreen + 67), new Color(215, 228, 210));

        for (int i = 0; i < Tabs.Count; i++)
        {
            var tab = Tabs[i];
            if (i == SelectedTab)
                b.Draw(Game1.fadeToBlackRect, tab.Bounds, Green2);
            b.DrawString(Game1.smallFont, tab.Name, new Vector2(tab.Bounds.X + 16, tab.Bounds.Y + 8), Color.White);
        }

        string role = !Context.IsMultiplayer ? "싱글플레이" : Context.IsMainPlayer ? "HOST" : "FARMHAND";
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
        b.DrawString(Game1.smallFont, $"Lv.{c.Level} · {CompanyCore.GetStageName(c.Level)}", new Vector2(x, y + 47), Accent);
        DrawXp(b, x, y + 77, w - 35);

        int cardY = y + 120;
        int gap = 12;
        int cardW = (w - gap * 3) / 4;
        DrawCard(b, x, cardY, cardW, "운영 자금", $"{Game1.player.Money:N0}G");
        DrawCard(b, x + (cardW + gap), cardY, cardW, "원물 재고", $"{Mod.Company.GetWarehouseUsed():N0}");
        DrawCard(b, x + (cardW + gap) * 2, cardY, cardW, "완제품", $"{Mod.Production.GetFinishedGoodsTotal():N0}");
        DrawCard(b, x + (cardW + gap) * 3, cardY, cardW, "가동 라인", $"{c.ProductionQueue.Count}/{Mod.Production.GetQueueCapacity()}");

        int sectionY = cardY + 125;
        b.DrawString(Game1.dialogueFont, "작물 생산 현황", new Vector2(x, sectionY), Game1.textColor);
        b.DrawString(Game1.smallFont, "수확한 원물은 창고에 입고한 뒤 생산라인의 재료로 사용할 수 있습니다.", new Vector2(x, sectionY + 39), Muted);

        int rowY = sectionY + 78;
        int rowW = (w - 36) / 4;
        DrawCropCard(b, x, rowY, rowW, "기본 작물", "Vanilla");
        DrawCropCard(b, x + rowW + 12, rowY, rowW, "수박 계열", "Watermelon");
        DrawCropCard(b, x + (rowW + 12) * 2, rowY, rowW, "참외 계열", "KoreanMelon");
        DrawCropCard(b, x + (rowW + 12) * 3, rowY, rowW, "배추", "NapaCabbage");

        string multiplayerStatus = !Context.IsMultiplayer
            ? "싱글플레이 · 로컬 회사 데이터"
            : Context.IsMainPlayer
                ? "멀티 호스트 · 회사 데이터 최종 권한"
                : Mod.Multiplayer.IsSynchronized
                    ? "멀티 게스트 · 호스트와 동기화 완료"
                    : "멀티 게스트 · 호스트 동기화 중";

        int noteY = rowY + 175;
        drawTextureBox(b, x, noteY, w, 72, Color.White);
        b.DrawString(Game1.smallFont, "Agricultural Company 0.3.1 · Multiplayer Foundation", new Vector2(x + 18, noteY + 12), Accent);
        b.DrawString(Game1.smallFont, multiplayerStatus, new Vector2(x + 18, noteY + 40), Muted);
    }

    private void DrawProduction(SpriteBatch b)
    {
        Mod.Company.EnsureState();
        Mod.Production.EnsureState();
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 24;
        int w = width - 280;

        b.DrawString(Game1.dialogueFont, "생산 관리", new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, "멀티에서는 호스트가 원재료 차감·생산 큐·완제품을 최종 처리합니다.", new Vector2(x, y + 43), Muted);

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

            bool synced = !Context.IsMultiplayer || Context.IsMainPlayer || Mod.Multiplayer.IsSynchronized;
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

        b.DrawString(Game1.dialogueFont, "회사 창고", new Vector2(x, y), Game1.textColor);
        b.DrawString(Game1.smallFont, "원물 농산물 보관 · 품질 등급 보존 · 멀티 호스트 승인", new Vector2(x, y + 44), Muted);

        int used = Mod.Company.GetWarehouseUsed();
        int capacity = Mod.Company.GetWarehouseCapacity();
        float ratio = capacity <= 0 ? 0f : Math.Clamp(used / (float)capacity, 0f, 1f);
        Rectangle capacityBack = new(x, y + 75, w - 20, 18);
        b.Draw(Game1.fadeToBlackRect, capacityBack, new Color(214, 211, 195));
        b.Draw(Game1.fadeToBlackRect, new Rectangle(capacityBack.X, capacityBack.Y, (int)(capacityBack.Width * ratio), capacityBack.Height), Accent);
        string capText = $"{used:N0} / {capacity:N0} 칸 사용";
        b.DrawString(Game1.smallFont, capText, new Vector2(x, y + 99), ratio >= 0.9f ? Color.DarkRed : Muted);

        int headerY = y + 135;
        b.DrawString(Game1.smallFont, "품목", new Vector2(x + 12, headerY), Muted);
        b.DrawString(Game1.smallFont, "소지", new Vector2(x + 205, headerY), Muted);
        b.DrawString(Game1.smallFont, "창고", new Vector2(x + 275, headerY), Muted);
        b.DrawString(Game1.smallFont, "입고", new Vector2(x + 370, headerY), Muted);
        b.DrawString(Game1.smallFont, "출고", new Vector2(x + 550, headerY), Muted);

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
            b.DrawString(Game1.smallFont, player.ToString("N0"), new Vector2(rowRect.X + 210, rowRect.Y + 14), Muted);
            b.DrawString(Game1.smallFont, stock.ToString("N0"), new Vector2(rowRect.X + 280, rowRect.Y + 14), Accent);

            DrawSmallButton(b, WarehouseButtonRect(row, 0), "+1", Button);
            DrawSmallButton(b, WarehouseButtonRect(row, 1), "전부", Button);
            DrawSmallButton(b, WarehouseButtonRect(row, 2), "-1", ButtonAlt);
            DrawSmallButton(b, WarehouseButtonRect(row, 3), "전부", ButtonAlt);
        }

        int footerY = y + 528;
        Rectangle messageBox = new(x, footerY, w - 20, 55);
        drawTextureBox(b, messageBox.X, messageBox.Y, messageBox.Width, messageBox.Height, Color.White);
        b.DrawString(Game1.smallFont, WarehouseMessage, new Vector2(messageBox.X + 15, messageBox.Y + 17), Muted);

        int maxPage = GetWarehouseMaxPage();
        Rectangle prev = PrevPageRect();
        Rectangle next = NextPageRect();
        DrawSmallButton(b, prev, "< 이전", WarehousePage > 0 ? Button : Disabled);
        DrawSmallButton(b, next, "다음 >", WarehousePage < maxPage ? Button : Disabled);
        string page = $"{WarehousePage + 1} / {maxPage + 1}";
        b.DrawString(Game1.smallFont, page, new Vector2(x + w - 165, footerY + 72), Muted);
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

                if (moved == -1)
                {
                    string verb = action <= 1 ? "입고" : "출고";
                    WarehouseMessage = $"{crop.DisplayName} {verb} 요청을 호스트에 전송했습니다.";
                    Game1.playSound("smallSelect");
                }
                else if (moved > 0)
                {
                    string verb = action <= 1 ? "입고" : "출고";
                    WarehouseMessage = $"{crop.DisplayName} {moved:N0}개 {verb} 완료.";
                    Game1.playSound(action <= 1 ? "Ship" : "coin");
                }
                else
                {
                    WarehouseMessage = action <= 1
                        ? (Mod.Company.GetWarehouseUsed() >= Mod.Company.GetWarehouseCapacity() ? "창고가 가득 찼습니다." : $"소지품에 {crop.DisplayName}이(가) 없습니다.")
                        : (Mod.Company.GetWarehouseQuantity(crop.ItemId) <= 0 ? $"창고에 {crop.DisplayName} 재고가 없습니다." : "인벤토리 공간이 부족합니다.");
                    Game1.playSound("cancel");
                }
                return;
            }
        }
    }

    private Rectangle WarehouseButtonRect(int row, int action)
    {
        int x = xPositionOnScreen + 245;
        int y = yPositionOnScreen + 26;
        int headerY = y + 135;
        int rowY = headerY + 30 + row * 58;
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
        => Mod.Crops
            .OrderBy(p => FamilyOrder(p.Family))
            .ThenBy(p => p.DisplayName, StringComparer.CurrentCulture)
            .ToList();

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
        if (values.Count == 0)
            return "";
        return string.Join(" · ", values.Select(p => $"{CompanyCore.QualityName(p.Quality)} {p.Quantity:N0}"));
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

    private static void DrawMiniInfo(SpriteBatch b, Rectangle rect, string label, string value)
    {
        drawTextureBox(b, rect.X, rect.Y, rect.Width, rect.Height, Color.White);
        b.DrawString(Game1.smallFont, label, new Vector2(rect.X + 10, rect.Y + 8), Muted);
        Vector2 valueSize = Game1.smallFont.MeasureString(value);
        b.DrawString(Game1.smallFont, value, new Vector2(rect.Right - valueSize.X - 10, rect.Y + 27), Accent);
    }

    private void DrawCropCard(SpriteBatch b, int x, int y, int w, string title, string family)
    {
        CompanySaveData c = Mod.State;
        drawTextureBox(b, x, y, w, 150, Color.White);
        b.DrawString(Game1.smallFont, title, new Vector2(x + 14, y + 18), Game1.textColor);
        b.DrawString(Game1.smallFont, $"오늘 {Mod.Company.GetTotal(c.TodayHarvest, family):N0}", new Vector2(x + 14, y + 55), Muted);
        b.DrawString(Game1.smallFont, $"계절 {Mod.Company.GetTotal(c.SeasonHarvest, family):N0}", new Vector2(x + 14, y + 84), Muted);
        b.DrawString(Game1.smallFont, $"누적 {Mod.Company.GetTotal(c.LifetimeHarvest, family):N0}", new Vector2(x + 14, y + 113), Accent);
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
        string version = tab == "계약" ? "0.4" : "후속 업데이트";
        b.DrawString(Game1.dialogueFont, $"{tab} 시스템 준비 중", new Vector2(x + 40, y + 95), Accent);
        b.DrawString(Game1.smallFont, $"{version}에서 실제 기능이 연결됩니다.", new Vector2(x + 42, y + 155), Muted);
    }
}
