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
    internal ProductionQualityCore Quality { get; private set; } = null!;
    internal ProductExpansionCore Products { get; private set; } = null!;
    internal ContractCore Contracts { get; private set; } = null!;
    internal ClientCore Clients { get; private set; } = null!;
    internal BrandCore Brand { get; private set; } = null!;
    internal MultiplayerCore Multiplayer { get; private set; } = null!;
    internal ProductIconRenderer Icons { get; private set; } = null!;
    internal ProductIconProductionOverlay IconOverlay { get; private set; } = null!;
    internal Production2Ui ProductionUi { get; private set; } = null!;
    internal ProductionQualityUi QualityUi { get; private set; } = null!;
    internal ProductExpansionUi ProductUi { get; private set; } = null!;
    internal UiHotfixCore UiHotfix { get; private set; } = null!;
    internal VersionLabelOverlay VersionLabels { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        Crops = helper.Data.ReadJsonFile<List<TrackedCropDefinition>>("data/tracked_crops.json") ?? new();
        List<TrackedCropDefinition> optionalCropGenetics = helper.Data.ReadJsonFile<List<TrackedCropDefinition>>("data/crop_genetics_crops.json") ?? new();
        foreach (TrackedCropDefinition crop in optionalCropGenetics)
        {
            if (!Crops.Any(p => string.Equals(p.ItemId, crop.ItemId, StringComparison.OrdinalIgnoreCase)))
                Crops.Add(crop);
        }

        Recipes = helper.Data.ReadJsonFile<List<ProductionRecipeDefinition>>("data/production_recipes.json") ?? new();
        List<VanillaProductDefinition> vanillaProducts = helper.Data.ReadJsonFile<List<VanillaProductDefinition>>("data/vanilla_products.json") ?? new();
        Recipes.AddRange(VanillaProductionCatalog.Build(vanillaProducts));
        Recipes = Recipes
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Key))
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => p.First())
            .ToList();

        ContractTemplates = helper.Data.ReadJsonFile<List<ContractTemplateDefinition>>("data/contract_templates.json") ?? new();
        ClientProfiles = helper.Data.ReadJsonFile<List<ClientProfileDefinition>>("data/client_profiles.json") ?? new();

        Company = new CompanyCore(this);
        Production = new ProductionCore(this);
        Quality = new ProductionQualityCore(this);
        Products = new ProductExpansionCore(this);
        Clients = new ClientCore(this);
        Brand = new BrandCore(this);
        Contracts = new ContractCore(this);
        Multiplayer = new MultiplayerCore(this);
        Icons = new ProductIconRenderer(this);
        IconOverlay = new ProductIconProductionOverlay(this);
        ProductionUi = new Production2Ui(this);
        QualityUi = new ProductionQualityUi(this);
        ProductUi = new ProductExpansionUi(this);
        UiHotfix = new UiHotfixCore(this);
        VersionLabels = new VersionLabelOverlay(this);

        Company.Initialize(helper);
        Production.Initialize();
        Quality.Initialize();
        Brand.Initialize();
        Multiplayer.Initialize();

        // 0.7.5: one UI controller owns menu positioning and overlays.
        // The older viewport-based UI layers stay constructed for code compatibility,
        // but are intentionally not registered so they cannot fight the corrected layout.
        UiHotfix.Initialize();
        VersionLabels.Initialize();

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        int vanillaCropCount = Crops.Count(p => p.Family.StartsWith("Vanilla", StringComparison.OrdinalIgnoreCase));
        Monitor.Log($"Agricultural Company 0.7.5 loaded. UI scale/centering hotfix enabled with Production 2.x and product icons for {Recipes.Count} recipes. Vanilla crops: {vanillaCropCount}. F7 opens management.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        State = Context.IsMainPlayer
            ? Helper.Data.ReadSaveData<CompanySaveData>(SaveKey) ?? new CompanySaveData()
            : new CompanySaveData();

        Company.EnsureState();
        Production.EnsureState();
        Quality.EnsureState();
        Products.EnsureState();
        Clients.EnsureState();
        Brand.EnsureState();
        Contracts.EnsureState();
        Multiplayer.OnSaveLoaded();
        Contracts.OnDayStarted();
        Products.AddDailyExpansionContracts();
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
        Quality.EnsureState();
        Products.EnsureState();
        Clients.EnsureState();
        Brand.EnsureState();
        Contracts.EnsureState();
        Contracts.OnDayStarted();
        Products.AddDailyExpansionContracts();
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
        Quality.EnsureState();
        Products.EnsureState();
        Clients.EnsureState();
        Brand.EnsureState();
        Contracts.EnsureState();
    }
}
