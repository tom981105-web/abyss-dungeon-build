namespace JunimoCards;

internal sealed class ModConfig
{
    private string OpenKeyValue = "F9";

    public string OpenKey
    {
        get => OpenKeyValue;
        set
        {
            string requested = string.IsNullOrWhiteSpace(value) ? "F9" : value.Trim();
            OpenKeyValue = string.Equals(requested, "F8", StringComparison.OrdinalIgnoreCase) ? "F9" : requested;
        }
    }

    public int PackPrice { get; set; } = 650;
    public int FivePackPrice { get; set; } = 3000;
    public int SaleShelfSlots { get; set; } = 8;
    public int MaxDailySales { get; set; } = 3;
}

internal sealed class CardDefinition
{
    public string Key { get; set; } = "";
    public string SetNo { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Rarity { get; set; } = "Common";
    public int BaseValue { get; set; }
    public string Flavor { get; set; } = "";
}

internal sealed class CardPull
{
    public string CardKey { get; set; } = "";
    public string Variant { get; set; } = "Normal";
    public string Condition { get; set; } = "Near Mint";
    public int MarketValue { get; set; }

    public string CollectionKey => CardKeys.Compose(CardKey, Variant, Condition);
}

internal sealed class SaleListing
{
    public int Slot { get; set; } = -1;
    public string CollectionKey { get; set; } = "";
    public int Price { get; set; }
    public int ListedDay { get; set; }
}

internal sealed class DailySaleRecord
{
    public int Day { get; set; }
    public string CardKey { get; set; } = "";
    public string Variant { get; set; } = "Normal";
    public string Condition { get; set; } = "Near Mint";
    public int Slot { get; set; }
    public int Price { get; set; }
}

internal sealed class CardSaveData
{
    public int UnopenedPacks { get; set; }
    public int PacksOpened { get; set; }
    public int PacksSinceRare { get; set; }
    public long LifetimeCardRevenue { get; set; }
    public Dictionary<string, int> Collection { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SaleListing> SaleShelf { get; set; } = new();

    // v0.3.0 feature-complete shop state.
    public List<int> ClaimedCollectionBonuses { get; set; } = new();
    public List<DailySaleRecord> SalesHistory { get; set; } = new();
    public int LastProcessedSalesDay { get; set; } = -1;
    public int LastCustomerCount { get; set; }
    public int LastCardsSold { get; set; }
    public int LastDailyRevenue { get; set; }
    public string LastDailySalesSummary { get; set; } = "오늘은 아직 카드샵 영업 전입니다.";
}

internal static class CardKeys
{
    private const char Sep = '|';

    internal static string Compose(string cardKey, string variant, string condition)
        => $"{cardKey}{Sep}{variant}{Sep}{condition}";

    internal static bool TryParse(string key, out string cardKey, out string variant, out string condition)
    {
        string[] parts = (key ?? "").Split(Sep);
        if (parts.Length != 3)
        {
            cardKey = variant = condition = "";
            return false;
        }
        cardKey = parts[0];
        variant = parts[1];
        condition = parts[2];
        return true;
    }
}
