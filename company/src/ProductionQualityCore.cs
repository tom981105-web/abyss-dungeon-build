using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductionQualityCore
{
    private readonly ModEntry Mod;
    private readonly Dictionary<string, PendingReport> PendingReports = new(StringComparer.Ordinal);

    internal ProductionQualityCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    internal void EnsureState()
    {
        Mod.State.ProductionReports ??= new List<ProductionResultReport>();
        Mod.State.ProductionReports.RemoveAll(p => p is null || string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.RecipeKey));
        while (Mod.State.ProductionReports.Count > 8)
            Mod.State.ProductionReports.RemoveAt(Mod.State.ProductionReports.Count - 1);
        Mod.State.LifetimePlannedOutput = Math.Max(0, Mod.State.LifetimePlannedOutput);
        Mod.State.LifetimeActualOutput = Math.Max(0, Mod.State.LifetimeActualOutput);
    }

    internal ProductionForecast GetForecast(ProductionRecipeDefinition recipe, int batches = 1)
    {
        batches = Math.Clamp(batches, 1, 99);
        int input = Mod.Production.EstimateInputQualityScore(recipe);
        ProductionLineState? line = Mod.State.ProductionLines
            .FirstOrDefault(p => string.Equals(p.LineType, recipe.LineType, StringComparison.OrdinalIgnoreCase));
        int efficiency = Mod.Production.GetLineEfficiency(line);
        return BuildForecast(recipe, batches, input, efficiency);
    }

    internal ProductionForecast GetForecast(ProductionJob job)
    {
        ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(job.RecipeKey);
        if (recipe is null)
            return new ProductionForecast();

        if (job.ForecastQualityScore > 0 && job.EstimatedMinOutput > 0 && job.EstimatedMaxOutput > 0)
        {
            return new ProductionForecast
            {
                InputQualityScore = job.InputQualityScore,
                LineEfficiency = job.EfficiencyPercent,
                ProcessQualityScore = job.ProcessQualityScore,
                FinalQualityScore = job.ForecastQualityScore,
                ExpectedYieldPercent = job.ExpectedYieldPercent,
                MinOutput = job.EstimatedMinOutput,
                MaxOutput = job.EstimatedMaxOutput,
                SChance = job.SGradeChance,
                AChance = job.AGradeChance,
                BChance = job.BGradeChance,
                CChance = job.CGradeChance,
                MostLikelyGrade = MostLikelyGrade(job.SGradeChance, job.AGradeChance, job.BGradeChance, job.CGradeChance),
                BottleneckStage = FindBottleneck(recipe)
            };
        }

        return BuildForecast(recipe, Math.Max(1, job.BatchCount), Math.Max(1, job.InputQualityScore), Math.Max(80, job.EfficiencyPercent));
    }

    internal ProductionResultReport? GetLatestReport()
    {
        EnsureState();
        return Mod.State.ProductionReports.FirstOrDefault();
    }

    internal int GetLifetimeYieldPercent()
    {
        if (Mod.State.LifetimePlannedOutput <= 0)
            return 0;
        return (int)Math.Clamp(Math.Round(Mod.State.LifetimeActualOutput * 100d / Mod.State.LifetimePlannedOutput), 0, 999);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !e.IsMultipleOf(15))
            return;

        EnsureState();
        bool stateChanged = false;
        HashSet<string> activeIds = Mod.State.ProductionQueue
            .Where(p => p is not null)
            .Select(p => p.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (ProductionJob job in Mod.State.ProductionQueue.Where(p => p is not null))
        {
            ProductionRecipeDefinition? recipe = Mod.Production.FindRecipe(job.RecipeKey);
            if (recipe is null)
                continue;

            if (PrepareJob(job, recipe))
                stateChanged = true;

            PendingReports[job.Id] = new PendingReport
            {
                JobId = job.Id,
                RecipeKey = recipe.Key,
                ProductName = recipe.DisplayName,
                LineName = Mod.Production.GetLine(job.LineId)?.DisplayName ?? "생산라인",
                BatchCount = Math.Max(1, job.BatchCount),
                InputQualityScore = job.InputQualityScore,
                LineEfficiency = job.EfficiencyPercent,
                ProcessQualityScore = job.ProcessQualityScore,
                FinalQualityScore = job.ForecastQualityScore,
                ExpectedYieldPercent = job.ExpectedYieldPercent,
                PlannedOutput = Math.Max(1, recipe.OutputQuantity) * Math.Max(1, job.BatchCount),
                ActualOutput = Math.Max(1, job.EstimatedOutputQuantity),
                Grade = job.OutputGrade,
                BottleneckStage = FindBottleneck(recipe),
                WasNearCompletion = job.RemainingMinutes <= 10 || (job.CurrentStageIndex >= recipe.Stages.Count - 1 && job.StageRemainingMinutes <= 10)
            };
        }

        foreach (string jobId in PendingReports.Keys.Where(p => !activeIds.Contains(p)).ToList())
        {
            PendingReport pending = PendingReports[jobId];
            PendingReports.Remove(jobId);
            if (!pending.WasNearCompletion)
                continue;

            RecordCompleted(pending);
            stateChanged = true;
        }

        if (stateChanged)
            Mod.Multiplayer.BroadcastState();
    }

    private bool PrepareJob(ProductionJob job, ProductionRecipeDefinition recipe)
    {
        bool changed = false;
        ProductionForecast forecast = BuildForecast(recipe, Math.Max(1, job.BatchCount), Math.Max(1, job.InputQualityScore), Math.Max(80, job.EfficiencyPercent));

        if (job.ResultSeed == 0)
        {
            job.ResultSeed = StableHash(job.Id);
            changed = true;
        }

        if (job.ProcessQualityScore != forecast.ProcessQualityScore) { job.ProcessQualityScore = forecast.ProcessQualityScore; changed = true; }
        if (job.ForecastQualityScore != forecast.FinalQualityScore) { job.ForecastQualityScore = forecast.FinalQualityScore; changed = true; }
        if (job.ExpectedYieldPercent != forecast.ExpectedYieldPercent) { job.ExpectedYieldPercent = forecast.ExpectedYieldPercent; changed = true; }
        if (job.EstimatedMinOutput != forecast.MinOutput) { job.EstimatedMinOutput = forecast.MinOutput; changed = true; }
        if (job.EstimatedMaxOutput != forecast.MaxOutput) { job.EstimatedMaxOutput = forecast.MaxOutput; changed = true; }
        if (job.SGradeChance != forecast.SChance) { job.SGradeChance = forecast.SChance; changed = true; }
        if (job.AGradeChance != forecast.AChance) { job.AGradeChance = forecast.AChance; changed = true; }
        if (job.BGradeChance != forecast.BChance) { job.BGradeChance = forecast.BChance; changed = true; }
        if (job.CGradeChance != forecast.CChance) { job.CGradeChance = forecast.CChance; changed = true; }

        Random result = new(job.ResultSeed);
        int gradeRoll = result.Next(100);
        string actualGrade = RollGrade(gradeRoll, forecast);
        int actualOutput = forecast.MinOutput >= forecast.MaxOutput
            ? forecast.MinOutput
            : result.Next(forecast.MinOutput, forecast.MaxOutput + 1);

        if (!string.Equals(job.OutputGrade, actualGrade, StringComparison.OrdinalIgnoreCase))
        {
            job.OutputGrade = actualGrade;
            changed = true;
        }

        int actualQuality = ProductionCore.QualityFromGrade(actualGrade);
        if (job.OutputQuality != actualQuality) { job.OutputQuality = actualQuality; changed = true; }
        actualOutput = Math.Max(1, actualOutput);
        if (job.EstimatedOutputQuantity != actualOutput) { job.EstimatedOutputQuantity = actualOutput; changed = true; }
        return changed;
    }

    private void RecordCompleted(PendingReport pending)
    {
        int actualYield = pending.PlannedOutput <= 0
            ? 100
            : (int)Math.Clamp(Math.Round(pending.ActualOutput * 100d / pending.PlannedOutput), 0, 999);

        ProductionResultReport report = new()
        {
            RecipeKey = pending.RecipeKey,
            ProductName = pending.ProductName,
            LineName = pending.LineName,
            BatchCount = pending.BatchCount,
            InputQualityScore = pending.InputQualityScore,
            LineEfficiency = pending.LineEfficiency,
            ProcessQualityScore = pending.ProcessQualityScore,
            FinalQualityScore = pending.FinalQualityScore,
            ExpectedYieldPercent = pending.ExpectedYieldPercent,
            ActualYieldPercent = actualYield,
            PlannedOutput = pending.PlannedOutput,
            ActualOutput = pending.ActualOutput,
            Grade = pending.Grade,
            BottleneckStage = pending.BottleneckStage,
            CompletedDayNumber = ContractCore.GetCurrentDayNumber()
        };

        Mod.State.ProductionReports.Insert(0, report);
        while (Mod.State.ProductionReports.Count > 8)
            Mod.State.ProductionReports.RemoveAt(Mod.State.ProductionReports.Count - 1);

        Mod.State.LifetimePlannedOutput += pending.PlannedOutput;
        Mod.State.LifetimeActualOutput += pending.ActualOutput;

        string notice = $"생산 분석: {pending.ProductName} {pending.Grade}급 · 수율 {actualYield}% · 품질 {pending.FinalQualityScore}점";
        Game1.addHUDMessage(new HUDMessage(notice));
        Mod.Multiplayer.BroadcastNotice(notice);
    }

    private ProductionForecast BuildForecast(ProductionRecipeDefinition recipe, int batches, int inputScore, int efficiency)
    {
        int processScore = GetProcessQualityScore(recipe);
        int batchPressure = Math.Min(7, Math.Max(0, batches - 1) / 4);
        int finalScore = (int)Math.Clamp(Math.Round(inputScore * 0.55 + efficiency * 0.25 + processScore * 0.20 - batchPressure), 0, 100);
        int expectedYield = (int)Math.Clamp(Math.Round(86 + (efficiency - 80) * 0.55 + (inputScore - 55) * 0.08 + (processScore - 80) * 0.10 - batchPressure * 0.35), 82, 100);

        int baseOutput = Math.Max(1, recipe.OutputQuantity) * Math.Max(1, batches);
        int minYield = Math.Max(80, expectedYield - 3);
        int maxYield = Math.Min(103, expectedYield + 3);
        int minOutput = Math.Max(1, (int)Math.Round(baseOutput * minYield / 100d));
        int maxOutput = Math.Max(minOutput, (int)Math.Round(baseOutput * maxYield / 100d));

        (int s, int a, int b, int c) = CalculateGradeProbabilities(finalScore);
        return new ProductionForecast
        {
            InputQualityScore = inputScore,
            LineEfficiency = efficiency,
            ProcessQualityScore = processScore,
            FinalQualityScore = finalScore,
            ExpectedYieldPercent = expectedYield,
            MinOutput = minOutput,
            MaxOutput = maxOutput,
            SChance = s,
            AChance = a,
            BChance = b,
            CChance = c,
            MostLikelyGrade = MostLikelyGrade(s, a, b, c),
            BottleneckStage = FindBottleneck(recipe)
        };
    }

    private static (int S, int A, int B, int C) CalculateGradeProbabilities(int center)
    {
        int s = 0, a = 0, b = 0, c = 0, total = 0;
        for (int offset = -10; offset <= 10; offset++)
        {
            int weight = 11 - Math.Abs(offset);
            total += weight;
            switch (ProductionCore.GradeFromScore(Math.Clamp(center + offset, 0, 100)))
            {
                case "S": s += weight; break;
                case "A": a += weight; break;
                case "B": b += weight; break;
                default: c += weight; break;
            }
        }

        int sp = (int)Math.Round(s * 100d / total);
        int ap = (int)Math.Round(a * 100d / total);
        int bp = (int)Math.Round(b * 100d / total);
        int cp = Math.Max(0, 100 - sp - ap - bp);
        return (sp, ap, bp, cp);
    }

    private static int GetProcessQualityScore(ProductionRecipeDefinition recipe)
    {
        if (recipe.Stages is null || recipe.Stages.Count == 0)
            return 88;
        return (int)Math.Round(recipe.Stages.Average(StageQuality));
    }

    private static string FindBottleneck(ProductionRecipeDefinition recipe)
    {
        if (recipe.Stages is null || recipe.Stages.Count == 0)
            return "가공";
        ProductionStageDefinition bottleneck = recipe.Stages.OrderBy(StageQuality).First();
        return bottleneck.DisplayName;
    }

    private static int StageQuality(ProductionStageDefinition stage)
    {
        string text = $"{stage.Key} {stage.DisplayName}".ToLowerInvariant();
        if (text.Contains("세척") || text.Contains("wash")) return 95;
        if (text.Contains("선별") || text.Contains("select")) return 94;
        if (text.Contains("살균") || text.Contains("steril")) return 93;
        if (text.Contains("병입") || text.Contains("bott")) return 91;
        if (text.Contains("포장") || text.Contains("pack")) return 92;
        if (text.Contains("착즙") || text.Contains("juice")) return 88;
        if (text.Contains("파쇄") || text.Contains("crush")) return 87;
        if (text.Contains("염장") || text.Contains("salt")) return 85;
        if (text.Contains("숙성") || text.Contains("ferment") || text.Contains("aging")) return 83;
        return 90;
    }

    private static string RollGrade(int roll, ProductionForecast forecast)
    {
        if (roll < forecast.SChance) return "S";
        roll -= forecast.SChance;
        if (roll < forecast.AChance) return "A";
        roll -= forecast.AChance;
        if (roll < forecast.BChance) return "B";
        return "C";
    }

    private static string MostLikelyGrade(int s, int a, int b, int c)
    {
        (string Grade, int Chance)[] values = { ("S", s), ("A", a), ("B", b), ("C", c) };
        return values.OrderByDescending(p => p.Chance).First().Grade;
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = 17;
            foreach (char ch in text ?? "")
                hash = hash * 31 + ch;
            hash &= 0x7fffffff;
            return hash == 0 ? 1 : hash;
        }
    }

    private sealed class PendingReport
    {
        public string JobId { get; set; } = "";
        public string RecipeKey { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string LineName { get; set; } = "";
        public int BatchCount { get; set; }
        public int InputQualityScore { get; set; }
        public int LineEfficiency { get; set; }
        public int ProcessQualityScore { get; set; }
        public int FinalQualityScore { get; set; }
        public int ExpectedYieldPercent { get; set; }
        public int PlannedOutput { get; set; }
        public int ActualOutput { get; set; }
        public string Grade { get; set; } = "C";
        public string BottleneckStage { get; set; } = "";
        public bool WasNearCompletion { get; set; }
    }
}