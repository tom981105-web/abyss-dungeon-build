using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductionCore
{
    private readonly ModEntry Mod;

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
        Mod.State.FinishedGoods ??= new Dictionary<string, ProductStockEntry>(StringComparer.OrdinalIgnoreCase);
    }

    internal ProductionRecipeDefinition? FindRecipe(string key)
        => Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));

    internal int GetQueueCapacity() => Mod.State.Level switch
    {
        <= 2 => 1,
        <= 4 => 2,
        _ => 3
    };

    internal int GetIngredientQuantity(ProductionRecipeDefinition recipe)
    {
        IEnumerable<WarehouseStockEntry> entries = Mod.State.Warehouse.Values.Where(p => p is not null && p.Quantity > 0);
        if (!string.IsNullOrWhiteSpace(recipe.IngredientItemId))
            return entries.Where(p => string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase)).Sum(p => p.Quantity);

        if (!string.IsNullOrWhiteSpace(recipe.IngredientFamily))
        {
            HashSet<string> ids = Mod.Crops
                .Where(p => string.Equals(p.Family, recipe.IngredientFamily, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ItemId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return entries.Where(p => ids.Contains(p.ItemId)).Sum(p => p.Quantity);
        }

        return 0;
    }

    internal int GetMaxBatches(ProductionRecipeDefinition recipe)
    {
        if (recipe.InputQuantity <= 0)
            return 0;
        return GetIngredientQuantity(recipe) / recipe.InputQuantity;
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
            if (recipe is null)
            {
                message = "생산 레시피를 찾을 수 없습니다.";
                return false;
            }

            Mod.Multiplayer.RequestProduction(recipeKey, Math.Max(1, requestedBatches));
            message = $"{recipe.DisplayName} 생산 요청을 전송했습니다.";
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
        ProductionRecipeDefinition? recipe = FindRecipe(recipeKey);
        if (recipe is null)
        {
            message = "생산 레시피를 찾을 수 없습니다.";
            return false;
        }

        if (Mod.State.Level < recipe.RequiredCompanyLevel)
        {
            message = $"회사 Lv.{recipe.RequiredCompanyLevel}부터 생산할 수 있습니다.";
            return false;
        }

        if (Mod.State.ProductionQueue.Count >= GetQueueCapacity())
        {
            message = "현재 생산라인이 모두 가동 중입니다.";
            return false;
        }

        int batches = Math.Max(1, requestedBatches);
        int max = GetMaxBatches(recipe);
        if (max <= 0)
        {
            message = $"회사 창고에 원재료가 부족합니다. ({recipe.InputQuantity}개 필요)";
            return false;
        }
        batches = Math.Min(batches, max);

        int required = checked(recipe.InputQuantity * batches);
        if (!TryConsumeIngredients(recipe, required, out int outputQuality))
        {
            message = "원재료 출고 중 재고가 변경되어 생산을 시작하지 못했습니다.";
            return false;
        }

        int totalMinutes = Math.Max(10, recipe.DurationMinutes) * batches;
        ProductionJob job = new()
        {
            RecipeKey = recipe.Key,
            BatchCount = batches,
            OutputQuality = outputQuality,
            RemainingMinutes = totalMinutes,
            TotalMinutes = totalMinutes
        };
        Mod.State.ProductionQueue.Add(job);
        message = $"{recipe.DisplayName} {batches}배치 생산을 시작했습니다.";
        return true;
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

    private bool TryConsumeIngredients(ProductionRecipeDefinition recipe, int required, out int outputQuality)
    {
        outputQuality = 0;
        HashSet<string>? familyIds = null;
        if (string.IsNullOrWhiteSpace(recipe.IngredientItemId) && !string.IsNullOrWhiteSpace(recipe.IngredientFamily))
        {
            familyIds = Mod.Crops
                .Where(p => string.Equals(p.Family, recipe.IngredientFamily, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ItemId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        List<WarehouseStockEntry> candidates = Mod.State.Warehouse.Values
            .Where(p => p is not null && p.Quantity > 0)
            .Where(p => !string.IsNullOrWhiteSpace(recipe.IngredientItemId)
                ? string.Equals(p.ItemId, recipe.IngredientItemId, StringComparison.OrdinalIgnoreCase)
                : familyIds is not null && familyIds.Contains(p.ItemId))
            .OrderBy(p => p.Quality)
            .ThenBy(p => p.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Sum(p => p.Quantity) < required)
            return false;

        int remaining = required;
        int minQuality = int.MaxValue;
        foreach (WarehouseStockEntry entry in candidates)
        {
            if (remaining <= 0)
                break;
            int take = Math.Min(remaining, entry.Quantity);
            if (take <= 0)
                continue;
            entry.Quantity -= take;
            remaining -= take;
            minQuality = Math.Min(minQuality, entry.Quality);
        }

        Mod.Company.CleanupWarehouse();
        outputQuality = minQuality == int.MaxValue ? 0 : minQuality;
        return remaining == 0;
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        EnsureState();
        if (Mod.State.ProductionQueue.Count == 0)
            return;

        bool changed = false;
        foreach (ProductionJob job in Mod.State.ProductionQueue.ToList())
        {
            job.RemainingMinutes = Math.Max(0, job.RemainingMinutes - 10);
            changed = true;
            if (job.RemainingMinutes > 0)
                continue;
            Complete(job);
            Mod.State.ProductionQueue.Remove(job);
        }

        if (changed)
            Mod.Multiplayer.BroadcastState();
    }

    private void Complete(ProductionJob job)
    {
        ProductionRecipeDefinition? recipe = FindRecipe(job.RecipeKey);
        if (recipe is null)
            return;

        int quantity = Math.Max(1, recipe.OutputQuantity) * Math.Max(1, job.BatchCount);
        string key = FinishedKey(recipe.Key, job.OutputQuality);
        if (!Mod.State.FinishedGoods.TryGetValue(key, out ProductStockEntry? stock) || stock is null)
        {
            stock = new ProductStockEntry
            {
                ProductKey = recipe.Key,
                Quality = job.OutputQuality,
                Quantity = 0
            };
            Mod.State.FinishedGoods[key] = stock;
        }
        stock.Quantity += quantity;
        Mod.State.LifetimeProductionBatches += Math.Max(1, job.BatchCount);
        Mod.State.LifetimeFinishedGoods += quantity;
        Mod.Company.AddCompanyExperience(Math.Max(1, job.BatchCount) * 2);

        string notice = $"생산 완료: {recipe.DisplayName} {quantity:N0}개";
        Game1.addHUDMessage(new HUDMessage(notice));
        Game1.playSound("newArtifact");
        Mod.Multiplayer.BroadcastNotice(notice);
    }

    private static string FinishedKey(string productKey, int quality) => $"{quality}:{productKey}";

    internal static string FormatDuration(int minutes)
    {
        minutes = Math.Max(0, minutes);
        if (minutes < 60)
            return $"{minutes}분";
        int hours = minutes / 60;
        int remain = minutes % 60;
        return remain == 0 ? $"{hours}시간" : $"{hours}시간 {remain}분";
    }
}
