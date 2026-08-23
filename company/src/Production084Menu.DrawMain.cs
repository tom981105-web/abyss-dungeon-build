using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace AgriculturalCompany;

internal sealed partial class Production084Menu : Company084MenuBase
{
    private void Header(SpriteBatch b)
    {
        WoodButton(b, Company(), CompanyName(), false);
        Sun(b, D(34, 30, 34, 34));
        Plaque(b, D(360, 12, 680, 66), $"{CompanyName()} · 생산 관리 2.0", 1.02f);
        Leaf(b, D(330, 28, 26, 32)); Leaf(b, D(1044, 28, 26, 32));
        WoodButton(b, Close(), "×", false, new Color(196, 106, 55));
    }

    private void Stats(SpriteBatch b)
    {
        Stat(b, D(25, 98, 327, 70), 0, "회사 자금", $"{Mod.State.CompanyFunds:N0}G");
        Stat(b, D(364, 98, 327, 70), 1, "브랜드", Mod.Brand.GetTierName(Mod.State.BrandPoints));
        Stat(b, D(703, 98, 327, 70), 2, "활성 계약", $"{Mod.State.AcceptedContracts.Count}건");
        Stat(b, D(1042, 98, 333, 70), 3, "평판", Mod.State.Reputation.ToString("N0"));
    }

    private void Stat(SpriteBatch b, Rectangle r, int icon, string label, string value)
    {
        Paper(b, r);
        Rectangle ir = new(r.X + S(19), r.Y + S(13), S(44), S(44));
        if (icon == 0) Coin(b, ir); else if (icon == 1) Shield(b, ir); else if (icon == 2) Scroll(b, ir); else Heart(b, ir);
        Text(b, Game1.smallFont, label, new Rectangle(r.X + S(78), r.Y + S(8), r.Width - S(92), S(24)), Ink, 0.94f);
        Text(b, Game1.dialogueFont, value, new Rectangle(r.X + S(78), r.Y + S(29), r.Width - S(92), S(32)), Ink, 0.72f);
    }

    private void Lines(SpriteBatch b)
    {
        Paper(b, D(24, 185, 360, 468), Cream2); Plaque(b, D(43, 183, 322, 40), "생산 라인", 0.74f);
        IReadOnlyList<ProductionLineState> lines = Mod.Production.GetLines();
        for (int i = 0; i < 3; i++)
        {
            Rectangle card = LineCard(i); Paper(b, card);
            if (i >= lines.Count) { Text(b, Game1.smallFont, $"라인 {i + 1} · 잠김", new Rectangle(card.X + S(15), card.Y + S(8), card.Width - S(30), S(26)), Muted, 0.88f); continue; }
            ProductionLineState line = lines[i];
            ProductionJob? job = Mod.Production.GetLineJob(line.Id);
            ProductionRecipeDefinition? recipe = job is null ? null : Mod.Production.FindRecipe(job.RecipeKey);
            Text(b, Game1.smallFont, $"라인 {i + 1} · {LineName(line.LineType)}", new Rectangle(card.X + S(14), card.Y + S(7), S(210), S(25)), Ink, 0.92f);
            StatusPill(b, new Rectangle(card.Right - S(74), card.Y + S(7), S(61), S(26)), job is null ? "대기" : "가동 중", job is not null);
            Machine(b, new Rectangle(card.X + S(12), card.Y + S(37), S(112), S(77)), line.LineType, job is not null);
            if (recipe is not null) Mod.Icons.DrawRecipeIcon(b, recipe, new Rectangle(card.X + S(132), card.Y + S(39), S(45), S(45)));
            Text(b, Game1.smallFont, recipe?.DisplayName ?? "대기 중", new Rectangle(card.X + S(184), card.Y + S(38), card.Width - S(198), S(28)), Ink, 0.92f);
            string stage = job is null ? "작업 없음" : Mod.Production.GetCurrentStageName(job);
            Text(b, Game1.smallFont, $"현재 단계  {stage}", new Rectangle(card.X + S(132), card.Y + S(67), card.Width - S(146), S(24)), job is null ? Muted : GreenDeep, 0.77f);
            float p = job is null ? 0f : (float)Mod.Production.GetJobProgress(job);
            Progress(b, new Rectangle(card.X + S(132), card.Y + S(94), S(142), S(13)), p);
            Text(b, Game1.smallFont, $"{Math.Clamp((int)(p * 100), 0, 100)}%", new Rectangle(card.X + S(281), card.Y + S(88), S(48), S(24)), Ink, 0.75f, true);
            int eff = job?.EfficiencyPercent ?? Mod.Production.GetLineEfficiency(line);
            string remain = job is null ? "-" : ProductionCore.FormatDuration(job.RemainingMinutes);
            Clock(b, new Rectangle(card.X + S(132), card.Y + S(113), S(18), S(18)));
            Text(b, Game1.smallFont, remain, new Rectangle(card.X + S(155), card.Y + S(110), S(95), S(22)), Ink, 0.68f);
            Leaf(b, new Rectangle(card.X + S(250), card.Y + S(111), S(18), S(19)));
            Text(b, Game1.smallFont, $"효율 {eff}%", new Rectangle(card.X + S(274), card.Y + S(109), S(62), S(23)), Green, 0.66f);
        }
        WoodButton(b, D(42, 617, 324, 29), "⚙ 작업 배정", false);
    }

