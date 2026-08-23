namespace AgriculturalCompany;

public sealed class ModConfig
{
    public string CompanyKey { get; set; } = "F7";
}

public sealed class CompanySaveData
{
    public string CompanyName { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int Reputation { get; set; }
    public int ContractsCompleted { get; set; }
    public int ActiveContracts { get; set; }
    public long LifetimeRevenue { get; set; }
    public long SeasonRevenue { get; set; }
    public string LastDayKey { get; set; } = "";
    public string SeasonKey { get; set; } = "";
    public Dictionary<string, int> TodayHarvest { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> SeasonHarvest { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> LifetimeHarvest { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, WarehouseStockEntry> Warehouse { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public long LifetimeDeposited { get; set; }
    public long LifetimeWithdrawn { get; set; }

    // 0.3 production data. Kept inside the same save payload so 0.1/0.2 saves migrate automatically.
    public List<ProductionJob> ProductionQueue { get; set; } = new();
    public Dictionary<string, ProductStockEntry> FinishedGoods { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public long LifetimeProductionBatches { get; set; }
    public long LifetimeFinishedGoods { get; set; }
}

public sealed class TrackedCropDefinition
{
    public string ItemId { get; set; } = "";
    public string Family { get; set; } = "Other";
    public string DisplayName { get; set; } = "";
    public string FamilyDisplayName { get; set; } = "";
}

public sealed class WarehouseStockEntry
{
    public string ItemId { get; set; } = "";
    public int Quality { get; set; }
    public int Quantity { get; set; }
}

public sealed class ProductionRecipeDefinition
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string IngredientItemId { get; set; } = "";
    public string IngredientFamily { get; set; } = "";
    public int InputQuantity { get; set; } = 1;
    public int OutputQuantity { get; set; } = 1;
    public int DurationMinutes { get; set; } = 60;
    public int RequiredCompanyLevel { get; set; } = 1;
}

public sealed class ProductionJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RecipeKey { get; set; } = "";
    public int BatchCount { get; set; } = 1;
    public int OutputQuality { get; set; }
    public int RemainingMinutes { get; set; }
    public int TotalMinutes { get; set; }
}

public sealed class ProductStockEntry
{
    public string ProductKey { get; set; } = "";
    public int Quality { get; set; }
    public int Quantity { get; set; }
}
