using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SObject = StardewValley.Object;

namespace WatermelonGeneticsCore;

public sealed class ModEntry : Mod
{
    // Keep the original item IDs so saves/items made with earlier prototype versions stay compatible.
    public const string HybridizerId = "(BC)Saebyeol.WatermelonCrop_SeedHybridizer";
    private const string SaveKey = "genetics-state";

    private ContentBridge? ContentBridge;

    internal ModConfig Config { get; private set; } = new();
    internal GeneticsSaveData State { get; private set; } = new();
    internal List<VarietyDefinition> Varieties { get; private set; } = new();
    internal Random Random { get; } = new();

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Varieties = helper.Data.ReadJsonFile<List<VarietyDefinition>>("data/varieties.json") ?? new();

        ContentBridge = new ContentBridge(this);
        helper.Events.Content.AssetRequested += ContentBridge.OnAssetRequested;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("Watermelon Genetics standalone core loaded. Content Patcher is not required.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        State = Helper.Data.ReadSaveData<GeneticsSaveData>(SaveKey) ?? new GeneticsSaveData();
        RefreshDiscoveries();
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

        RefreshDiscoveries();
        foreach (HybridJob job in State.Jobs.Values.ToList())
        {
            job.DaysRemaining--;
            if (job.DaysRemaining > 0)
                continue;

            Item item = ItemRegistry.Create(job.ResultItemId, 1);
            if (!Game1.player.addItemToInventoryBool(item))
                Game1.createItemDebris(item, Game1.player.Position, -1, Game1.currentLocation);

            VarietyDefinition? result = Varieties.FirstOrDefault(v => v.SeedId == job.ResultItemId);
            if (result is not null)
            {
                State.Discovered.Add(result.Key);
                State.Records.TryAdd(result.Key, new VarietyRecord());
                State.Records[result.Key].TimesFound++;
            }

            State.Jobs.Remove(JobKey(job.LocationName, job.TileX, job.TileY));
            Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("hybridizer.done")));
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Enum.TryParse<SButton>(Config.CodexKey, true, out SButton key) && e.Button == key)
        {
            if (Game1.activeClickableMenu is null)
                Game1.activeClickableMenu = new CodexMenu(this);
            Helper.Input.Suppress(e.Button);
            return;
        }

        if (Game1.activeClickableMenu is not null || !Context.IsPlayerFree)
            return;

        if (e.Button != SButton.MouseRight && e.Button != SButton.ControllerA)
            return;

        Vector2 tile = e.Cursor.GrabTile;
        if (Game1.currentLocation.Objects.TryGetValue(tile, out SObject? obj) && obj?.QualifiedItemId == HybridizerId)
        {
            Helper.Input.Suppress(e.Button);
            string locationName = Game1.currentLocation.NameOrUniqueName;
            string jobKey = JobKey(locationName, (int)tile.X, (int)tile.Y);
            if (State.Jobs.ContainsKey(jobKey))
            {
                Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("hybridizer.working"), HUDMessage.error_type));
                return;
            }
            Game1.activeClickableMenu = new HybridizerMenu(this, locationName, (int)tile.X, (int)tile.Y);
        }
    }

    internal IReadOnlyList<VarietyDefinition> GetOwnedSeedVarieties()
    {
        HashSet<string> owned = Game1.player.Items
            .Where(i => i is not null)
            .Select(i => i!.QualifiedItemId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Varieties.Where(v => owned.Contains(v.SeedId)).ToList();
    }

    internal bool TryStartHybrid(string location, int x, int y, VarietyDefinition a, VarietyDefinition b, out int chance)
    {
        chance = GeneticsEngine.GetSuccessChance(a, b);
        int needA = a.SeedId == b.SeedId ? 2 : 1;
        int needB = a.SeedId == b.SeedId ? 2 : 1;
        if (!HasSeed(a.SeedId, needA) || !HasSeed(b.SeedId, needB))
            return false;

        ConsumeSeed(a.SeedId, 1);
        ConsumeSeed(b.SeedId, 1);

        bool success = Random.Next(100) < chance;
        VarietyDefinition result = success ? GeneticsEngine.RollResult(a, b, Varieties, Random) : a;
        string key = JobKey(location, x, y);
        State.Jobs[key] = new HybridJob
        {
            LocationName = location,
            TileX = x,
            TileY = y,
            ParentA = a.Key,
            ParentB = b.Key,
            ResultItemId = result.SeedId,
            DaysRemaining = Math.Max(1, Config.ResearchDays)
        };
        State.Records.TryAdd(result.Key, new VarietyRecord());
        State.Records[result.Key].TimesBred++;
        return true;
    }

    internal void RefreshDiscoveries()
    {
        if (!Context.IsWorldReady)
            return;
        foreach (VarietyDefinition v in Varieties)
        {
            bool found = Game1.player.Items.Any(i => i is not null && (i.QualifiedItemId == v.SeedId || i.QualifiedItemId == v.FruitId));
            if (found)
                State.Discovered.Add(v.Key);
        }
        State.Discovered.Add("Common");
    }

    private bool HasSeed(string id, int count)
        => Game1.player.Items.Where(i => i?.QualifiedItemId == id).Sum(i => i?.Stack ?? 0) >= count;

    private void ConsumeSeed(string id, int count)
    {
        for (int i = Game1.player.Items.Count - 1; i >= 0 && count > 0; i--)
        {
            Item? item = Game1.player.Items[i];
            if (item?.QualifiedItemId != id)
                continue;
            int take = Math.Min(count, item.Stack);
            item.Stack -= take;
            count -= take;
            if (item.Stack <= 0)
                Game1.player.Items[i] = null;
        }
    }

    internal static string JobKey(string location, int x, int y) => $"{location}:{x},{y}";
}
