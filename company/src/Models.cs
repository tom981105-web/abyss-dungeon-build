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
