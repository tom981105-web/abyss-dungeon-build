namespace WatermelonGeneticsCore;

public sealed class ModConfig
{
    public string CodexKey { get; set; } = "F8";
    public string CompanyKey { get; set; } = "F7";
    public int ResearchDays { get; set; } = 2;
}

public sealed class VarietyDefinition
{
    public string Key { get; set; } = "";
    public string NameKey { get; set; } = "";
    public string SeedId { get; set; } = "";
    public string FruitId { get; set; } = "";
    public int Sweetness { get; set; }
    public int Size { get; set; }
    public int Growth { get; set; }
    public int Resistance { get; set; }
    public int Rarity { get; set; }
}

public sealed class GeneticsSaveData
{
    public HashSet<string> Discovered { get; set; } = new(StringComparer.OrdinalIgnoreCase) { "Common" };
    public Dictionary<string, HybridJob> Jobs { get; set; } = new();
    public Dictionary<string, VarietyRecord> Records { get; set; } = new();
    public CompanySaveData Company { get; set; } = new();
}

public sealed class HybridJob
{
    public string LocationName { get; set; } = "";
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string ParentA { get; set; } = "";
    public string ParentB { get; set; } = "";
    public string ResultItemId { get; set; } = "";
    public int DaysRemaining { get; set; } = 2;
}

public sealed class VarietyRecord
{
    public int TimesBred { get; set; }
    public int TimesFound { get; set; }
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
}

public sealed class CompanyCropDefinition
{
    public string ItemId { get; init; } = "";
    public string Family { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string FamilyDisplayName { get; init; } = "";
}
