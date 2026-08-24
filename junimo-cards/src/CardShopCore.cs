using StardewValley;

namespace JunimoCards;

internal sealed class CardShopCore
{
    private readonly ModEntry Mod;
    private readonly Random Rng = new();

    private static readonly (int UniqueCards, int RewardPacks)[] BonusMilestones =
    {
        (5, 1), (10, 2), (20, 3), (30, 5)
    };

    internal CardShopCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void EnsureState()
    {
        Mod.State ??= new CardSaveData();
        Mod.State.Collection ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Mod.State.SaleShelf ??= new List<SaleListing>();
        Mod.State.ClaimedCollectionBonuses ??= new List<int>();
        Mod.State.SalesHistory ??= new List<DailySaleRecord>();
        Mod.State.LastDailySalesSummary ??= "오늘은 아직 카드샵 영업 전입니다.";

        Mod.State.SaleShelf.RemoveAll(p => p is null || string.IsNullOrWhiteSpace(p.CollectionKey));

        // Migrate the old list-only shelf into stable 0..7 display slots.
        HashSet<int> used = new();
        foreach (SaleListing listing in Mod.State.SaleShelf)
        {
            if (listing.Slot < 0 || listing.Slot >= Mod.Config.SaleShelfSlots || !used.Add(listing.Slot))
            {
                listing.Slot = FirstFreeSlot(used);
                if (listing.Slot >= 0)
                    used.Add(listing.Slot);
            }

            if (CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out string condition))
            {
                CardDefinition? card = Mod.FindCard(cardKey);
                if (card is not null)
                {
                    int suggested = Mod.GetMarketValue(card, variant, condition);
                    if (listing.Price <= 0)
                        listing.Price = suggested;
                    listing.Price = Math.Clamp(listing.Price, GetMinimumPrice(suggested), suggested);
                }
            }
        }

        Mod.State.SaleShelf.RemoveAll(p => p.Slot < 0 || p.Slot >= Mod.Config.SaleShelfSlots);
        if (Mod.State.SaleShelf.Count > Mod.Config.SaleShelfSlots)
            Mod.State.SaleShelf = Mod.State.SaleShelf.OrderBy(p => p.Slot).Take(Mod.Config.SaleShelfSlots).ToList();

