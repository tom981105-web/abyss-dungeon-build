using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed partial class Production084Menu : Company084MenuBase
{
    private void Plans(SpriteBatch b)
    {
        Paper(b, D(985, 185, 391, 468), Cream2); Plaque(b, D(1008, 183, 345, 40), "생산 계획", 0.74f);
        List<ProductionPlanEntry> plans = Mod.Production.GetPlans().ToList();
        int start = PlanPage * 5;
        for (int row = 0; row < 5; row++)
        {
            Rectangle r = PlanRow(row); Paper(b, r);
            Rectangle number = new(r.X, r.Y, S(48), r.Height); Fill(b, number, GreenDeep); Text(b, Game1.dialogueFont, (start + row + 1).ToString(), number, Color.White, 0.72f, true);
            int idx = start + row;
            if (idx >= plans.Count) { Text(b, Game1.smallFont, "빈 계획", new Rectangle(r.X + S(70), r.Y, S(170), r.Height), Muted, 0.79f); continue; }
            ProductionPlanEntry plan = plans[idx]; ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(plan.RecipeKey);
            if (recipe is not null) Mod.Icons.DrawRecipeIcon(b, recipe, new Rectangle(r.X + S(61), r.Y + S(10), S(44), S(44)));
            Text(b, Game1.smallFont, $"{recipe?.DisplayName ?? plan.RecipeKey} × {plan.BatchCount}", new Rectangle(r.X + S(114), r.Y + S(8), S(184), S(45)), Ink, 0.78f);
            ArrowButton(b, PlanUp(row), true); ArrowButton(b, PlanDown(row), false);
            bool running = Mod.State.ProductionQueue.Any(p => string.Equals(p.RecipeKey, plan.RecipeKey, StringComparison.OrdinalIgnoreCase));
            StatusDot(b, new Rectangle(r.Right - S(26), r.Y + S(25), S(13), S(13)), running ? GreenBright : Blue);
            Fill(b, PlanRemove(row), Red); Text(b, Game1.smallFont, "×", PlanRemove(row), Color.White, 0.55f, true);
        }
        WoodButton(b, D(1006, 599, 349, 43), "+  계획 추가", false);
        Text(b, Game1.smallFont, $"빈 라인 자동 배정  ✓      {PlanPage + 1}/{Math.Max(1, (plans.Count + 4) / 5)}", D(1015, 643, 330, 18), Ink, 0.65f, true);
    }

    private void Bottom(SpriteBatch b)
    {
        BottomPanel(b, D(24, 678, 660, 124), "중간재", false);
        BottomPanel(b, D(698, 678, 678, 124), "완제품", true);
    }

    private void BottomPanel(SpriteBatch b, Rectangle r, string title, bool finished)
    {
        Paper(b, r); Fill(b, new Rectangle(r.X, r.Y, r.Width, S(31)), WoodDeep); Fill(b, new Rectangle(r.X + S(4), r.Y + S(4), r.Width - S(8), S(23)), Wood);
        Text(b, Game1.dialogueFont, title, new Rectangle(r.X, r.Y, r.Width, S(31)), new Color(248, 220, 147), 0.62f, true);
        if (!finished)
        {
            List<IntermediateStockEntry> rows = Mod.Production.GetIntermediateStock().Where(p => p.Quantity > 0).Take(3).ToList();
            if (rows.Count == 0) { Text(b, Game1.smallFont, "보유 중간재가 없습니다.", new Rectangle(r.X + S(30), r.Y + S(50), r.Width - S(60), S(34)), Muted, 0.78f); return; }
            for (int i = 0; i < rows.Count; i++)
            {
                int y = r.Y + S(36 + i * 27); var row = rows[i]; Mod.Icons.DrawProductIcon(b, row.Key, new Rectangle(r.X + S(22), y, S(23), S(23)));
                Text(b, Game1.smallFont, row.DisplayName, new Rectangle(r.X + S(56), y - S(2), S(245), S(25)), Ink, 0.72f); Dots(b, new Rectangle(r.X + S(305), y + S(11), S(235), S(2))); Text(b, Game1.smallFont, row.Quantity.ToString("N0"), new Rectangle(r.Right - S(100), y - S(2), S(70), S(25)), Ink, 0.74f, true);
            }
        }
        else
        {
            List<ProductStockEntry> rows = Mod.State.FinishedGoods.Values.Where(p => p is not null && p.Quantity > 0).OrderByDescending(p => p.Quality).ThenByDescending(p => p.Quantity).Take(3).ToList();
            if (rows.Count == 0) { Text(b, Game1.smallFont, "보유 완제품이 없습니다.", new Rectangle(r.X + S(30), r.Y + S(50), r.Width - S(60), S(34)), Muted, 0.78f); return; }
            for (int i = 0; i < rows.Count; i++)
            {
                int y = r.Y + S(36 + i * 27); var row = rows[i]; Mod.Icons.DrawProductIcon(b, row.ProductKey, new Rectangle(r.X + S(22), y, S(23), S(23)));
                string name = Mod.Production.FindRecipe(row.ProductKey)?.DisplayName ?? row.ProductKey; Text(b, Game1.smallFont, name, new Rectangle(r.X + S(56), y - S(2), S(250), S(25)), Ink, 0.72f); Dots(b, new Rectangle(r.X + S(306), y + S(11), S(140), S(2))); Grade(b, new Rectangle(r.X + S(457), y - S(1), S(58), S(24)), row.Grade); Text(b, Game1.smallFont, row.Quantity.ToString("N0"), new Rectangle(r.Right - S(100), y - S(2), S(70), S(25)), Ink, 0.74f, true);
            }
        }
    }

    private void MessageBar(SpriteBatch b)
    {
        if (string.IsNullOrWhiteSpace(Message)) return;
        Rectangle r = D(372, 803, 656, 14); Text(b, Game1.smallFont, Message, r, new Color(93, 65, 37), 0.56f, true);
    }

    private void Metric(SpriteBatch b, string label, string value, int y)
    {
        Text(b, Game1.smallFont, label, D(446, y, 120, 24), Ink, 0.72f); Dots(b, D(568, y + 12, 120, 2)); Text(b, Game1.smallFont, value, D(695, y, 215, 24), Ink, 0.76f);
    }

    private void GradeChance(SpriteBatch b, string g, int chance, int x, int y, Color c)
    {
        Star(b, new Rectangle(x, y, S(20), S(20)), c); Text(b, Game1.smallFont, $"{g}급 {chance}%", new Rectangle(x + S(27), y - S(1), S(54), S(22)), Ink, 0.61f);
    }

    private void ArrowButton(SpriteBatch b, Rectangle r, bool up)
    {
        Paper(b, r, new Color(238, 211, 163)); int cx = r.X + r.Width / 2; int cy = r.Y + r.Height / 2; Color c = new(107, 75, 40);
        Fill(b, new Rectangle(cx - S(2), up ? cy - S(2) : cy - S(7), S(4), S(9)), c);
        if (up) TriangleUp(b, new Rectangle(cx - S(7), cy - S(9), S(14), S(8)), c); else TriangleDown(b, new Rectangle(cx - S(7), cy + S(1), S(14), S(8)), c);
    }

    private void TriangleUp(SpriteBatch b, Rectangle r, Color c)
    {
        for (int i = 0; i < Math.Max(1, r.Height); i++) { int w = Math.Max(1, (int)(r.Width * ((i + 1f) / r.Height))); Fill(b, new Rectangle(r.X + (r.Width - w) / 2, r.Bottom - 1 - i, w, 1), c); }
    }

    private void StatusDot(SpriteBatch b, Rectangle r, Color c)
    {
        Fill(b, new Rectangle(r.X + r.Width / 3, r.Y, r.Width / 3, r.Height), c); Fill(b, new Rectangle(r.X, r.Y + r.Height / 3, r.Width, r.Height / 3), c); Fill(b, Inset(r, S(2)), c);
    }

    private void Clock(SpriteBatch b, Rectangle r)
    {
        Fill(b, r, WoodDeep); Fill(b, Inset(r, S(3)), Cream); int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2; Fill(b, new Rectangle(cx, r.Y + S(5), S(2), cy - r.Y - S(4)), Ink); Fill(b, new Rectangle(cx, cy, S(6), S(2)), Ink);
    }

    private void Sun(SpriteBatch b, Rectangle r)
    {
        Fill(b, Inset(r, S(10)), GoldLight); Fill(b, new Rectangle(r.X + r.Width / 2 - S(2), r.Y, S(4), S(9)), GoldLight); Fill(b, new Rectangle(r.X + r.Width / 2 - S(2), r.Bottom - S(9), S(4), S(9)), GoldLight); Fill(b, new Rectangle(r.X, r.Y + r.Height / 2 - S(2), S(9), S(4)), GoldLight); Fill(b, new Rectangle(r.Right - S(9), r.Y + r.Height / 2 - S(2), S(9), S(4)), GoldLight);
    }

    private void Leaf(SpriteBatch b, Rectangle r)
    {
        Fill(b, new Rectangle(r.X, r.Y + S(7), r.Width / 2, r.Height / 2), new Color(67, 131, 43)); Fill(b, new Rectangle(r.X + r.Width / 3, r.Y + r.Height / 2, r.Width / 2, r.Height / 2), GreenBright); Fill(b, new Rectangle(r.X + r.Width / 2 - S(2), r.Y + S(6), S(4), r.Height - S(5)), GreenDeep);
    }

    private Rectangle Company() => D(18, 16, 245, 62);
    private Rectangle Close() => D(1331, 17, 50, 50);
    private Rectangle LineCard(int i) => D(39, 229 + i * 126, 330, 118);
    private Rectangle OneBatch() => D(427, 592, 175, 48);
    private Rectangle MaxBatch() => D(616, 592, 175, 48);
    private Rectangle Catalog() => D(805, 592, 139, 48);
    private Rectangle PlanRow(int row) => D(1001, 229 + row * 70, 358, 62);
    private Rectangle PlanUp(int row) => D(1284, 234 + row * 70, 34, 25);
    private Rectangle PlanDown(int row) => D(1284, 261 + row * 70, 34, 25);
    private Rectangle PlanRemove(int row) => D(1328, 270 + row * 70, 20, 20);
}
