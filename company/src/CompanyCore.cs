using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SObject = StardewValley.Object;

namespace AgriculturalCompany;

internal sealed class CompanyCore
{
    private readonly ModEntry Mod;

    internal CompanyCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize(IModHelper helper)
    {
        helper.Events.Player.InventoryChanged += OnInventoryChanged;
    }

    internal bool HandleButton(ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return false;

        if (!Enum.TryParse<SButton>(Mod.Config.CompanyKey, true, out SButton key) || e.Button != key)
            return false;

        if (Game1.activeClickableMenu is null)
        {
            EnsureState();
            Game1.activeClickableMenu = new CompanyMenu(Mod);
            Game1.playSound("bigSelect");
        }

        Mod.Helper.Input.Suppress(e.Button);
        return true;
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsLocalPlayer || Game1.activeClickableMenu is not null)
            return;

        foreach (Item item in e.Added)
            TrackProduction(item, item.Stack);

        foreach (var change in e.QuantityChanged)
        {
            int gained = change.NewSize - change.OldSize;
            if (gained > 0)
                TrackProduction(change.Item, gained);
        }
    }

    private void TrackProduction(Item item, int amount)
    {
        if (amount <= 0)
            return;

        TrackedCropDefinition? crop = FindCrop(item.QualifiedItemId);
        if (crop is null)
            return;

        EnsureState();
        Add(Mod.State.TodayHarvest, crop.ItemId, amount);
        Add(Mod.State.SeasonHarvest, crop.ItemId, amount);
        Add(Mod.State.LifetimeHarvest, crop.ItemId, amount);
        AddCompanyExperience(amount);
    }

    internal void EnsureState()
    {
        if (!Context.IsWorldReady)
            return;

        Mod.State.TodayHarvest ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Mod.State.SeasonHarvest ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Mod.State.LifetimeHarvest ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Mod.State.Warehouse ??= new Dictionary<string, WarehouseStockEntry>(StringComparer.OrdinalIgnoreCase);
        Mod.State.ProductionQueue ??= new List<ProductionJob>();
        Mod.State.FinishedGoods ??= new Dictionary<string, ProductStockEntry>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(Mod.State.CompanyName))
        {
            string farm = Game1.player.farmName.Value;
            Mod.State.CompanyName = string.IsNullOrWhiteSpace(farm) ? "새별 농업" : $"{farm} 농업";
        }

        string dayKey = $"{Game1.year}:{Game1.currentSeason}:{Game1.dayOfMonth}";
        if (!string.Equals(Mod.State.LastDayKey, dayKey, StringComparison.Ordinal))
        {
            Mod.State.TodayHarvest.Clear();
            Mod.State.LastDayKey = dayKey;
        }

        string seasonKey = $"{Game1.year}:{Game1.currentSeason}";
        if (!string.Equals(Mod.State.SeasonKey, seasonKey, StringComparison.Ordinal))
        {
            Mod.State.SeasonHarvest.Clear();
            Mod.State.SeasonRevenue = 0;
            Mod.State.SeasonKey = seasonKey;
        }

        foreach ((string key, WarehouseStockEntry entry) in Mod.State.Warehouse.ToList())
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.ItemId) || entry.Quantity <= 0)
                Mod.State.Warehouse.Remove(key);
        }

        foreach ((string key, ProductStockEntry entry) in Mod.State.FinishedGoods.ToList())
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.ProductKey) || entry.Quantity <= 0)
                Mod.State.FinishedGoods.Remove(key);
        }

        Mod.State.ProductionQueue.RemoveAll(p => p is null || string.IsNullOrWhiteSpace(p.RecipeKey) || p.BatchCount <= 0 || p.RemainingMinutes <= 0);
        UpdateLevel(Mod.State);
    }

    internal TrackedCropDefinition? FindCrop(string itemId)
        => Mod.Crops.FirstOrDefault(p => string.Equals(p.ItemId, itemId, StringComparison.OrdinalIgnoreCase));

    internal int GetTotal(Dictionary<string, int> source, string? family = null)
    {
        int total = 0;
        foreach (TrackedCropDefinition crop in Mod.Crops)
        {
            if (family is not null && !string.Equals(crop.Family, family, StringComparison.OrdinalIgnoreCase))
                continue;
            if (source.TryGetValue(crop.ItemId, out int value))
                total += value;
        }
        return total;
    }

    internal int GetWarehouseCapacity() => Mod.State.Level switch
    {
        <= 1 => 200,
        2 => 500,
        3 => 1000,
        4 => 2500,
        _ => 5000
    };

    internal int GetWarehouseUsed()
        => Mod.State.Warehouse.Values.Where(p => p is not null).Sum(p => Math.Max(0, p.Quantity));

    internal int GetWarehouseQuantity(string itemId)
        => Mod.State.Warehouse.Values
            .Where(p => p is not null && string.Equals(p.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            .Sum(p => Math.Max(0, p.Quantity));

    internal int GetWarehouseQuantity(string itemId, int quality)
        => Mod.State.Warehouse.TryGetValue(WarehouseKey(itemId, quality), out WarehouseStockEntry? entry)
            ? Math.Max(0, entry.Quantity)
            : 0;

    internal int GetPlayerQuantity(string itemId)
        => Game1.player.Items.Where(p => p is not null && string.Equals(p.QualifiedItemId, itemId, StringComparison.OrdinalIgnoreCase)).Sum(p => p?.Stack ?? 0);

    internal int DepositFromPlayer(string itemId, int requested)
    {
        EnsureState();
        if (requested <= 0 || FindCrop(itemId) is null)
            return 0;

        int remainingCapacity = Math.Max(0, GetWarehouseCapacity() - GetWarehouseUsed());
        int remaining = Math.Min(requested, remainingCapacity);
        if (remaining <= 0)
            return 0;

        int moved = 0;
        for (int i = Game1.player.Items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            Item? item = Game1.player.Items[i];
            if (item is null || !string.Equals(item.QualifiedItemId, itemId, StringComparison.OrdinalIgnoreCase))
                continue;

            int quality = item is SObject obj ? obj.Quality : 0;
            int take = Math.Min(remaining, item.Stack);
            AddWarehouse(itemId, quality, take);
            item.Stack -= take;
            remaining -= take;
            moved += take;

            if (item.Stack <= 0)
                Game1.player.Items[i] = null;
        }

        Mod.State.LifetimeDeposited += moved;
        return moved;
    }

    internal int DepositAllFromPlayer(string itemId)
        => DepositFromPlayer(itemId, int.MaxValue);

    internal int WithdrawToPlayer(string itemId, int requested)
    {
        EnsureState();
        if (requested <= 0)
            return 0;

        int remaining = requested;
        int moved = 0;
        List<WarehouseStockEntry> entries = Mod.State.Warehouse.Values
            .Where(p => p is not null && p.Quantity > 0 && string.Equals(p.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Quality)
            .ToList();

        foreach (WarehouseStockEntry entry in entries)
        {
            while (entry.Quantity > 0 && remaining > 0)
            {
                Item item = ItemRegistry.Create(entry.ItemId, 1, entry.Quality);
                if (!Game1.player.addItemToInventoryBool(item))
                {
                    Mod.State.LifetimeWithdrawn += moved;
                    CleanupWarehouse();
                    return moved;
                }

                entry.Quantity--;
                remaining--;
                moved++;
            }

            if (remaining <= 0)
                break;
        }

        Mod.State.LifetimeWithdrawn += moved;
        CleanupWarehouse();
        return moved;
    }

    internal int WithdrawAllToPlayer(string itemId)
        => WithdrawToPlayer(itemId, GetWarehouseQuantity(itemId));

    internal IReadOnlyList<(int Quality, int Quantity)> GetQualityBreakdown(string itemId)
        => Mod.State.Warehouse.Values
            .Where(p => p is not null && p.Quantity > 0 && string.Equals(p.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            .GroupBy(p => p.Quality)
            .OrderBy(p => p.Key)
            .Select(p => (p.Key, p.Sum(x => x.Quantity)))
            .ToList();

    internal void AddCompanyExperience(int amount)
    {
        if (amount <= 0)
            return;

        int oldLevel = Mod.State.Level;
        Mod.State.Experience += amount;
        UpdateLevel(Mod.State);
        if (Context.IsWorldReady && Mod.State.Level > oldLevel)
        {
            Game1.addHUDMessage(new HUDMessage($"농업회사 단계 상승! {GetStageName(Mod.State.Level)}"));
            Game1.playSound("achievement");
        }
    }

    private void AddWarehouse(string itemId, int quality, int amount)
    {
        if (amount <= 0)
            return;

        string key = WarehouseKey(itemId, quality);
        if (!Mod.State.Warehouse.TryGetValue(key, out WarehouseStockEntry? entry) || entry is null)
        {
            entry = new WarehouseStockEntry { ItemId = itemId, Quality = quality, Quantity = 0 };
            Mod.State.Warehouse[key] = entry;
        }
        entry.Quantity += amount;
    }

    internal void CleanupWarehouse()
    {
        foreach ((string key, WarehouseStockEntry entry) in Mod.State.Warehouse.ToList())
        {
            if (entry is null || entry.Quantity <= 0)
                Mod.State.Warehouse.Remove(key);
        }
    }

    private static string WarehouseKey(string itemId, int quality) => $"{quality}:{itemId}";

    internal static string QualityName(int quality) => quality switch
    {
        1 => "은",
        2 => "금",
        4 => "이리듐",
        _ => "일반"
    };

    internal static string GetStageName(int level) => level switch
    {
        <= 1 => "개인 농장",
        2 => "소규모 농장",
        3 => "농산물 공방",
        4 => "농업 회사",
        _ => "지역 대표 기업"
    };

    internal static int GetLevelStartXp(int level) => level switch
    {
        <= 1 => 0,
        2 => 50,
        3 => 150,
        4 => 400,
        _ => 900
    };

    internal static int GetNextLevelXp(int level) => level switch
    {
        <= 1 => 50,
        2 => 150,
        3 => 400,
        4 => 900,
        _ => 900
    };

    private static void UpdateLevel(CompanySaveData company)
    {
        int xp = Math.Max(0, company.Experience);
        company.Level = xp >= 900 ? 5 : xp >= 400 ? 4 : xp >= 150 ? 3 : xp >= 50 ? 2 : 1;
    }

    private static void Add(Dictionary<string, int> values, string id, int amount)
    {
        values.TryGetValue(id, out int old);
        values[id] = old + amount;
    }
}
