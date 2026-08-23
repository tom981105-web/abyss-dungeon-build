using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed partial class Production084Menu : Company084MenuBase
{
    private string SelectedRecipeKey = "";
    private int PlanPage;
    private string Message = "생산 계획을 등록하면 빈 라인에 자동으로 배정됩니다.";

    internal Production084Menu(ModEntry mod) : base(mod)
    {
        Mod.Production.EnsureState();
        SelectedRecipeKey = Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, "TomatoJuice", StringComparison.OrdinalIgnoreCase))?.Key
            ?? Mod.Recipes.FirstOrDefault(p => !p.RequiresCropGenetics)?.Key
            ?? Mod.Recipes.FirstOrDefault()?.Key ?? "";
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y)) { Game1.playSound("bigDeSelect"); exitThisMenu(); return; }
        if (Company().Contains(x, y)) { Game1.playSound("bigDeSelect"); Game1.activeClickableMenu = new CompanyMenu(Mod); return; }
        if (Catalog().Contains(x, y)) { Game1.playSound("bigSelect"); Game1.activeClickableMenu = new ProductCatalog084Menu(Mod, SelectedRecipeKey); return; }

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
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.66f);
        Frame(b); Header(b); Stats(b); Lines(b); Current(b); Plans(b); Bottom(b); MessageBar(b); drawMouse(b);
    }
}
