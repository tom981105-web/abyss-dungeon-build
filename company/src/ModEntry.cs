using System.Reflection;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

public sealed class ModEntry : Mod
{
    private const string SaveKey = "company-state";
    private static readonly FieldInfo? CompanyMenuSelectedTabField = typeof(CompanyMenu).GetField("SelectedTab", BindingFlags.Instance | BindingFlags.NonPublic);

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
    internal MultiplayerCore Multiplayer { get; private set; } = null!;

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
        Contracts = new ContractCore(this);
        Multiplayer = new MultiplayerCore(this);
        Company.Initialize(helper);
        Production.Initialize();
        Multiplayer.Initialize();

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log($"Agricultural Company 0.5 loaded. Persistent client relationships + contracts + equal-partner multiplayer enabled. {ClientProfiles.Count} clients / {ContractTemplates.Count} contract templates. F7 opens management.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        State = Context.IsMainPlayer
            ? Helper.Data.ReadSaveData<CompanySaveData>(SaveKey) ?? new CompanySaveData()
            : new CompanySaveData();

        Company.EnsureState();
        Production.EnsureState();
        Clients.EnsureState();
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
        Contracts.EnsureState();
        Contracts.OnDayStarted();
        Multiplayer.OnDayStarted();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not CompanyMenu menu)
            return;

        // 0.5 activates the existing client sidebar slot without changing the 0.4 menu's warehouse/contract behavior.
        if (CompanyMenuSelectedTabField?.GetValue(menu) is int selectedTab && selectedTab == 4)
            Game1.activeClickableMenu = new ClientMenu(this);
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
        Contracts.EnsureState();
    }
}
