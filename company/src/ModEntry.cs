using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AgriculturalCompany;

public sealed class ModEntry : Mod
{
    private const string SaveKey = "company-state";

    internal ModConfig Config { get; private set; } = new();
    internal CompanySaveData State { get; private set; } = new();
    internal List<TrackedCropDefinition> Crops { get; private set; } = new();
    internal CompanyCore Company { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Crops = helper.Data.ReadJsonFile<List<TrackedCropDefinition>>("data/tracked_crops.json") ?? new();
        Company = new CompanyCore(this);
        Company.Initialize(helper);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log($"Agricultural Company 0.2 loaded. Warehouse enabled. Tracking {Crops.Count} crop IDs. F7 opens management.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        State = Helper.Data.ReadSaveData<CompanySaveData>(SaveKey) ?? new CompanySaveData();
        Company.EnsureState();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
            Helper.Data.WriteSaveData(SaveKey, State);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsWorldReady)
            Company.EnsureState();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        Company.HandleButton(e);
    }
}
