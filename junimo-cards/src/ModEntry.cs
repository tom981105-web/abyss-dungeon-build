using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace JunimoCards;

public sealed class ModEntry : Mod
{
    private const string SaveKey = "junimo-cards-state";
    private readonly Random Rng = new();

    internal ModConfig Config { get; private set; } = new();
    internal CardSaveData State { get; set; } = new();
    internal List<CardDefinition> Cards { get; private set; } = new();
    internal CardVisualOverlay VisualOverlay { get; private set; } = null!;
    internal CardShopCore Core { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Cards = helper.Data.ReadJsonFile<List<CardDefinition>>("data/cards.json") ?? new();
        Cards = Cards.Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => p.First())
            .ToList();

        Core = new CardShopCore(this);
        VisualOverlay = new CardVisualOverlay(this);
        VisualOverlay.Initialize(helper);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log($"Junimo Cards 0.3.4 bigger-text + wow reveal flow loaded with {Cards.Count} Pelican Origins cards. {Config.OpenKey} opens the card shop.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        State = Context.IsMainPlayer
            ? Helper.Data.ReadSaveData<CardSaveData>(SaveKey) ?? new CardSaveData()
            : new CardSaveData();
        EnsureState();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            EnsureState();
            Helper.Data.WriteSaveData(SaveKey, State);
        }
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;
        EnsureState();
        Core.ProcessDailyCustomers();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null)
            return;
        if (!Enum.TryParse<SButton>(Config.OpenKey, true, out SButton key) || e.Button != key)
            return;

        Helper.Input.Suppress(e.Button);
        if (!Context.IsMainPlayer)
        {
            Monitor.Log("Junimo Cards card-shop ownership is currently handled by the main player only.", LogLevel.Info);
            return;
        }

        EnsureState();
        Game1.activeClickableMenu = new ReadableCardShopMenu032(this);
        Game1.playSound("bigSelect");
    }

    internal void EnsureState()
    {
        Core.EnsureState();
        CardShopRules.NormalizeShelf(this);
    }

    internal CardDefinition? FindCard(string key)
        => Cards.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));

    internal bool TryBuyPacks(int count, out string message)
    {
        count = count <= 1 ? 1 : 5;
        int price = count == 1 ? Config.PackPrice : Config.FivePackPrice;
        if (Game1.player.Money < price)
        {
            message = $"골드가 부족합니다. {price:N0}G 필요";
            Game1.playSound("cancel");
            return false;
        }

        Game1.player.Money -= price;
        State.UnopenedPacks += count;
        message = $"Pelican Origins 팩 {count}개 구매! 보유 팩 {State.UnopenedPacks}개";
        Game1.playSound("purchase");
        return true;
    }

    internal bool TryOpenPack(out List<CardPull> pulls, out string message)
    {
        pulls = new List<CardPull>();
        if (State.UnopenedPacks <= 0)
        {
            message = "먼저 카드팩을 구매하세요.";
            return false;
        }
        if (Cards.Count == 0)
        {
            message = "카드 데이터가 없습니다.";
            return false;
        }

        State.UnopenedPacks--;
        State.PacksOpened++;
        bool pity = State.PacksSinceRare >= 9;
        bool gotRarePlus = false;

        for (int i = 0; i < 5; i++)
        {
            bool guaranteeUncommon = i == 4;
            bool forceRare = pity && i == 4;
            CardDefinition card = RollCard(guaranteeUncommon, forceRare);
            string variant = RollVariant();
            string condition = RollCondition();
            CardPull pull = new()
            {
                CardKey = card.Key,
                Variant = variant,
                Condition = condition,
                MarketValue = GetMarketValue(card, variant, condition)
            };
            pulls.Add(pull);
            AddOwned(pull.CollectionKey, 1);
            if (GetRarityRank(card.Rarity) >= GetRarityRank("Rare"))
                gotRarePlus = true;
        }

        State.PacksSinceRare = gotRarePlus ? 0 : State.PacksSinceRare + 1;
        message = pity
            ? "천장 발동! Rare 이상 카드가 보장되었습니다."
            : "카드를 클릭해서 한 장씩 공개하세요.";
        return true;
    }

    private CardDefinition RollCard(bool guaranteeUncommon, bool forceRare)
    {
        string rarity;
        double roll = Rng.NextDouble() * 100.0;
        if (forceRare)
            rarity = roll < 70 ? "Rare" : roll < 91 ? "Epic" : roll < 98 ? "Legendary" : "Secret";
        else if (guaranteeUncommon)
            rarity = roll < 60 ? "Uncommon" : roll < 85 ? "Rare" : roll < 95 ? "Epic" : roll < 99 ? "Legendary" : "Secret";
        else
            rarity = roll < 68 ? "Common" : roll < 90 ? "Uncommon" : roll < 97 ? "Rare" : roll < 99.2 ? "Epic" : roll < 99.85 ? "Legendary" : "Secret";

        List<CardDefinition> pool = Cards.Where(p => string.Equals(p.Rarity, rarity, StringComparison.OrdinalIgnoreCase)).ToList();
        if (pool.Count == 0)
            pool = Cards;
        return pool[Rng.Next(pool.Count)];
    }

    private string RollVariant()
    {
        double r = Rng.NextDouble() * 100.0;
        if (r < 82) return "Normal";
        if (r < 94) return "Holo";
        if (r < 99) return "Gold";
        return "Rainbow";
    }

    private string RollCondition()
    {
        double r = Rng.NextDouble() * 100.0;
        if (r < 10) return "Good";
        if (r < 82) return "Near Mint";
        return "Mint";
    }

    internal int GetMarketValue(CardDefinition card, string variant, string condition)
    {
        double variantMult = variant switch
        {
            "Holo" => 2.0,
            "Gold" => 5.0,
            "Rainbow" => 12.0,
            _ => 1.0
        };
        double conditionMult = condition switch
        {
            "Good" => 0.75,
            "Mint" => 1.30,
            _ => 1.0
        };
        return Math.Max(10, (int)Math.Round(card.BaseValue * variantMult * conditionMult / 10.0) * 10);
    }

    internal void AddOwned(string collectionKey, int amount)
    {
        if (amount == 0)
            return;
        State.Collection.TryGetValue(collectionKey, out int count);
        int next = count + amount;
        if (next <= 0)
            State.Collection.Remove(collectionKey);
        else
            State.Collection[collectionKey] = next;
    }

    internal int GetOwned(string collectionKey)
        => State.Collection.TryGetValue(collectionKey, out int count) ? count : 0;

    internal int GetListedCount(string collectionKey) => Core.GetListedCount(collectionKey);
    internal bool TryListForSale(string collectionKey, out string message) => Core.TryListForSale(collectionKey, out message);
    internal void RemoveListing(int index) => Core.RemoveListingByListIndex(index);

    internal IEnumerable<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> GetCollectionRowsRaw()
    {
        foreach (var pair in State.Collection)
        {
            if (pair.Value <= 0)
                continue;
            if (!CardKeys.TryParse(pair.Key, out string cardKey, out string variant, out string condition))
                continue;
            CardDefinition? card = FindCard(cardKey);
            if (card is null)
                continue;
            yield return (pair.Key, card, variant, condition, pair.Value, GetMarketValue(card, variant, condition));
        }
    }

    internal IEnumerable<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> GetCollectionRows()
        => Core.GetCollectionRows("All");

    internal static int GetRarityRank(string rarity) => rarity switch
    {
        "Uncommon" => 1,
        "Rare" => 2,
        "Epic" => 3,
        "Legendary" => 4,
        "Secret" => 5,
        _ => 0
    };

    internal static string RarityName(string rarity) => rarity switch
    {
        "Common" => "커먼",
        "Uncommon" => "언커먼",
        "Rare" => "레어",
        "Epic" => "에픽",
        "Legendary" => "레전더리",
        "Secret" => "시크릿",
        _ => rarity
    };

    internal static string VariantName(string variant) => variant switch
    {
        "Normal" => "일반",
        "Holo" => "홀로",
        "Gold" => "골드",
        "Rainbow" => "레인보우",
        _ => variant
    };
}
