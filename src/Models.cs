namespace WatermelonGeneticsCore;

public sealed class ModConfig
{
    public string CodexKey { get; set; } = "F8";
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
