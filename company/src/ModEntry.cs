using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AgriculturalCompany;

public sealed class ModEntry : Mod
{
    private const string SaveKey = "company-state";

    internal ModConfig Config { get; private set; } = new();
    internal CompanySaveData State { get; private set; } = new();
    internal List<TrackedCropDefinition> Crops { get; private set; } = new();
    internal List<ProductionRecipeDefinition> Recipes { get; private set; } = new();
    internal List<ContractTemplateDefinition> ContractTemplates { get; private set; } = new();
    internal List<ClientProfileDefinition> ClientProfiles { get; private set; } = new();
    internal CompanyCore Company { get; private set; } = null!;
    internal ProductionCore Production { get; private set; } = null!;
    internal ContractCore Contracts { get; private set; } = null!;
    internal ClientCore Clients { get; private set; } = null!;
    internal BrandCore Brand { get; private set; } = null!;
    internal MultiplayerCore Multiplayer { get; private set; } = null!;
    internal Production2Ui ProductionUi { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Crops = helper.Data.ReadJsonFile<List<TrackedCropDefinition>>("data/tracked_crops.json") ?? new();
        Recipes = helper.Data.ReadJsonFile<List<ProductionRecipeDefinition>>("data/production_recipes.json") ?? new();
        ContractTemplates = helper.Data.ReadJsonFile<List<ContractTemplateDefinition>>("data/contract_templates.json") ?? new();
        ClientProfiles = helper.Data.ReadJsonFile<List<ClientProfileDefinition>>("data/client_profiles.json") ?? new();

        Company = new CompanyCore(this);
        Production = new ProductionCore(this);
        Clients = new ClientCore(this);
        Brand = new BrandCore(this);
        Contracts = new ContractCore(this);
        Multiplayer = new MultiplayerCore(this);
        ProductionUi = new Production2Ui(this);

        Company.Initialize(helper);
        Production.Initialize();
        Brand.Initialize();
        Multiplayer.Initialize();
        ProductionUi.Initialize();

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log($"Agricultural Company 0.7 loaded. Production 2.0 lines + multi-stage processing + production planning + intermediates + graded finished goods enabled. {Recipes.Count} production recipes. F7 opens management.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        State = Context.IsMainPlayer
            ? Helper.Data.ReadSaveData<CompanySaveData>(SaveKey) ?? new CompanySaveData()
            : new CompanySaveData();

        Company.EnsureState();
        Production.EnsureState();
        Clients.EnsureState();
        Brand.EnsureState();
        Contracts.EnsureState();
        Multiplayer.OnSaveLoaded();
        Contracts.OnDayStarted();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
            Helper.Data.WriteSaveData(SaveKey, State);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        Company.EnsureState();
        Production.EnsureState();
        Clients.EnsureState();
        Brand.EnsureState();
        Contracts.EnsureState();
        Contracts.OnDayStarted();
        Multiplayer.OnDayStarted();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        Company.HandleButton(e);
    }

    internal void ApplyNetworkState(CompanySaveData state)
    {
        State = state ?? new CompanySaveData();
        Company.EnsureState();
        Production.EnsureState();
        Clients.EnsureState();
        Brand.EnsureState();
        Contracts.EnsureState();
    }
}
