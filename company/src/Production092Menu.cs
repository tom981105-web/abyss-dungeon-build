using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>
/// Image-backed production screen. The approved PNG is rendered verbatim;
/// 0.9.3 adds accurate interaction feedback without redrawing the UI itself.
/// </summary>
internal sealed class Production092Menu : ImageBackedUi092Base
{
    private string SelectedRecipeKey = "";
    private int SelectedLine = -1;
    private int PlanPage;
    private string Message = "버튼 위에 마우스를 올리면 클릭 가능한 영역이 표시됩니다.";

    internal Production092Menu(ModEntry mod)
        : base(mod, "assets/ui_092_production.png")
    {
        Mod.Production.EnsureState();
        SelectedRecipeKey = Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, "TomatoJuice", StringComparison.OrdinalIgnoreCase))?.Key
            ?? Mod.Recipes.FirstOrDefault(p => !p.RequiresCropGenetics)?.Key
            ?? Mod.Recipes.FirstOrDefault()?.Key ?? "";
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            exitThisMenu();
            return;
        }

        if (Company().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.activeClickableMenu = new CompanyMenu(Mod);
            return;
        }

        if (Catalog().Contains(x, y) || PlanAdd().Contains(x, y))
        {
            Game1.playSound("bigSelect");
            Game1.activeClickableMenu = new ProductCatalog092Menu(Mod, SelectedRecipeKey);
            return;
        }

        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < 3; i++)
        {
            if (!LineCard(i).Contains(x, y))
                continue;

            if (i >= lines.Count)
            {
                Message = $"라인 {i + 1}은 아직 잠겨 있습니다.";
                Game1.playSound("cancel");
                return;
            }

            SelectedLine = i;
            ProductionJob? job = Mod.Production.GetLineJob(lines[i].Id);
            ProductionRecipeDefinition? recipe = job is null
                ? Mod.Recipes.FirstOrDefault(p => string.Equals(p.LineType, lines[i].LineType, StringComparison.OrdinalIgnoreCase))
                : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is not null)
                SelectedRecipeKey = recipe.Key;

            Message = $"라인 {i + 1} · {LineLabel(lines[i].LineType)} 선택" + (recipe is null ? "" : $" — {recipe.DisplayName}");
            Game1.playSound("smallSelect");
            return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedRecipeKey);
        if (OneBatch().Contains(x, y))
        {
            if (selected is null)
            {
                Message = "생산할 제품을 먼저 선택해 주세요.";
                Game1.playSound("cancel");
                return;
            }

            bool ok = Mod.Production.TryStart(selected.Key, 1, out string result);
            Message = string.IsNullOrWhiteSpace(result) ? (ok ? $"{selected.DisplayName} 1배치 생산을 시작했습니다." : "생산을 시작하지 못했습니다.") : result;
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }

        if (MaxBatch().Contains(x, y))
        {
            if (selected is null)
            {
                Message = "생산할 제품을 먼저 선택해 주세요.";
                Game1.playSound("cancel");
                return;
            }

            int max = Mod.Production.GetMaxBatches(selected);
            if (max <= 0)
            {
                Message = $"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다.";
                Game1.playSound("cancel");
                return;
            }

            int batches = Math.Min(10, max);
            bool ok = Mod.Production.TryStart(selected.Key, batches, out string result);
            Message = string.IsNullOrWhiteSpace(result) ? (ok ? $"{selected.DisplayName} {batches}배치 생산을 시작했습니다." : "최대 생산을 시작하지 못했습니다.") : result;
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            if (!PlanRow(row).Contains(x, y))
                continue;

            int idx = start + row;
            if (idx >= plans.Count)
            {
                Message = $"생산 계획 {start + row + 1}은 비어 있습니다. 아래 ‘계획 추가’를 눌러 등록할 수 있습니다.";
                Game1.playSound("cancel");
                return;
            }

            SelectedRecipeKey = plans[idx].RecipeKey;
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedRecipeKey);
            Message = $"생산 계획 {start + row + 1} 선택" + (recipe is null ? "" : $" — {recipe.DisplayName}");
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
            if (!PlanRow(row).Contains(x, y))
                continue;

            int idx = start + row;
            if (idx >= plans.Count)
            {
                Message = "삭제할 생산 계획이 없습니다.";
                Game1.playSound("cancel");
                return;
            }

            bool ok = Mod.Production.TryRemovePlan(plans[idx].Id, out string result);
            Message = string.IsNullOrWhiteSpace(result) ? (ok ? "생산 계획을 삭제했습니다." : "생산 계획을 삭제하지 못했습니다.") : result;
            Game1.playSound(ok ? "trashcan" : "cancel");
            return;
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int max = Math.Max(0, (Mod.Production.GetPlans().Count - 1) / 5);
        if (direction < 0 && PlanPage < max)
        {
            PlanPage++;
            Message = $"생산 계획 페이지 {PlanPage + 1}/{max + 1}";
        }
        else if (direction > 0 && PlanPage > 0)
        {
            PlanPage--;
            Message = $"생산 계획 페이지 {PlanPage + 1}/{max + 1}";
        }
    }

    public override void draw(SpriteBatch b)
    {
        DrawImage(b);

        DrawHover(b, Company());
        DrawHover(b, Close());
        DrawHover(b, OneBatch());
        DrawHover(b, MaxBatch());
        DrawHover(b, Catalog());
        DrawHover(b, PlanAdd());

        for (int i = 0; i < 3; i++)
        {
            DrawHover(b, LineCard(i));
            if (i == SelectedLine)
                DrawSelected(b, LineCard(i));
        }

        for (int row = 0; row < 5; row++)
            DrawHover(b, PlanRow(row));

        DrawToast(b, Message);
        drawMouse(b);
    }

    private static string LineLabel(string? type) => type switch
    {
        "Fermentation" => "발효",
        "Packaging" => "포장",
        _ => "음료"
    };

    // Exact 1672x941 image-space hit boxes.
    private Rectangle Company() => H(38, 17, 238, 62);
    private Rectangle Close() => H(1554, 17, 70, 68);
    private Rectangle LineCard(int i) => H(38, 218 + i * 188, 432, 181);
    private Rectangle OneBatch() => H(488, 692, 204, 72);
    private Rectangle MaxBatch() => H(720, 692, 204, 72);
    private Rectangle Catalog() => H(960, 692, 216, 72);
    private Rectangle PlanAdd() => H(1220, 716, 388, 58);
    private Rectangle PlanRow(int row) => H(1220, 225 + row * 105, 392, 94);
}
