using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>
/// 0.9.2 production screen: the approved reference PNG is rendered verbatim.
/// Interaction is provided by invisible image-space hit boxes only.
/// </summary>
internal sealed class Production092Menu : ImageBackedUi092Base
{
    private string SelectedRecipeKey = "";
    private int PlanPage;

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
        for (int i = 0; i < Math.Min(3, lines.Count); i++)
        {
            if (!LineCard(i).Contains(x, y))
                continue;

            ProductionJob? job = Mod.Production.GetLineJob(lines[i].Id);
            ProductionRecipeDefinition? recipe = job is null
                ? Mod.Recipes.FirstOrDefault(p => string.Equals(p.LineType, lines[i].LineType, StringComparison.OrdinalIgnoreCase))
                : Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is not null)
                SelectedRecipeKey = recipe.Key;
            Game1.playSound("smallSelect");
            return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedRecipeKey);
        if (selected is not null && OneBatch().Contains(x, y))
        {
            bool ok = Mod.Production.TryStart(selected.Key, 1, out _);
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }

        if (selected is not null && MaxBatch().Contains(x, y))
        {
            int max = Mod.Production.GetMaxBatches(selected);
            if (max <= 0)
            {
                Game1.playSound("cancel");
                return;
            }
            bool ok = Mod.Production.TryStart(selected.Key, Math.Min(10, max), out _);
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }

        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            int idx = start + row;
            if (idx >= plans.Count || !PlanRow(row).Contains(x, y))
                continue;
            SelectedRecipeKey = plans[idx].RecipeKey;
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
            int idx = start + row;
            if (idx >= plans.Count || !PlanRow(row).Contains(x, y))
                continue;
            bool ok = Mod.Production.TryRemovePlan(plans[idx].Id, out _);
            Game1.playSound(ok ? "trashcan" : "cancel");
            return;
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
        DrawImage(b);
        drawMouse(b);
    }

    // Hit boxes are authored in the exact 1672x941 PNG coordinate system.
    private Rectangle Company() => H(34, 14, 250, 63);
    private Rectangle Close() => H(1560, 18, 68, 66);
    private Rectangle LineCard(int i) => H(31, 216 + i * 190, 432, 184);
    private Rectangle OneBatch() => H(480, 690, 204, 74);
    private Rectangle MaxBatch() => H(708, 690, 204, 74);
    private Rectangle Catalog() => H(944, 690, 218, 74);
    private Rectangle PlanAdd() => H(1224, 704, 389, 67);
    private Rectangle PlanRow(int row) => H(1220, 225 + row * 105, 394, 96);
}
