using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class CompanyCore
{
    private readonly ModEntry Mod;

    internal CompanyCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize(IModHelper helper)
    {
        helper.Events.Player.InventoryChanged += OnInventoryChanged;
    }

    internal bool HandleButton(ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return false;

        if (!Enum.TryParse<SButton>(Mod.Config.CompanyKey, true, out SButton key) || e.Button != key)
            return false;

        if (Game1.activeClickableMenu is null)
        {
            EnsureState();
            Game1.activeClickableMenu = new CompanyMenu(Mod);
            Game1.playSound("bigSelect");
        }

        Mod.Helper.Input.Suppress(e.Button);
        return true;
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsLocalPlayer || Game1.activeClickableMenu is not null)
            return;

        foreach (Item item in e.Added)
            Track(item, item.Stack);

        foreach (var change in e.QuantityChanged)
        {
            int gained = change.NewSize - change.OldSize;
            if (gained > 0)
                Track(change.Item, gained);
        }
    }

    private void Track(Item item, int amount)
    {
        if (amount <= 0)
            return;

        TrackedCropDefinition? crop = Mod.Crops.FirstOrDefault(p => string.Equals(p.ItemId, item.QualifiedItemId, StringComparison.OrdinalIgnoreCase));
        if (crop is null)
            return;

        EnsureState();
        Add(Mod.State.TodayHarvest, crop.ItemId, amount);
        Add(Mod.State.SeasonHarvest, crop.ItemId, amount);
        Add(Mod.State.LifetimeHarvest, crop.ItemId, amount);

        int oldLevel = Mod.State.Level;
        Mod.State.Experience += amount;
        UpdateLevel(Mod.State);
        if (Mod.State.Level > oldLevel)
        {
            Game1.addHUDMessage(new HUDMessage($"농업회사 단계 상승! {GetStageName(Mod.State.Level)}"));
            Game1.playSound("achievement");
        }
    }

    internal void EnsureState()
    {
        if (!Context.IsWorldReady)
            return;

        if (string.IsNullOrWhiteSpace(Mod.State.CompanyName))
        {
            string farm = Game1.player.farmName.Value;
            Mod.State.CompanyName = string.IsNullOrWhiteSpace(farm) ? "새별 농업" : $"{farm} 농업";
        }

        string dayKey = $"{Game1.year}:{Game1.currentSeason}:{Game1.dayOfMonth}";
        if (!string.Equals(Mod.State.LastDayKey, dayKey, StringComparison.Ordinal))
        {
            Mod.State.TodayHarvest.Clear();
            Mod.State.LastDayKey = dayKey;
        }

        string seasonKey = $"{Game1.year}:{Game1.currentSeason}";
        if (!string.Equals(Mod.State.SeasonKey, seasonKey, StringComparison.Ordinal))
        {
            Mod.State.SeasonHarvest.Clear();
            Mod.State.SeasonRevenue = 0;
            Mod.State.SeasonKey = seasonKey;
        }

        UpdateLevel(Mod.State);
    }

    internal int GetTotal(Dictionary<string, int> source, string? family = null)
    {
        int total = 0;
        foreach (TrackedCropDefinition crop in Mod.Crops)
        {
            if (family is not null && !string.Equals(crop.Family, family, StringComparison.OrdinalIgnoreCase))
                continue;
            if (source.TryGetValue(crop.ItemId, out int value))
                total += value;
        }
        return total;
    }

    internal static string GetStageName(int level) => level switch
    {
        <= 1 => "개인 농장",
        2 => "소규모 농장",
        3 => "농산물 공방",
        4 => "농업 회사",
        _ => "지역 대표 기업"
    };

    internal static int GetLevelStartXp(int level) => level switch
    {
        <= 1 => 0,
        2 => 50,
        3 => 150,
        4 => 400,
        _ => 900
    };

    internal static int GetNextLevelXp(int level) => level switch
    {
        <= 1 => 50,
        2 => 150,
        3 => 400,
        4 => 900,
        _ => 900
    };

    private static void UpdateLevel(CompanySaveData company)
    {
        int xp = Math.Max(0, company.Experience);
        company.Level = xp >= 900 ? 5 : xp >= 400 ? 4 : xp >= 150 ? 3 : xp >= 50 ? 2 : 1;
    }

    private static void Add(Dictionary<string, int> values, string id, int amount)
    {
        values.TryGetValue(id, out int old);
        values[id] = old + amount;
    }
}