        Mod.State.SalesHistory = Mod.State.SalesHistory
            .OrderByDescending(p => p.Day)
            .Take(90)
            .ToList();
    }

    private int FirstFreeSlot(HashSet<int>? used = null)
    {
        used ??= Mod.State.SaleShelf.Select(p => p.Slot).ToHashSet();
        for (int slot = 0; slot < Mod.Config.SaleShelfSlots; slot++)
            if (!used.Contains(slot))
                return slot;
        return -1;
    }

    internal int UniqueCardCount()
    {
        return Mod.State.Collection
            .Where(p => p.Value > 0)
            .Select(p => CardKeys.TryParse(p.Key, out string cardKey, out _, out _) ? cardKey : "")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    internal int TotalOwnedCopies() => Mod.State.Collection.Values.Where(p => p > 0).Sum();

    internal int UniqueCountForRarity(string rarity)
    {
        return Mod.State.Collection
            .Where(p => p.Value > 0)
            .Select(p => CardKeys.TryParse(p.Key, out string cardKey, out _, out _) ? Mod.FindCard(cardKey) : null)
            .Where(p => p is not null && string.Equals(p.Rarity, rarity, StringComparison.OrdinalIgnoreCase))
            .Select(p => p!.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    internal List<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> GetCollectionRows(string rarityFilter = "All")
    {
        IEnumerable<(string CollectionKey, CardDefinition Card, string Variant, string Condition, int Count, int Value)> rows = Mod.GetCollectionRowsRaw();
        if (!string.Equals(rarityFilter, "All", StringComparison.OrdinalIgnoreCase))
            rows = rows.Where(p => string.Equals(p.Card.Rarity, rarityFilter, StringComparison.OrdinalIgnoreCase));

        return rows
            .OrderByDescending(p => ModEntry.GetRarityRank(p.Card.Rarity))
            .ThenBy(p => p.Card.SetNo)
            .ThenBy(p => p.Variant)
            .ThenBy(p => p.Condition)
            .ToList();
    }

    internal (int Required, int Reward, bool CanClaim, bool Complete) GetNextCollectionBonus()
    {
        int unique = UniqueCardCount();
        foreach ((int required, int reward) in BonusMilestones)
        {
            bool claimed = Mod.State.ClaimedCollectionBonuses.Contains(required);
            if (!claimed)
                return (required, reward, unique >= required, false);
        }
        return (30, 0, false, true);
    }

    internal bool TryClaimCollectionBonus(out string message)
    {
        int unique = UniqueCardCount();
        foreach ((int required, int reward) in BonusMilestones)
        {
            if (Mod.State.ClaimedCollectionBonuses.Contains(required))
                continue;
            if (unique < required)
            {
                message = $"컬렉션 보너스: {required}종 수집 시 카드팩 {reward}개 · 현재 {unique}/{required}";
                return false;
            }

            Mod.State.ClaimedCollectionBonuses.Add(required);
            Mod.State.UnopenedPacks += reward;
            message = $"컬렉션 {required}종 달성! Pelican Origins 팩 {reward}개를 받았습니다.";
            Game1.playSound("reward");
            return true;
        }

        message = "Pelican Origins 컬렉션 보너스를 모두 받았습니다.";
        return false;
    }

    internal SaleListing? GetListingAtSlot(int slot)
        => Mod.State.SaleShelf.FirstOrDefault(p => p.Slot == slot);

    internal IReadOnlyList<SaleListing?> GetShelfSlots()
    {
        SaleListing?[] slots = new SaleListing?[Mod.Config.SaleShelfSlots];
        foreach (SaleListing listing in Mod.State.SaleShelf)
            if (listing.Slot >= 0 && listing.Slot < slots.Length)
                slots[listing.Slot] = listing;
        return slots;
    }

    internal int GetListedCount(string collectionKey)
        => Mod.State.SaleShelf.Count(p => string.Equals(p.CollectionKey, collectionKey, StringComparison.OrdinalIgnoreCase));

    internal bool TryListForSale(string collectionKey, out string message)
    {
        int slot = FirstFreeSlot();
        if (slot < 0)
        {
            message = $"판매 진열대가 가득 찼습니다. ({Mod.Config.SaleShelfSlots}/{Mod.Config.SaleShelfSlots})";
            return false;
        }
        return TryListForSaleAtSlot(collectionKey, slot, out message);
    }

    internal bool TryListForSaleAtSlot(string collectionKey, int slot, out string message)
    {
        if (slot < 0 || slot >= Mod.Config.SaleShelfSlots)
        {
            message = "유효하지 않은 진열 슬롯입니다.";
            return false;
        }
        if (GetListingAtSlot(slot) is not null)
        {
            message = $"{slot + 1}번 진열 슬롯은 이미 사용 중입니다.";
            return false;
        }
        if (!CardKeys.TryParse(collectionKey, out string cardKey, out string variant, out string condition))
        {
            message = "카드 정보를 읽을 수 없습니다.";
            return false;
        }
        CardDefinition? card = Mod.FindCard(cardKey);
        if (card is null)
        {
            message = "카드 데이터를 찾을 수 없습니다.";
            return false;
        }

        int available = Mod.GetOwned(collectionKey) - GetListedCount(collectionKey);
        if (available <= 0)
        {
            message = "판매 가능한 여분 카드가 없습니다.";
            return false;
        }

        int price = Mod.GetMarketValue(card, variant, condition);
        Mod.State.SaleShelf.Add(new SaleListing
        {
            Slot = slot,
            CollectionKey = collectionKey,
            Price = price,
            ListedDay = CurrentDay()
        });
        message = $"{slot + 1}번 슬롯에 {card.Name} · {ModEntry.VariantName(variant)}를 {price:N0}G로 진열했습니다.";
        Game1.playSound("smallSelect");
        return true;
    }

    internal bool TrySetListingPrice(int slot, int price, out string message)
    {
        SaleListing? listing = GetListingAtSlot(slot);
        if (listing is null)
        {
            message = "선택한 슬롯에 카드가 없습니다.";
            return false;
        }
        if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out string condition))
        {
            message = "카드 정보를 읽을 수 없습니다.";
            return false;
        }
        CardDefinition? card = Mod.FindCard(cardKey);
        if (card is null)
        {
            message = "카드 데이터를 찾을 수 없습니다.";
            return false;
        }

        int max = Mod.GetMarketValue(card, variant, condition);
        int min = GetMinimumPrice(max);
        int normalized = Math.Clamp((int)Math.Round(price / 50d) * 50, min, max);
        listing.Price = normalized;
        message = $"{slot + 1}번 슬롯 진열가를 {normalized:N0}G로 변경했습니다. (범위 {min:N0}~{max:N0}G)";
        Game1.playSound("smallSelect");
        return true;
    }

    internal bool TryAdjustListingPrice(int slot, int direction, out string message)
    {
        SaleListing? listing = GetListingAtSlot(slot);
        if (listing is null)
        {
            message = "선택한 슬롯에 카드가 없습니다.";
            return false;
        }
        return TrySetListingPrice(slot, listing.Price + Math.Sign(direction) * 50, out message);
    }

    internal bool RemoveListingBySlot(int slot, out string message)
    {
        SaleListing? listing = GetListingAtSlot(slot);
        if (listing is null)
        {
            message = "선택한 슬롯은 비어 있습니다.";
            return false;
        }
        Mod.State.SaleShelf.Remove(listing);
        message = $"{slot + 1}번 슬롯의 카드를 회수했습니다.";
        Game1.playSound("smallSelect");
        return true;
    }

    internal void RemoveListingByListIndex(int index)
    {
        if (index < 0 || index >= Mod.State.SaleShelf.Count)
            return;
        Mod.State.SaleShelf.RemoveAt(index);
        Game1.playSound("smallSelect");
    }

    internal int SuggestedPrice(string collectionKey)
    {
        if (!CardKeys.TryParse(collectionKey, out string cardKey, out string variant, out string condition))
            return 0;
        CardDefinition? card = Mod.FindCard(cardKey);
        return card is null ? 0 : Mod.GetMarketValue(card, variant, condition);
    }

    private static int GetMinimumPrice(int market)
        => Math.Max(50, (int)Math.Round((market * 0.50) / 50d) * 50);

    internal void ProcessDailyCustomers()
    {
        EnsureState();
        int day = CurrentDay();
        if (Mod.State.LastProcessedSalesDay == day)
            return;
        Mod.State.LastProcessedSalesDay = day;
        Mod.State.LastCustomerCount = 0;
        Mod.State.LastCardsSold = 0;
        Mod.State.LastDailyRevenue = 0;

        List<SaleListing> available = Mod.State.SaleShelf
            .Where(p => Mod.GetOwned(p.CollectionKey) > 0)
            .OrderBy(p => p.Slot)
            .ToList();

        if (available.Count == 0)
        {
            Mod.State.LastDailySalesSummary = "오늘은 판매 진열대가 비어 있어 손님 판매가 없었습니다.";
            return;
        }

        int visitorLimit = Math.Min(Mod.Config.MaxDailySales, available.Count);
        int visitors = visitorLimit;
        int sold = 0;
        int revenue = 0;
        List<string> soldNames = new();

        for (int customer = 0; customer < visitors; customer++)
        {
            available = available.Where(p => Mod.State.SaleShelf.Contains(p) && Mod.GetOwned(p.CollectionKey) > 0).ToList();
            if (available.Count == 0)
                break;

            SaleListing target = PickCustomerTarget(available);
            Mod.State.LastCustomerCount++;
            double chance = GetSaleChance(target);
            if (Rng.NextDouble() > chance)
                continue;

            if (!CardKeys.TryParse(target.CollectionKey, out string cardKey, out string variant, out string condition))
                continue;
            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null)
                continue;

            Mod.AddOwned(target.CollectionKey, -1);
            Game1.player.Money += target.Price;
            revenue += target.Price;
            sold++;
            soldNames.Add(card.Name);
            Mod.State.SalesHistory.Insert(0, new DailySaleRecord
            {
                Day = day,
                CardKey = cardKey,
                Variant = variant,
                Condition = condition,
                Slot = target.Slot,
                Price = target.Price
            });
            Mod.State.SaleShelf.Remove(target);
        }

        Mod.State.LastCardsSold = sold;
        Mod.State.LastDailyRevenue = revenue;
        Mod.State.LifetimeCardRevenue += revenue;
        Mod.State.SalesHistory = Mod.State.SalesHistory.Take(90).ToList();

        Mod.State.LastDailySalesSummary = sold == 0
            ? $"오늘 손님 {Mod.State.LastCustomerCount}명이 방문했지만 구매 없이 돌아갔습니다."
            : $"오늘 손님 {Mod.State.LastCustomerCount}명 · {sold}장 판매 · +{revenue:N0}G ({string.Join(", ", soldNames)})";
    }

    private SaleListing PickCustomerTarget(List<SaleListing> listings)
    {
        double total = listings.Sum(GetAppealWeight);
        if (total <= 0)
            return listings[Rng.Next(listings.Count)];

        double roll = Rng.NextDouble() * total;
        foreach (SaleListing listing in listings)
        {
            roll -= GetAppealWeight(listing);
            if (roll <= 0)
                return listing;
        }
        return listings[^1];
    }

    private double GetAppealWeight(SaleListing listing)
    {
        if (!TryResolveListing(listing, out CardDefinition? card, out string variant, out string condition, out int market))
            return 0.1;

        double quality = 1.0 + ModEntry.GetRarityRank(card!.Rarity) * 0.65;
        quality += variant switch { "Holo" => 0.55, "Gold" => 1.10, "Rainbow" => 1.85, _ => 0.0 };
        quality += condition switch { "Mint" => 0.55, "Near Mint" => 0.25, _ => 0.0 };
        double priceRatio = market <= 0 ? 1.0 : listing.Price / (double)market;
        double valueFactor = 1.45 - 0.55 * priceRatio;
        return Math.Max(0.1, quality * valueFactor);
    }

    internal double GetSaleChance(SaleListing listing)
    {
        if (!TryResolveListing(listing, out CardDefinition? card, out string variant, out string condition, out int market))
            return 0.15;

        double chance = 0.34 + ModEntry.GetRarityRank(card!.Rarity) * 0.055;
        chance += variant switch { "Holo" => 0.045, "Gold" => 0.085, "Rainbow" => 0.14, _ => 0.0 };
        chance += condition switch { "Mint" => 0.055, "Near Mint" => 0.025, _ => -0.02 };
        double priceRatio = market <= 0 ? 1.0 : listing.Price / (double)market;
        chance += (1.0 - priceRatio) * 0.35;
        return Math.Clamp(chance, 0.18, 0.92);
    }

    private bool TryResolveListing(SaleListing listing, out CardDefinition? card, out string variant, out string condition, out int market)
    {
        card = null;
        variant = condition = "";
        market = 0;
        if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out variant, out condition))
            return false;
        card = Mod.FindCard(cardKey);
        if (card is null)
            return false;
        market = Mod.GetMarketValue(card, variant, condition);
        return true;
    }

    private static int CurrentDay() => (int)Game1.stats.DaysPlayed;
}
