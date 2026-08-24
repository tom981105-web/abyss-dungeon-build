using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>
/// 0.9.2 product catalog: the approved catalog PNG is rendered verbatim and all controls
/// are invisible hit boxes mapped to image coordinates.
/// </summary>
internal sealed class ProductCatalog092Menu : ImageBackedUi092Base
{
    private int Page;
    private int Filter;
    private string SelectedKey;

    internal ProductCatalog092Menu(ModEntry mod, string selectedKey = "")
        : base(mod, "assets/ui_092_catalog.png")
    {
        Mod.Production.EnsureState();
        SelectedKey = selectedKey;
    }

    private List<ProductionRecipeDefinition> Rows()
    {
        IEnumerable<ProductionRecipeDefinition> q = Mod.Production.GetCatalogRecipes(true);
        if (Filter == 1)
            q = q.Where(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        else if (Filter == 2)
            q = q.Where(p => !string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase));
        return q.ToList();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (Close().Contains(x, y) || Back().Contains(x, y))
        {
            Game1.playSound("bigDeSelect");
            Game1.activeClickableMenu = new Production092Menu(Mod);
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            if (!FilterButton(i).Contains(x, y))
                continue;
            Filter = i;
            Page = 0;
            Game1.playSound("smallSelect");
            return;
        }

        List<ProductionRecipeDefinition> rows = Rows();
        int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            int idx = start + i;
            if (idx >= rows.Count || !RecipeCard(i).Contains(x, y))
                continue;
            SelectedKey = rows[idx].Key;
            Game1.playSound("smallSelect");
            return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedKey) ?? rows.Skip(start).FirstOrDefault();
        if (selected is not null && OneBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out _))
            {
                Game1.playSound("cancel");
                return;
            }
            bool ok = Mod.Production.TryStart(selected.Key, 1, out _);
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }

        if (selected is not null && MaxBatch().Contains(x, y))
        {
            if (!Mod.Production.IsRecipeUnlocked(selected, out _))
            {
                Game1.playSound("cancel");
                return;
            }
            int max = Math.Min(10, Mod.Production.GetMaxBatches(selected));
            if (max <= 0)
            {
                Game1.playSound("cancel");
                return;
            }
            bool ok = Mod.Production.TryStart(selected.Key, max, out _);
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }

        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (Prev().Contains(x, y) && Page > 0)
        {
            Page--;
            Game1.playSound("shwip");
            return;
        }
        if (Next().Contains(x, y) && Page < maxPage)
        {
            Page++;
            Game1.playSound("shwip");
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int max = Math.Max(0, (rows.Count - 1) / 6);
        if (direction < 0 && Page < max) Page++;
        else if (direction > 0 && Page > 0) Page--;
    }

    public override void draw(SpriteBatch b)
    {
        DrawImage(b);
        drawMouse(b);
    }

    private Rectangle Close() => H(1561, 12, 69, 69);
    private Rectangle Back() => H(38, 845, 250, 70);
    private Rectangle FilterButton(int i) => i switch
    {
        0 => H(48, 92, 276, 65),
        1 => H(337, 92, 259, 65),
        _ => H(608, 92, 259, 65)
    };

    private Rectangle RecipeCard(int i)
    {
        int col = i % 2;
        int row = i / 2;
        return H(47 + col * 475, 211 + row * 207, 440, 190);
    }

    private Rectangle OneBatch() => H(1048, 760, 255, 73);
    private Rectangle MaxBatch() => H(1316, 760, 255, 73);
    private Rectangle Prev() => H(38, 845, 250, 70);
    private Rectangle Next() => H(1384, 845, 244, 70);
}
