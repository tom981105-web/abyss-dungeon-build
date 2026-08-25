namespace JunimoCards;

/// <summary>
/// Gameplay invariants that sit above the persistent shelf engine.
/// The collection always keeps at least one physical copy of every collected base card,
/// while any remaining copies may be placed on the sale shelf.
/// </summary>
internal static class CardShopRules
{
    internal static int GetListableCount(ModEntry mod, string collectionKey)
    {
        if (!CardKeys.TryParse(collectionKey, out string cardKey, out _, out _))
            return 0;

        int ownedThisCopy = mod.GetOwned(collectionKey);
        int listedThisCopy = mod.GetListedCount(collectionKey);
        int unlistedThisCopy = Math.Max(0, ownedThisCopy - listedThisCopy);

        int ownedBaseCard = CountOwnedBaseCard(mod, cardKey);
        int listedBaseCard = CountListedBaseCard(mod, cardKey);
        int remainingShelfCapacity = Math.Max(0, ownedBaseCard - 1 - listedBaseCard);

        return Math.Max(0, Math.Min(unlistedThisCopy, remainingShelfCapacity));
    }

    internal static bool TryListForSale(ModEntry mod, string collectionKey, int slot, out string message)
    {
        if (GetListableCount(mod, collectionKey) <= 0)
        {
            message = "컬렉션 보관용 1장을 제외한 여분 카드가 없습니다.";
            return false;
        }

        return slot >= 0
            ? mod.Core.TryListForSaleAtSlot(collectionKey, slot, out message)
            : mod.Core.TryListForSale(collectionKey, out message);
    }

    /// <summary>
    /// Repairs older saves which could have every owned copy listed for sale.
    /// Listings are trimmed, not collection cards, so upgrading never destroys a card.
    /// </summary>
    internal static void NormalizeShelf(ModEntry mod)
    {
        if (mod.State.SaleShelf.Count == 0)
            return;

        Dictionary<string, int> keptPerBase = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> keptPerCopy = new(StringComparer.OrdinalIgnoreCase);
        List<SaleListing> keep = new();

        foreach (SaleListing listing in mod.State.SaleShelf.OrderBy(p => p.Slot))
        {
            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out _, out _))
                continue;

            int ownedBase = CountOwnedBaseCard(mod, cardKey);
            int ownedCopy = mod.GetOwned(listing.CollectionKey);
            if (ownedBase <= 1 || ownedCopy <= 0)
                continue;

            keptPerBase.TryGetValue(cardKey, out int keptBase);
            keptPerCopy.TryGetValue(listing.CollectionKey, out int keptCopy);

            int maxBaseListings = Math.Max(0, ownedBase - 1);
            if (keptBase >= maxBaseListings || keptCopy >= ownedCopy)
                continue;

            keep.Add(listing);
            keptPerBase[cardKey] = keptBase + 1;
            keptPerCopy[listing.CollectionKey] = keptCopy + 1;
        }

        if (keep.Count != mod.State.SaleShelf.Count)
            mod.State.SaleShelf = keep;
    }

    private static int CountOwnedBaseCard(ModEntry mod, string cardKey)
    {
        int total = 0;
        foreach (var pair in mod.State.Collection)
        {
            if (pair.Value <= 0)
                continue;
            if (!CardKeys.TryParse(pair.Key, out string currentCardKey, out _, out _))
                continue;
            if (string.Equals(currentCardKey, cardKey, StringComparison.OrdinalIgnoreCase))
                total += pair.Value;
        }
        return total;
    }

    private static int CountListedBaseCard(ModEntry mod, string cardKey)
    {
        int total = 0;
        foreach (SaleListing listing in mod.State.SaleShelf)
        {
            if (!CardKeys.TryParse(listing.CollectionKey, out string currentCardKey, out _, out _))
                continue;
            if (string.Equals(currentCardKey, cardKey, StringComparison.OrdinalIgnoreCase))
                total++;
        }
        return total;
    }
}
