using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AgriculturalCompany;

/// <summary>
/// Image-backed catalog. The authored PNG remains intact while 0.9.3 adds
/// hover, selection, page/filter feedback and visible action results.
/// </summary>
internal sealed class ProductCatalog092Menu : ImageBackedUi092Base
{
    private int Page;
    private int Filter;
    private string SelectedKey;
    private int SelectedCard = 1;
    private string Message = "제품 카드나 버튼 위에 마우스를 올리면 클릭 가능한 영역이 표시됩니다.";

    internal ProductCatalog092Menu(ModEntry mod, string selectedKey = "")
        : base(mod, "assets/ui_092_catalog.png")
    {
        Mod.Production.EnsureState();
        SelectedKey = selectedKey;
        SyncSelectedCard();
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
            List<ProductionRecipeDefinition> filtered = Rows();
            if (filtered.Count > 0)
                SelectedKey = filtered[0].Key;
            SelectedCard = filtered.Count > 0 ? 0 : -1;
            Message = $"{FilterName(i)} 필터 선택 — {filtered.Count}개 레시피";
            Game1.playSound("smallSelect");
            return;
        }

        List<ProductionRecipeDefinition> rows = Rows();
        int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            if (!RecipeCard(i).Contains(x, y))
                continue;

            int idx = start + i;
            if (idx >= rows.Count)
            {
                Message = "이 카드에는 제품이 없습니다.";
                Game1.playSound("cancel");
                return;
            }

            SelectedKey = rows[idx].Key;
            SelectedCard = i;
            Message = $"{rows[idx].DisplayName} 선택";
            Game1.playSound("smallSelect");
            return;
        }

        ProductionRecipeDefinition? selected = Mod.Production.FindRecipe(SelectedKey) ?? rows.Skip(start).FirstOrDefault();
        if (OneBatch().Contains(x, y))
        {
            if (selected is null)
            {
                Message = "생산할 제품을 먼저 선택해 주세요.";
                Game1.playSound("cancel");
                return;
            }
            if (!Mod.Production.IsRecipeUnlocked(selected, out string reason))
            {
                Message = string.IsNullOrWhiteSpace(reason) ? "아직 해금되지 않은 제품입니다." : reason;
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
            if (!Mod.Production.IsRecipeUnlocked(selected, out string reason))
            {
                Message = string.IsNullOrWhiteSpace(reason) ? "아직 해금되지 않은 제품입니다." : reason;
                Game1.playSound("cancel");
                return;
            }

            int max = Math.Min(10, Mod.Production.GetMaxBatches(selected));
            if (max <= 0)
            {
                Message = $"{Mod.Production.GetIngredientDisplayName(selected)} 재고가 부족합니다.";
                Game1.playSound("cancel");
                return;
            }

            bool ok = Mod.Production.TryStart(selected.Key, max, out string result);
            Message = string.IsNullOrWhiteSpace(result) ? (ok ? $"{selected.DisplayName} {max}배치 생산을 시작했습니다." : "최대 생산을 시작하지 못했습니다.") : result;
            Game1.playSound(ok ? "Ship" : "cancel");
            return;
        }

        int maxPage = Math.Max(0, (rows.Count - 1) / 6);
        if (Prev().Contains(x, y))
        {
            if (Page <= 0)
            {
                Message = "첫 페이지입니다.";
                Game1.playSound("cancel");
                return;
            }
            Page--;
            SelectFirstOnPage();
            Message = $"이전 페이지 — {Page + 1}/{maxPage + 1}";
            Game1.playSound("shwip");
            return;
        }

        if (Next().Contains(x, y))
        {
            if (Page >= maxPage)
            {
                Message = "마지막 페이지입니다.";
                Game1.playSound("cancel");
                return;
            }
            Page++;
            SelectFirstOnPage();
            Message = $"다음 페이지 — {Page + 1}/{maxPage + 1}";
            Game1.playSound("shwip");
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int max = Math.Max(0, (rows.Count - 1) / 6);
        if (direction < 0 && Page < max)
        {
            Page++;
            SelectFirstOnPage();
            Message = $"페이지 {Page + 1}/{max + 1}";
        }
        else if (direction > 0 && Page > 0)
        {
            Page--;
            SelectFirstOnPage();
            Message = $"페이지 {Page + 1}/{max + 1}";
        }
    }

    public override void draw(SpriteBatch b)
    {
        DrawImage(b);

        DrawHover(b, Close());
        DrawHover(b, Back());
        DrawHover(b, Prev());
        DrawHover(b, Next());
        DrawHover(b, OneBatch());
        DrawHover(b, MaxBatch());

        for (int i = 0; i < 3; i++)
        {
            DrawHover(b, FilterButton(i));
            if (i == Filter)
                DrawSelected(b, FilterButton(i));
        }

        List<ProductionRecipeDefinition> rows = Rows();
        int start = Page * 6;
        for (int i = 0; i < 6; i++)
        {
            DrawHover(b, RecipeCard(i));
            if (i == SelectedCard && start + i < rows.Count)
                DrawSelected(b, RecipeCard(i));
        }

        DrawToast(b, Message);
        drawMouse(b);
    }

    private void SyncSelectedCard()
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int idx = rows.FindIndex(p => string.Equals(p.Key, SelectedKey, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            SelectedCard = rows.Count > 0 ? 0 : -1;
            if (rows.Count > 0)
                SelectedKey = rows[0].Key;
            return;
        }

        Page = idx / 6;
        SelectedCard = idx % 6;
    }

    private void SelectFirstOnPage()
    {
        List<ProductionRecipeDefinition> rows = Rows();
        int idx = Page * 6;
        if (idx < rows.Count)
        {
            SelectedKey = rows[idx].Key;
            SelectedCard = 0;
        }
        else
        {
            SelectedCard = -1;
        }
    }

    private static string FilterName(int i) => i switch
    {
        1 => "중간재",
        2 => "완제품",
        _ => "전체 제품"
    };

    private Rectangle Close() => H(1558, 16, 67, 67);
    private Rectangle Back() => H(42, 844, 252, 68);
    private Rectangle FilterButton(int i) => i switch
    {
        0 => H(52, 95, 272, 63),
        1 => H(338, 95, 258, 63),
        _ => H(608, 95, 258, 63)
    };

    private Rectangle RecipeCard(int i)
    {
        int col = i % 2;
        int row = i / 2;
        return H(52 + col * 472, 217 + row * 207, 442, 191);
    }

    private Rectangle OneBatch() => H(1040, 744, 245, 70);
    private Rectangle MaxBatch() => H(1304, 744, 248, 70);
    private Rectangle Prev() => H(42, 844, 252, 68);
    private Rectangle Next() => H(1380, 844, 248, 68);
}