    private void Current(SpriteBatch b)
    {
        Paper(b, D(397, 185, 574, 468), Cream2); Plaque(b, D(421, 183, 526, 40), "현재 생산 상세", 0.74f);
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(SelectedRecipeKey) ?? Mod.Recipes.FirstOrDefault();
        if (recipe is null) return;
        ProductionJob? active = Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.RecipeKey, recipe.Key, StringComparison.OrdinalIgnoreCase));
        ProductionForecast forecast = active is null ? Mod.Quality.GetForecast(recipe, 1) : Mod.Quality.GetForecast(active);

        Mod.Icons.DrawRecipeIcon(b, recipe, D(593, 231, 57, 57));
        Text(b, Game1.dialogueFont, recipe.DisplayName, D(662, 232, 260, 42), Ink, 0.76f);
        Flow(b, recipe, active);

        Rectangle metrics = D(422, 455, 516, 125); Paper(b, metrics, new Color(247, 229, 186));
        Metric(b, "진행률", $"{Math.Clamp((int)((active is null ? 0f : (float)Mod.Production.GetJobProgress(active)) * 100), 0, 100)}%", 468);
        Metric(b, "예상 생산량", $"{forecast.MinOutput} ~ {forecast.MaxOutput}{recipe.OutputUnit}", 497);
        Metric(b, "예상 등급", forecast.MostLikelyGrade, 526);
        Metric(b, "예상 시간", ProductionCore.FormatDuration(active?.RemainingMinutes ?? recipe.DurationMinutes), 555);

        Rectangle q = D(742, 463, 179, 108); Paper(b, q);
        Text(b, Game1.smallFont, "품질 요약", new Rectangle(q.X, q.Y + S(2), q.Width, S(23)), Ink, 0.83f, true);
        GradeChance(b, "S", forecast.SChance, q.X + S(12), q.Y + S(29), Gold);
        GradeChance(b, "A", forecast.AChance, q.X + S(92), q.Y + S(29), GreenBright);
        GradeChance(b, "B", forecast.BChance, q.X + S(12), q.Y + S(66), Blue);
        GradeChance(b, "C", forecast.CChance, q.X + S(92), q.Y + S(66), new Color(188, 116, 57));

        WoodButton(b, OneBatch(), "+  1배치 추가", true);
        WoodButton(b, MaxBatch(), "⚙  최대 생산", false, Blue);
        WoodButton(b, Catalog(), "▦  제품 카탈로그", false);
    }

    private void Flow(SpriteBatch b, ProductionRecipeDefinition recipe, ProductionJob? active)
    {
        List<(string name, int idx)> nodes = new() { ("원재료", -1) };
        foreach (var stage in recipe.Stages.Take(4).Select((s, i) => (s.DisplayName, i))) nodes.Add(stage);
        nodes.Add(("완제품", 99));
        int n = nodes.Count; float total = 510f; float gap = 13f; float nodeW = (total - gap * (n - 1)) / n;
        for (int i = 0; i < n; i++)
        {
            int dx = 425 + (int)MathF.Round(i * (nodeW + gap));
            bool current = active is not null && nodes[i].idx >= 0 && nodes[i].idx < 99 && active.CurrentStageIndex == nodes[i].idx;
            Rectangle card = D(dx, 305, (int)nodeW, 132);
            if (current) { Fill(b, D(dx - 4, 301, (int)nodeW + 8, 140), Orange); Fill(b, D(dx, 305, (int)nodeW, 132), new Color(255, 226, 177)); }
            else Paper(b, card, Cream);
            Rectangle icon = D(dx + Math.Max(2, ((int)nodeW - 50) / 2), 322, 50, 55);
            if (nodes[i].idx == -1 || nodes[i].idx == 99) Mod.Icons.DrawRecipeIcon(b, recipe, icon);
            else ProcessIcon(b, icon, nodes[i].name, current);
            Text(b, Game1.smallFont, nodes[i].name, D(dx + 2, 385, (int)nodeW - 4, 35), current ? GreenDeep : Ink, 0.65f, true);
            if (i < n - 1) Arrow(b, D(dx + (int)nodeW + 1, 352, (int)gap + 9, 20));
        }
    }
}
