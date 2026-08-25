namespace JunimoCards;

/// <summary>
/// Sale-shelf rules layered on top of the persistent shelf engine.
/// v0.3.4 allows the player's final physical copy to be listed too;
/// the only limit is how many copies are actually owned and not already listed.
/// </summary>
internal static class CardShopRules
{
    internal static int GetListableCount(ModEntry mod, string collectionKey)
    {
        if (!CardKeys.TryParse(collectionKey, out _, out _, out _))
            return 0;

        int owned = mod.GetOwned(collectionKey);
        int listed = mod.GetListedCount(collectionKey);
        return Math.Max(0, owned - listed);
    }

    internal static bool TryListForSale(ModEntry mod, string collectionKey, int slot, out string message)
    {
        if (GetListableCount(mod, collectionKey) <= 0)
        {
            message = "판매 가능한 카드가 없습니다.";
            return false;
        }

        return slot >= 0
            ? mod.Core.TryListForSaleAtSlot(collectionKey, slot, out message)
            : mod.Core.TryListForSale(collectionKey, out message);
    }

    /// <summary>
    /// Repairs stale shelf data without deleting collection cards.
    /// A save may contain at most as many listings of a variant/condition copy
    /// as the number of copies actually owned.
    /// </summary>
    internal static void NormalizeShelf(ModEntry mod)
    {
        if (mod.State.SaleShelf.Count == 0)
            return;

        Dictionary<string, int> keptPerCopy = new(StringComparer.OrdinalIgnoreCase);
        List<SaleListing> keep = new();

        foreach (SaleListing listing in mod.State.SaleShelf.OrderBy(p => p.Slot))
        {
            if (!CardKeys.TryParse(listing.CollectionKey, out _, out _, out _))
                continue;

            int owned = mod.GetOwned(listing.CollectionKey);
            if (owned <= 0)
                continue;

            keptPerCopy.TryGetValue(listing.CollectionKey, out int kept);
            if (kept >= owned)
                continue;

            keep.Add(listing);
            keptPerCopy[listing.CollectionKey] = kept + 1;
        }

        if (keep.Count != mod.State.SaleShelf.Count)
            mod.State.SaleShelf = keep;
    }
}
