using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace JunimoCards;

public sealed class ModEntry : Mod
{
    private const string SaveKey = "junimo-cards-state";
    private readonly Random Rng = new();

    internal ModConfig Config { get; private set; } = new();
    internal CardSaveData State { get; private set; } = new();
    internal List<CardDefinition> Cards { get; private set; } = new();
    internal CardVisualOverlay VisualOverlay { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Cards = helper.Data.ReadJsonFile<List<CardDefinition>>("data/cards.json") ?? new();
        Cards = Cards.Where(p => !string.IsNullOrWhiteSpace(p.Key)).GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => p.First()).ToList();

        VisualOverlay = new CardVisualOverlay(this);
        VisualOverlay.Initialize(helper);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log($"Junimo Cards 0.2.0 loaded with {Cards.Count} Pelican Origins cards. Five featured cards use illustrated art. {Config.OpenKey} opens the card shop.", LogLevel.Info);
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
            Helper.Data.WriteSaveData(SaveKey, State);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;
        EnsureState();
        ProcessDailyCustomers();
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
        Game1.activeClickableMenu = new CardShopHomeMenu(this);
        Game1.playSound("bigSelect");
    }

    internal void EnsureState()
    {
        State ??= new CardSaveData();
        State.Collection ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        State.SaleShelf ??= new List<SaleListing>();
        State.LastDailySalesSummary ??= "오늘은 아직 카드샵 영업 전입니다.";
        State.SaleShelf.RemoveAll(p => string.IsNullOrWhiteSpace(p.CollectionKey));
        if (State.SaleShelf.Count > Config.SaleShelfSlots)
            State.SaleShelf = State.SaleShelf.Take(Config.SaleShelfSlots).ToList();
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
        message = pity ? "천장 발동! Rare 이상 카드가 보장되었습니다." : "팩을 개봉합니다. 카드를 한 장씩 확인하세요.";
        return true;
    }

    private CardDefinition RollCard(bool guaranteeUncommon, bool forceRare)
    {
        string rarity;
        double roll = Rng.NextDouble() * 100.0;
        if (forceRare)
        {
            rarity = roll < 70 ? "Rare" : roll < 91 ? "Epic" : roll < 98 ? "Legendary" : "Secret";
        }
        else if (guaranteeUncommon)
        {
            rarity = roll < 60 ? "Uncommon" : roll < 85 ? "Rare" : roll < 95 ? "Epic" : roll < 99 ? "Legendary" : "Secret";
        }
        else
        {
            rarity = roll < 68 ? "Common" : roll < 90 ? "Uncommon" : roll < 97 ? "Rare" : roll < 99.2 ? "Epic" : roll < 99.85 ? "Legendary" : "Secret";
        }

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
        if (amount == 0) return;
        State.Collection.TryGetValue(collectionKey, out int count);
        int next = count + amount;
        if (next <= 0) State.Collection.Remove(collectionKey);
        else State.Collection[collectionKey] = next;
    }

    internal int GetOwned(string collectionKey)
        => State.Collection.TryGetValue(collectionKey, out int count) ? count : 0;

    internal int GetListedCount(string collectionKey)
        => State.SaleShelf.Count(p => string.Equals(p.CollectionKey, collectionKey, StringComparison.OrdinalIgnoreCase));

    internal bool TryListForSale(string collectionKey, out string message)
    {
        if (!CardKeys.TryParse(collectionKey, out string cardKey, out string variant, out string condition))
        {
            message = "카드 정보를 읽을 수 없습니다.";
            return false;
        }
        CardDefinition? card = FindCard(cardKey);
        if (card is null)
        {
            message = "카드 데이터를 찾을 수 없습니다.";
            return false;
        }
        if (State.SaleShelf.Count >= Config.SaleShelfSlots)
        {
            message = $"판매 진열대가 가득 찼습니다. ({Config.SaleShelfSlots}칸)";
            return false;
        }
        int available = GetOwned(collectionKey) - GetListedCount(collectionKey);
        if (available <= 0)
        {
            message = "판매 가능한 여분 카드가 없습니다.";
            return false;
        }

        int price = GetMarketValue(card, variant, condition);
        State.SaleShelf.Add(new SaleListing { CollectionKey = collectionKey, Price = price });
        message = $"{card.Name} {VariantName(variant)}를 {price:N0}G에 진열했습니다.";
        Game1.playSound("smallSelect");
        return true;
    }

    internal void RemoveListing(int index)
    {
        if (index < 0 || index >= State.SaleShelf.Count) return;
        State.SaleShelf.RemoveAt(index);
        Game1.playSound("smallSelect");
    }

    private void ProcessDailyCustomers()
    {
        if (State.SaleShelf.Count == 0)
        {
            State.LastDailySalesSummary = "오늘은 판매 진열대가 비어 있어 손님 판매가 없었습니다.";
            return;
        }

        int sold = 0;
        int revenue = 0;
        List<string> names = new();
        for (int i = State.SaleShelf.Count - 1; i >= 0 && sold < Config.MaxDailySales; i--)
        {
            SaleListing listing = State.SaleShelf[i];
            if (GetOwned(listing.CollectionKey) <= 0)
            {
                State.SaleShelf.RemoveAt(i);
                continue;
            }
            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out _))
                continue;
            CardDefinition? card = FindCard(cardKey);
            if (card is null) continue;

            double chance = 0.48 + GetRarityRank(card.Rarity) * 0.045;
            if (variant == "Holo") chance += 0.04;
            if (variant == "Gold") chance += 0.07;
            if (variant == "Rainbow") chance += 0.10;
            chance = Math.Min(0.88, chance);
            if (Rng.NextDouble() > chance)
                continue;

            AddOwned(listing.CollectionKey, -1);
            Game1.player.Money += listing.Price;
            revenue += listing.Price;
            sold++;
            names.Add(card.Name);
            State.SaleShelf.RemoveAt(i);
        }

        State.LifetimeCardRevenue += revenue;
        State.LastDailySalesSummary = sold == 0
            ? "오늘 손님들은 구경만 하고 돌아갔습니다. 내일 다시 기대해 보세요."
            : $"오늘 {sold}장 판매 · +{revenue:N0}G ({string.Join(", ", names)})";
    }

    internal IEnumerable<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> GetCollectionRows()
    {
        foreach (var pair in State.Collection)
        {
            if (pair.Value <= 0) continue;
            if (!CardKeys.TryParse(pair.Key, out string cardKey, out string variant, out string condition)) continue;
            CardDefinition? card = FindCard(cardKey);
            if (card is null) continue;
            yield return (pair.Key, card, variant, condition, pair.Value, GetMarketValue(card, variant, condition));
        }
    }

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
