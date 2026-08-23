using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductionCore
{
    private readonly ModEntry Mod;
    private const int PlanLimit = 16;

    internal ProductionCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        Mod.Helper.Events.GameLoop.TimeChanged += OnTimeChanged;
    }

    internal void EnsureState()
    {
        Mod.State.ProductionQueue ??= new List<ProductionJob>();
        Mod.State.ProductionPlans ??= new List<ProductionPlanEntry>();
        Mod.State.ProductionLines ??= new List<ProductionLineState>();
        Mod.State.IntermediateStock ??= new Dictionary<string, IntermediateStockEntry>(StringComparer.OrdinalIgnoreCase);
        Mod.State.FinishedGoods ??= new Dictionary<string, ProductStockEntry>(StringComparer.OrdinalIgnoreCase);

        EnsureDefaultLine("line-beverage", "Beverage", "라인 1 · 음료 라인", 92);
        EnsureDefaultLine("line-packaging", "Packaging", "라인 2 · 포장 라인", 88);
        EnsureDefaultLine("line-fermentation", "Fermentation", "라인 3 · 발효 라인", 86);

        foreach (ProductionRecipeDefinition recipe in Mod.Recipes)
        {
            recipe.Stages ??= new List<ProductionStageDefinition>();
            if (recipe.Stages.Count == 0)
            {
                recipe.Stages.Add(new ProductionStageDefinition
                {
                    Key = "process",
                    DisplayName = "가공",
                    DurationMinutes = Math.Max(10, recipe.DurationMinutes)
                });
            }

            if (string.IsNullOrWhiteSpace(recipe.OutputKind))
                recipe.OutputKind = "Finished";
            if (string.IsNullOrWhiteSpace(recipe.ProductFamily))
                recipe.ProductFamily = "General";
            if (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(recipe.OutputIntermediateKey))
            {
                recipe.OutputIntermediateKey = recipe.Key;
            }
            if (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(recipe.OutputIntermediateDisplayName))
            {
                recipe.OutputIntermediateDisplayName = recipe.DisplayName;
            }
        }

        foreach (ProductStockEntry stock in Mod.State.FinishedGoods.Values.Where(p => p is not null))
        {
            if (string.IsNullOrWhiteSpace(stock.Grade))
                stock.Grade = GradeFromQuality(stock.Quality);
        }

        foreach (IntermediateStockEntry stock in Mod.State.IntermediateStock.Values.Where(p => p is not null))
        {
            if (string.IsNullOrWhiteSpace(stock.Grade))
                stock.Grade = GradeFromQuality(stock.Quality);
        }

        Mod.State.ProductionPlans.RemoveAll(p => p is null || string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.RecipeKey) || p.BatchCount <= 0);
        NormalizePriorities();
        MigrateLegacyJobs();
        CleanupIntermediate();
    }

    private void EnsureDefaultLine(string id, string type, string displayName, int efficiency)
    {
        ProductionLineState? line = Mod.State.ProductionLines.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        if (line is null)
        {
            Mod.State.ProductionLines.Add(new ProductionLineState
            {
                Id = id,
                LineType = type,
                DisplayName = displayName,
                BaseEfficiency = efficiency,
                Level = 1
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(line.LineType)) line.LineType = type;
        if (string.IsNullOrWhiteSpace(line.DisplayName)) line.DisplayName = displayName;
        if (line.BaseEfficiency <= 0) line.BaseEfficiency = efficiency;
        if (line.Level <= 0) line.Level = 1;
    }

    private void MigrateLegacyJobs()
    {
        HashSet<string> usedLines = Mod.State.ProductionQueue
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.LineId))
            .Select(p => p.LineId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (ProductionJob job in Mod.State.ProductionQueue.Where(p => p is not null))
        {
            ProductionRecipeDefinition? recipe = FindRecipe(job.RecipeKey);
            if (recipe is null)
                continue;

            if (string.IsNullOrWhiteSpace(job.LineId))
            {
                ProductionLineState? line = Mod.State.ProductionLines
                    .FirstOrDefault(p => !usedLines.Contains(p.Id) && string.Equals(p.LineType, recipe.LineType, StringComparison.OrdinalIgnoreCase))
                    ?? Mod.State.ProductionLines.FirstOrDefault(p => !usedLines.Contains(p.Id));
                if (line is not null)
                {
                    job.LineId = line.Id;
                    usedLines.Add(line.Id);
                }
            }

            job.CurrentStageIndex = Math.Clamp(job.CurrentStageIndex, 0, Math.Max(0, recipe.Stages.Count - 1));
            ProductionStageDefinition stage = recipe.Stages[job.CurrentStageIndex];
            int legacyRemaining = job.StageRemainingMinutes > 0 ? job.StageRemainingMinutes : Math.Max(10, job.RemainingMinutes);
            job.StageRemainingMinutes = legacyRemaining;
            job.StageTotalMinutes = job.StageTotalMinutes > 0 ? job.StageTotalMinutes : Math.Max(10, stage.DurationMinutes * Math.Max(1, job.BatchCount));
            job.TotalMinutes = job.TotalMinutes > 0 ? job.TotalMinutes : GetRecipeTotalMinutes(recipe, job.BatchCount);
            job.RemainingMinutes = job.RemainingMinutes > 0 ? job.RemainingMinutes : job.StageRemainingMinutes;
            if (job.EfficiencyPercent <= 0)
                job.EfficiencyPercent = GetLineEfficiency(GetLine(job.LineId));
            if (job.InputQualityScore <= 0)
                job.InputQualityScore = 55;
            if (string.IsNullOrWhiteSpace(job.OutputGrade))
                job.OutputGrade = GradeFromQuality(job.OutputQuality);
            if (job.EstimatedOutputQuantity <= 0)
                job.EstimatedOutputQuantity = EstimateOutputQuantity(recipe, job.BatchCount, job.EfficiencyPercent);
        }
    }

    internal ProductionRecipeDefinition? FindRecipe(string key)
        => Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));

    internal ProductionLineState? GetLine(string lineId)
        => Mod.State.ProductionLines.FirstOrDefault(p => string.Equals(p.Id, lineId, StringComparison.OrdinalIgnoreCase));

    internal ProductionJob? GetLineJob(string lineId)
        => Mod.State.ProductionQueue.FirstOrDefault(p => string.Equals(p.LineId, lineId, StringComparison.OrdinalIgnoreCase));

    internal IReadOnlyList<ProductionLineState> GetLines()
    {
        EnsureState();
        return Mod.State.ProductionLines.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal IReadOnlyList<ProductionPlanEntry> GetPlans()
    {
        EnsureState();
        return Mod.State.ProductionPlans.OrderBy(p => p.Priority).ThenBy(p => p.CreatedDayNumber).ToList();
    }

    internal IReadOnlyList<ProductionRecipeDefinition> GetCatalogRecipes(bool includeLocked = true)
    {
        EnsureState();
        IEnumerable<ProductionRecipeDefinition> query = Mod.Recipes;
        if (!includeLocked)
            query = query.Where(p => IsRecipeUnlocked(p, out _));

        return query
            .OrderBy(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(p => FamilyOrder(p.ProductFamily))
            .ThenBy(p => p.RequiredCompanyLevel)
            .ThenBy(p => p.RequiredBrandPoints)
            .ThenBy(p => p.DisplayName, StringComparer.CurrentCulture)
            .ToList();
    }

    private static int FamilyOrder(string family) => family switch
    {
        "Tomato" => 0,
        "Watermelon" => 1,
        "KoreanMelon" => 2,
        "NapaCabbage" => 3,
        _ => 9
    };

    internal bool IsRecipeUnlocked(ProductionRecipeDefinition recipe, out string reason)
    {
        if (Mod.State.Level < Math.Max(1, recipe.RequiredCompanyLevel))
        {
            reason = $"회사 Lv.{Math.Max(1, recipe.RequiredCompanyLevel)} 필요";
            return false;
        }
        if (Mod.State.BrandPoints < Math.Max(0, recipe.RequiredBrandPoints))
        {
            reason = $"브랜드 {Math.Max(0, recipe.RequiredBrandPoints)} 필요";
            return false;
        }
        if (recipe.RequiresCropGenetics && !Mod.Helper.ModRegistry.IsLoaded("Saebyeol.WatermelonGenetics"))
        {
            reason = "Crop Genetics 필요";
            return false;
        }
        reason = "생산 가능";
        return true;
    }

    internal int GetQueueCapacity() => Math.Max(1, Mod.State.ProductionLines.Count);
    internal int GetPlanLimit() => PlanLimit;

    internal int GetLineEfficiency(ProductionLineState? line)
    {
        if (line is null)
            return 88;
        return Math.Clamp(line.BaseEfficiency + Math.Max(0, line.Level - 1) * 2 + Math.Max(0, Mod.State.Level - 1), 80, 98);
    }

    internal int GetIngredientQuantity(ProductionRecipeDefinition recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientIntermediateKey))
        {
            return Mod.State.IntermediateStock.Values
                .Where(p => p is not null && p.Quantity > 0 && string.Equals(p.Key, recipe.IngredientIntermediateKey, StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.Quantity);
        }

        IEnumerable<WarehouseStockEntry> entries = Mod.State.Warehouse.Values.Where(p => p is not null && p.Quantity > 0);
        if (!string.IsNullOrWhiteSpace(recipe.IngredientItemId))
            return entries.Where(p => string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase)).Sum(p => p.Quantity);

        if (!string.IsNullOrWhiteSpace(recipe.IngredientFamily))
        {
            HashSet<string> ids = GetFamilyIds(recipe.IngredientFamily);
            return entries.Where(p => ids.Contains(p.ItemId)).Sum(p => p.Quantity);
        }
        return 0;
    }

    internal string GetIngredientDisplayName(ProductionRecipeDefinition recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientDisplayName))
            return recipe.IngredientDisplayName;
        if (!string.IsNullOrWhiteSpace(recipe.IngredientIntermediateKey))
        {
            IntermediateStockEntry? stock = Mod.State.IntermediateStock.Values.FirstOrDefault(p => string.Equals(p.Key, recipe.IngredientIntermediateKey, StringComparison.OrdinalIgnoreCase));
            if (stock is not null && !string.IsNullOrWhiteSpace(stock.DisplayName))
                return stock.DisplayName;
            ProductionRecipeDefinition? producer = Mod.Recipes.FirstOrDefault(p => string.Equals(p.OutputIntermediateKey, recipe.IngredientIntermediateKey, StringComparison.OrdinalIgnoreCase));
            return producer?.OutputIntermediateDisplayName ?? producer?.DisplayName ?? recipe.IngredientIntermediateKey;
        }
        if (!string.IsNullOrWhiteSpace(recipe.IngredientItemId))
            return Mod.Crops.FirstOrDefault(p => string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? "원재료";
        if (!string.IsNullOrWhiteSpace(recipe.IngredientFamily))
            return Mod.Crops.FirstOrDefault(p => string.Equals(p.Family, recipe.IngredientFamily, StringComparison.OrdinalIgnoreCase))?.FamilyDisplayName?.Replace(" 계열", "") ?? recipe.IngredientFamily;
        return "원재료";
    }

    internal int GetMaxBatches(ProductionRecipeDefinition recipe)
    {
        if (recipe.InputQuantity <= 0)
            return 0;
        return Math.Max(0, GetIngredientQuantity(recipe) / recipe.InputQuantity);
    }

    internal int GetRecipeTotalMinutes(ProductionRecipeDefinition recipe, int batches = 1)
    {
        int perBatch = recipe.Stages?.Count > 0
            ? recipe.Stages.Sum(p => Math.Max(10, p.DurationMinutes))
            : Math.Max(10, recipe.DurationMinutes);
        return perBatch * Math.Max(1, batches);
    }

    internal string GetCurrentStageName(ProductionJob job)
    {
        ProductionRecipeDefinition? recipe = FindRecipe(job.RecipeKey);
        if (recipe is null || recipe.Stages.Count == 0)
            return "가공";
        int index = Math.Clamp(job.CurrentStageIndex, 0, recipe.Stages.Count - 1);
        return job.AwaitingStageAdvance ? $"{recipe.Stages[index].DisplayName} 완료" : recipe.Stages[index].DisplayName;
    }

    internal float GetJobProgress(ProductionJob job)
    {
        if (job.TotalMinutes <= 0)
            return 1f;
        return Math.Clamp(1f - job.RemainingMinutes / (float)job.TotalMinutes, 0f, 1f);
    }

    internal string EstimateGrade(ProductionRecipeDefinition recipe)
    {
        int inputScore = EstimateInputQualityScore(recipe);
        ProductionLineState? line = Mod.State.ProductionLines.FirstOrDefault(p => string.Equals(p.LineType, recipe.LineType, StringComparison.OrdinalIgnoreCase));
        return GradeFromScore(ComputeFinalScore(inputScore, GetLineEfficiency(line)));
    }

    internal int EstimateInputQualityScore(ProductionRecipeDefinition recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientIntermediateKey))
        {
            List<IntermediateStockEntry> candidates = GetIntermediateCandidates(recipe);
            int quantity = candidates.Sum(p => Math.Max(0, p.Quantity));
            if (quantity <= 0)
                return 55;
            long total = candidates.Sum(p => (long)Math.Max(0, p.Quantity) * QualityScore(p.Quality));
            return (int)Math.Clamp(total / quantity, 0, 100);
        }

        List<WarehouseStockEntry> raw = GetIngredientCandidates(recipe);
        int rawQuantity = raw.Sum(p => Math.Max(0, p.Quantity));
        if (rawQuantity <= 0)
            return 55;
        long rawTotal = raw.Sum(p => (long)Math.Max(0, p.Quantity) * QualityScore(p.Quality));
        return (int)Math.Clamp(rawTotal / rawQuantity, 0, 100);
    }

    internal int EstimateOutputQuantity(ProductionRecipeDefinition recipe, int batches, int efficiency)
    {
        int baseOutput = Math.Max(1, recipe.OutputQuantity) * Math.Max(1, batches);
        float yieldFactor = 0.90f + Math.Clamp(efficiency, 80, 100) / 1000f;
        return Math.Max(1, (int)Math.Round(baseOutput * yieldFactor));
    }

    internal bool TryStart(string recipeKey, int requestedBatches, out string message)
    {
        EnsureState();
        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            if (!Mod.Multiplayer.IsSynchronized)
            {
                message = "공동 회사 데이터를 동기화하는 중입니다.";
                Mod.Multiplayer.RequestSync();
                return false;
            }
            ProductionRecipeDefinition? recipe = FindRecipe(recipeKey);
            if (recipe is null && !recipeKey.StartsWith('@'))
            {
                message = "생산 레시피를 찾을 수 없습니다.";
                return false;
            }
            Mod.Multiplayer.RequestProduction(recipeKey, Math.Max(1, requestedBatches));
            message = recipe is null ? "생산계획 변경을 공동 회사에 반영 중입니다." : $"{recipe.DisplayName} 생산계획을 공동 회사에 반영 중입니다.";
            return true;
        }

        bool ok = TryStartAuthoritative(recipeKey, requestedBatches, out message);
        if (ok)
            Mod.Multiplayer.BroadcastState();
        return ok;
    }

    internal bool TryStartAuthoritative(string recipeKey, int requestedBatches, out string message)
    {
        EnsureState();
        if (TryParsePlanAction(recipeKey, out string action, out string planId))
            return TryPlanActionAuthoritative(action, planId, out message);

        ProductionRecipeDefinition? recipe = FindRecipe(recipeKey);
        if (recipe is null)
        {
            message = "생산 레시피를 찾을 수 없습니다.";
            return false;
        }
        if (!IsRecipeUnlocked(recipe, out string lockReason))
        {
            message = $"{recipe.DisplayName}: {lockReason}";
            return false;
        }
        if (Mod.State.ProductionPlans.Count >= PlanLimit)
        {
            message = $"생산계획은 최대 {PlanLimit}개까지 등록할 수 있습니다.";
            return false;
        }

        int batches = Math.Clamp(requestedBatches, 1, 99);
        ProductionPlanEntry plan = new()
        {
            RecipeKey = recipe.Key,
            BatchCount = batches,
            Priority = Mod.State.ProductionPlans.Count,
            CreatedDayNumber = Context.IsWorldReady ? ContractCore.GetCurrentDayNumber() : 0
        };
        Mod.State.ProductionPlans.Add(plan);
        NormalizePriorities();
        bool started = DispatchPlans();
        message = started
            ? $"{recipe.DisplayName} {batches}배치 계획을 추가하고 생산을 시작했습니다."
            : $"{recipe.DisplayName} {batches}배치를 생산계획에 추가했습니다.";
        return true;
    }

    internal bool TryMovePlan(string planId, int direction, out string message)
    {
        string action = direction < 0 ? "moveup" : "movedown";
        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            Mod.Multiplayer.RequestProduction($"@{action}:{planId}", 1);
            message = "생산계획 우선순위를 공동 회사에 반영 중입니다.";
            return true;
        }
        bool ok = TryPlanActionAuthoritative(action, planId, out message);
        if (ok) Mod.Multiplayer.BroadcastState();
        return ok;
    }

    internal bool TryRemovePlan(string planId, out string message)
    {
        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            Mod.Multiplayer.RequestProduction($"@remove:{planId}", 1);
            message = "생산계획 삭제를 공동 회사에 반영 중입니다.";
            return true;
        }
        bool ok = TryPlanActionAuthoritative("remove", planId, out message);
        if (ok) Mod.Multiplayer.BroadcastState();
        return ok;
    }

    private bool TryPlanActionAuthoritative(string action, string planId, out string message)
    {
        EnsureState();
        ProductionPlanEntry? plan = Mod.State.ProductionPlans.FirstOrDefault(p => string.Equals(p.Id, planId, StringComparison.Ordinal));
        if (plan is null)
        {
            message = "이미 처리되었거나 존재하지 않는 생산계획입니다.";
            return false;
        }

        List<ProductionPlanEntry> ordered = Mod.State.ProductionPlans.OrderBy(p => p.Priority).ToList();
        int index = ordered.IndexOf(plan);
        if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
        {
            Mod.State.ProductionPlans.Remove(plan);
            NormalizePriorities();
            message = "생산계획을 삭제했습니다.";
            return true;
        }

        int target = string.Equals(action, "moveup", StringComparison.OrdinalIgnoreCase) ? index - 1 : index + 1;
        if (target < 0 || target >= ordered.Count)
        {
            message = "더 이상 우선순위를 이동할 수 없습니다.";
            return false;
        }
        (ordered[index].Priority, ordered[target].Priority) = (ordered[target].Priority, ordered[index].Priority);
        NormalizePriorities();
        message = "생산계획 우선순위를 변경했습니다.";
        return true;
    }

    private static bool TryParsePlanAction(string recipeKey, out string action, out string planId)
    {
        action = "";
        planId = "";
        if (string.IsNullOrWhiteSpace(recipeKey) || !recipeKey.StartsWith('@'))
            return false;
        int split = recipeKey.IndexOf(':');
        if (split <= 1 || split >= recipeKey.Length - 1)
            return false;
        action = recipeKey[1..split];
        planId = recipeKey[(split + 1)..];
        return action is "moveup" or "movedown" or "remove";
    }

    private void NormalizePriorities()
    {
        int index = 0;
        foreach (ProductionPlanEntry plan in Mod.State.ProductionPlans.OrderBy(p => p.Priority).ThenBy(p => p.CreatedDayNumber).ThenBy(p => p.Id, StringComparer.Ordinal))
            plan.Priority = index++;
    }

    private bool DispatchPlans()
    {
        if (!Context.IsMainPlayer)
            return false;

        bool anyStarted = false;
        bool started;
        do
        {
            started = false;
            foreach (ProductionPlanEntry plan in Mod.State.ProductionPlans.OrderBy(p => p.Priority).ToList())
            {
                ProductionRecipeDefinition? recipe = FindRecipe(plan.RecipeKey);
                if (recipe is null)
                {
                    Mod.State.ProductionPlans.Remove(plan);
                    started = true;
                    continue;
                }
                if (!IsRecipeUnlocked(recipe, out _))
                    continue;

                ProductionLineState? line = Mod.State.ProductionLines
                    .Where(p => GetLineJob(p.Id) is null)
                    .FirstOrDefault(p => string.Equals(p.LineType, recipe.LineType, StringComparison.OrdinalIgnoreCase));
                if (line is null)
                    continue;

                int required = Math.Max(1, recipe.InputQuantity) * Math.Max(1, plan.BatchCount);
                if (GetIngredientQuantity(recipe) < required)
                    continue;

                if (!TryConsumeIngredients(recipe, required, out int inputQualityScore))
                    continue;

                StartJob(plan, recipe, line, inputQualityScore);
                Mod.State.ProductionPlans.Remove(plan);
                NormalizePriorities();
                anyStarted = true;
                started = true;
                break;
            }
        }
        while (started);

        return anyStarted;
    }

    private void StartJob(ProductionPlanEntry plan, ProductionRecipeDefinition recipe, ProductionLineState line, int inputQualityScore)
    {
        int efficiency = GetLineEfficiency(line);
        int score = ComputeFinalScore(inputQualityScore, efficiency);
        string grade = GradeFromScore(score);
        int quality = QualityFromGrade(grade);
        ProductionStageDefinition stage = recipe.Stages[0];
        int stageMinutes = Math.Max(10, stage.DurationMinutes) * Math.Max(1, plan.BatchCount);
        int totalMinutes = GetRecipeTotalMinutes(recipe, plan.BatchCount);

        Mod.State.ProductionQueue.Add(new ProductionJob
        {
            RecipeKey = recipe.Key,
            BatchCount = Math.Max(1, plan.BatchCount),
            OutputQuality = quality,
            OutputGrade = grade,
            RemainingMinutes = totalMinutes,
            TotalMinutes = totalMinutes,
            LineId = line.Id,
            CurrentStageIndex = 0,
            StageRemainingMinutes = stageMinutes,
            StageTotalMinutes = stageMinutes,
            EfficiencyPercent = efficiency,
            InputQualityScore = inputQualityScore,
            EstimatedOutputQuantity = EstimateOutputQuantity(recipe, plan.BatchCount, efficiency)
        });
    }

    internal int GetFinishedQuantity(string productKey)
        => Mod.State.FinishedGoods.Values
            .Where(p => p is not null && p.Quantity > 0 && string.Equals(p.ProductKey, productKey, StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Quantity);

    internal int GetFinishedGoodsTotal()
        => Mod.State.FinishedGoods.Values.Where(p => p is not null).Sum(p => Math.Max(0, p.Quantity));

    internal IReadOnlyList<(int Quality, int Quantity)> GetFinishedQualityBreakdown(string productKey)
        => Mod.State.FinishedGoods.Values
            .Where(p => p is not null && p.Quantity > 0 && string.Equals(p.ProductKey, productKey, StringComparison.OrdinalIgnoreCase))
            .GroupBy(p => p.Quality)
            .OrderBy(p => p.Key)
            .Select(p => (p.Key, p.Sum(x => x.Quantity)))
            .ToList();

    internal IReadOnlyList<IntermediateStockEntry> GetIntermediateStock()
    {
        EnsureState();
        return Mod.State.IntermediateStock.Values
            .Where(p => p is not null && p.Quantity > 0)
            .OrderBy(p => p.DisplayName, StringComparer.CurrentCulture)
            .ToList();
    }

    private bool TryConsumeIngredients(ProductionRecipeDefinition recipe, int required, out int inputQualityScore)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientIntermediateKey))
            return TryConsumeIntermediate(recipe, required, out inputQualityScore);
        return TryConsumeRaw(recipe, required, out inputQualityScore);
    }

    private bool TryConsumeIntermediate(ProductionRecipeDefinition recipe, int required, out int inputQualityScore)
    {
        inputQualityScore = 55;
        List<(string Key, IntermediateStockEntry Stock)> candidates = Mod.State.IntermediateStock
            .Where(p => p.Value is not null
                && p.Value.Quantity > 0
                && string.Equals(p.Value.Key, recipe.IngredientIntermediateKey, StringComparison.OrdinalIgnoreCase))
            .Select(p => (p.Key, p.Value))
            .OrderBy(p => p.Value.Quality)
            .ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Sum(p => p.Stock.Quantity) < required)
            return false;

        int remaining = required;
        long weightedScore = 0;
        int consumed = 0;
        foreach ((string key, IntermediateStockEntry stock) in candidates)
        {
            if (remaining <= 0) break;
            int take = Math.Min(remaining, stock.Quantity);
            if (take <= 0) continue;
            stock.Quantity -= take;
            remaining -= take;
            consumed += take;
            weightedScore += (long)take * QualityScore(stock.Quality);
            if (stock.Quantity <= 0)
                Mod.State.IntermediateStock.Remove(key);
        }

        if (remaining > 0 || consumed <= 0)
            return false;
        inputQualityScore = (int)Math.Clamp(weightedScore / consumed, 0, 100);
        return true;
    }

    private bool TryConsumeRaw(ProductionRecipeDefinition recipe, int required, out int inputQualityScore)
    {
        inputQualityScore = 55;
        List<WarehouseStockEntry> candidates = GetIngredientCandidates(recipe)
            .OrderBy(p => p.Quality)
            .ThenBy(p => p.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Sum(p => p.Quantity) < required)
            return false;

        int remaining = required;
        long weightedScore = 0;
        int consumed = 0;
        foreach (WarehouseStockEntry entry in candidates)
        {
            if (remaining <= 0) break;
            int take = Math.Min(remaining, entry.Quantity);
            if (take <= 0) continue;
            entry.Quantity -= take;
            remaining -= take;
            consumed += take;
            weightedScore += (long)take * QualityScore(entry.Quality);
        }
        Mod.Company.CleanupWarehouse();
        if (remaining > 0 || consumed <= 0)
            return false;
        inputQualityScore = (int)Math.Clamp(weightedScore / consumed, 0, 100);
        return true;
    }

    private List<WarehouseStockEntry> GetIngredientCandidates(ProductionRecipeDefinition recipe)
    {
        IEnumerable<WarehouseStockEntry> entries = Mod.State.Warehouse.Values.Where(p => p is not null && p.Quantity > 0);
        if (!string.IsNullOrWhiteSpace(recipe.IngredientItemId))
            return entries.Where(p => string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(recipe.IngredientFamily))
        {
            HashSet<string> ids = GetFamilyIds(recipe.IngredientFamily);
            return entries.Where(p => ids.Contains(p.ItemId)).ToList();
        }
        return new List<WarehouseStockEntry>();
    }

    private List<IntermediateStockEntry> GetIntermediateCandidates(ProductionRecipeDefinition recipe)
        => Mod.State.IntermediateStock.Values
            .Where(p => p is not null
                && p.Quantity > 0
                && string.Equals(p.Key, recipe.IngredientIntermediateKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private HashSet<string> GetFamilyIds(string family)
        => Mod.Crops.Where(p => string.Equals(p.Family, family, StringComparison.OrdinalIgnoreCase)).Select(p => p.ItemId).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        EnsureState();
        bool changed = false;
        foreach (ProductionJob job in Mod.State.ProductionQueue.ToList())
        {
            ProductionRecipeDefinition? recipe = FindRecipe(job.RecipeKey);
            if (recipe is null)
            {
                Mod.State.ProductionQueue.Remove(job);
                changed = true;
                continue;
            }

            if (job.AwaitingStageAdvance)
            {
                if (TryAdvanceStage(job, recipe))
                    changed = true;
                continue;
            }

            job.StageRemainingMinutes = Math.Max(0, job.StageRemainingMinutes - 10);
            job.RemainingMinutes = Math.Max(0, job.RemainingMinutes - 10);
            changed = true;

            if (job.StageRemainingMinutes > 0)
                continue;

            bool finalStage = job.CurrentStageIndex >= recipe.Stages.Count - 1;
            if (finalStage)
            {
                Complete(job, recipe);
                Mod.State.ProductionQueue.Remove(job);
                continue;
            }

            BufferStageIntermediate(job, recipe.Stages[job.CurrentStageIndex]);
            job.AwaitingStageAdvance = true;
        }

        if (DispatchPlans())
            changed = true;

        if (changed)
            Mod.Multiplayer.BroadcastState();
    }

    private void BufferStageIntermediate(ProductionJob job, ProductionStageDefinition stage)
    {
        string logicalKey = string.IsNullOrWhiteSpace(stage.IntermediateKey) ? $"stage:{job.Id}:{stage.Key}" : $"stage:{job.Id}:{stage.IntermediateKey}";
        string display = string.IsNullOrWhiteSpace(stage.IntermediateDisplayName) ? $"{stage.DisplayName} 공정재" : stage.IntermediateDisplayName;
        int quantity = Math.Max(1, job.EstimatedOutputQuantity);
        string stockKey = IntermediateKey(logicalKey, job.OutputQuality);
        if (!Mod.State.IntermediateStock.TryGetValue(stockKey, out IntermediateStockEntry? stock) || stock is null)
        {
            stock = new IntermediateStockEntry
            {
                Key = logicalKey,
                DisplayName = display,
                Quality = job.OutputQuality,
                Grade = job.OutputGrade,
                Quantity = 0
            };
            Mod.State.IntermediateStock[stockKey] = stock;
        }
        stock.Quantity += quantity;
        job.BufferedIntermediateKey = logicalKey;
        job.BufferedIntermediateQuantity = quantity;
    }

    private bool TryAdvanceStage(ProductionJob job, ProductionRecipeDefinition recipe)
    {
        if (job.CurrentStageIndex >= recipe.Stages.Count - 1)
            return false;

        if (!string.IsNullOrWhiteSpace(job.BufferedIntermediateKey) && job.BufferedIntermediateQuantity > 0)
        {
            string stockKey = IntermediateKey(job.BufferedIntermediateKey, job.OutputQuality);
            if (!Mod.State.IntermediateStock.TryGetValue(stockKey, out IntermediateStockEntry? stock) || stock is null || stock.Quantity < job.BufferedIntermediateQuantity)
                return false;
            stock.Quantity -= job.BufferedIntermediateQuantity;
            if (stock.Quantity <= 0)
                Mod.State.IntermediateStock.Remove(stockKey);
        }

        job.CurrentStageIndex++;
        ProductionStageDefinition next = recipe.Stages[job.CurrentStageIndex];
        int duration = Math.Max(10, next.DurationMinutes) * Math.Max(1, job.BatchCount);
        job.StageRemainingMinutes = duration;
        job.StageTotalMinutes = duration;
        job.AwaitingStageAdvance = false;
        job.BufferedIntermediateKey = "";
        job.BufferedIntermediateQuantity = 0;
        return true;
    }

    private void Complete(ProductionJob job, ProductionRecipeDefinition recipe)
    {
        int quantity = Math.Max(1, job.EstimatedOutputQuantity);
        if (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase))
            AddPersistentIntermediate(recipe, job, quantity);
        else
            AddFinishedProduct(recipe, job, quantity);

        Mod.State.LifetimeProductionBatches += Math.Max(1, job.BatchCount);
        Mod.Company.AddCompanyExperience(Math.Max(1, job.BatchCount) * (string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase) ? 2 : 3));

        string outputName = string.Equals(recipe.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase)
            ? (string.IsNullOrWhiteSpace(recipe.OutputIntermediateDisplayName) ? recipe.DisplayName : recipe.OutputIntermediateDisplayName)
            : recipe.DisplayName;
        string notice = $"생산 완료: {outputName} {job.OutputGrade}급 {quantity:N0}{recipe.OutputUnit}";
        Game1.addHUDMessage(new HUDMessage(notice));
        Game1.playSound("newArtifact");
        Mod.Multiplayer.BroadcastNotice(notice);
    }

    private void AddPersistentIntermediate(ProductionRecipeDefinition recipe, ProductionJob job, int quantity)
    {
        string logicalKey = string.IsNullOrWhiteSpace(recipe.OutputIntermediateKey) ? recipe.Key : recipe.OutputIntermediateKey;
        string display = string.IsNullOrWhiteSpace(recipe.OutputIntermediateDisplayName) ? recipe.DisplayName : recipe.OutputIntermediateDisplayName;
        string key = IntermediateKey(logicalKey, job.OutputQuality);
        if (!Mod.State.IntermediateStock.TryGetValue(key, out IntermediateStockEntry? stock) || stock is null)
        {
            stock = new IntermediateStockEntry
            {
                Key = logicalKey,
                DisplayName = display,
                Quality = job.OutputQuality,
                Grade = job.OutputGrade,
                Quantity = 0
            };
            Mod.State.IntermediateStock[key] = stock;
        }
        stock.Grade = job.OutputGrade;
        stock.Quantity += quantity;
        Mod.State.LifetimeIntermediateUnits += quantity;
    }

    private void AddFinishedProduct(ProductionRecipeDefinition recipe, ProductionJob job, int quantity)
    {
        string key = FinishedKey(recipe.Key, job.OutputQuality);
        if (!Mod.State.FinishedGoods.TryGetValue(key, out ProductStockEntry? stock) || stock is null)
        {
            stock = new ProductStockEntry
            {
                ProductKey = recipe.Key,
                Quality = job.OutputQuality,
                Grade = job.OutputGrade,
                Quantity = 0
            };
            Mod.State.FinishedGoods[key] = stock;
        }
        stock.Grade = string.IsNullOrWhiteSpace(stock.Grade) ? job.OutputGrade : stock.Grade;
        stock.Quantity += quantity;
        Mod.State.LifetimeFinishedGoods += quantity;
    }

    private void CleanupIntermediate()
    {
        foreach (string key in Mod.State.IntermediateStock.Where(p => p.Value is null || p.Value.Quantity <= 0).Select(p => p.Key).ToList())
            Mod.State.IntermediateStock.Remove(key);
    }

    private static int ComputeFinalScore(int inputQualityScore, int efficiency)
        => (int)Math.Clamp(Math.Round(inputQualityScore * 0.68 + efficiency * 0.32), 0, 100);

    private static int QualityScore(int quality) => quality switch
    {
        4 => 96,
        2 => 84,
        1 => 70,
        _ => 55
    };

    internal static string GradeFromScore(int score)
        => score >= 90 ? "S" : score >= 78 ? "A" : score >= 65 ? "B" : "C";

    internal static string GradeFromQuality(int quality) => quality switch
    {
        4 => "S",
        2 => "A",
        1 => "B",
        _ => "C"
    };

    internal static int QualityFromGrade(string grade) => grade switch
    {
        "S" => 4,
        "A" => 2,
        "B" => 1,
        _ => 0
    };

    private static string FinishedKey(string productKey, int quality) => $"{quality}:{productKey}";
    private static string IntermediateKey(string key, int quality) => $"{quality}:{key}";

    internal static string FormatDuration(int minutes)
    {
        minutes = Math.Max(0, minutes);
        if (minutes < 60) return $"{minutes}분";
        int hours = minutes / 60;
        int remain = minutes % 60;
        return remain == 0 ? $"{hours}시간" : $"{hours}시간 {remain}분";
    }
}
