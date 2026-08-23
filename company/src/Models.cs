namespace AgriculturalCompany;

public sealed class ModConfig
{
    public string CompanyKey { get; set; } = "F7";
}

public sealed class CompanySaveData
{
    public string CompanyName { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int Reputation { get; set; }
    public int ContractsCompleted { get; set; }
    public int ContractsFailed { get; set; }
    public int ActiveContracts { get; set; }
    public long CompanyFunds { get; set; }
    public long LifetimeRevenue { get; set; }
    public long SeasonRevenue { get; set; }
    public string LastDayKey { get; set; } = "";
    public string SeasonKey { get; set; } = "";
    public Dictionary<string, int> TodayHarvest { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> SeasonHarvest { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> LifetimeHarvest { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, WarehouseStockEntry> Warehouse { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public long LifetimeDeposited { get; set; }
    public long LifetimeWithdrawn { get; set; }

    // Production 2.0 keeps the old fields for forward save compatibility.
    public List<ProductionJob> ProductionQueue { get; set; } = new();
    public Dictionary<string, ProductStockEntry> FinishedGoods { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public long LifetimeProductionBatches { get; set; }
    public long LifetimeFinishedGoods { get; set; }
    public List<ProductionLineState> ProductionLines { get; set; } = new();
    public List<ProductionPlanEntry> ProductionPlans { get; set; } = new();
    public Dictionary<string, IntermediateStockEntry> IntermediateStock { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public long LifetimeIntermediateUnits { get; set; }

    // 0.4 contracts.
    public string ContractBoardDayKey { get; set; } = "";
    public List<CompanyContract> AvailableContracts { get; set; } = new();
    public List<CompanyContract> AcceptedContracts { get; set; } = new();

    // 0.5 persistent client relationships.
    public Dictionary<string, ClientRelationship> ClientRelationships { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // 0.6 brand progression.
    public int BrandPoints { get; set; }
    public int BrandCampaignsRun { get; set; }
    public int LastBrandCampaignDayNumber { get; set; }
    public Dictionary<string, ProductBrandStats> ProductBrands { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public long NetworkRevision { get; set; }
}

public sealed class TrackedCropDefinition
{
    public string ItemId { get; set; } = "";
    public string Family { get; set; } = "Other";
    public string DisplayName { get; set; } = "";
    public string FamilyDisplayName { get; set; } = "";
}

public sealed class WarehouseStockEntry
{
    public string ItemId { get; set; } = "";
    public int Quality { get; set; }
    public int Quantity { get; set; }
}

public sealed class ProductionRecipeDefinition
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string IngredientItemId { get; set; } = "";
    public string IngredientFamily { get; set; } = "";
    public int InputQuantity { get; set; } = 1;
    public int OutputQuantity { get; set; } = 1;
    public int DurationMinutes { get; set; } = 60;
    public int RequiredCompanyLevel { get; set; } = 1;
    public string LineType { get; set; } = "Beverage";
    public string OutputUnit { get; set; } = "개";
    public List<ProductionStageDefinition> Stages { get; set; } = new();
}

public sealed class ProductionStageDefinition
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int DurationMinutes { get; set; } = 30;
    public string IntermediateKey { get; set; } = "";
    public string IntermediateDisplayName { get; set; } = "";
}

public sealed class ProductionLineState
{
    public string Id { get; set; } = "";
    public string LineType { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int BaseEfficiency { get; set; } = 88;
    public int Level { get; set; } = 1;
}

public sealed class ProductionPlanEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RecipeKey { get; set; } = "";
    public int BatchCount { get; set; } = 1;
    public int Priority { get; set; }
    public int CreatedDayNumber { get; set; }
}

public sealed class ProductionJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RecipeKey { get; set; } = "";
    public int BatchCount { get; set; } = 1;
    public int OutputQuality { get; set; }
    public string OutputGrade { get; set; } = "C";
    public int RemainingMinutes { get; set; }
    public int TotalMinutes { get; set; }
    public string LineId { get; set; } = "";
    public int CurrentStageIndex { get; set; }
    public int StageRemainingMinutes { get; set; }
    public int StageTotalMinutes { get; set; }
    public int EfficiencyPercent { get; set; } = 88;
    public int InputQualityScore { get; set; } = 55;
    public int EstimatedOutputQuantity { get; set; }
    public bool AwaitingStageAdvance { get; set; }
    public string BufferedIntermediateKey { get; set; } = "";
    public int BufferedIntermediateQuantity { get; set; }
}

public sealed class IntermediateStockEntry
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Quality { get; set; }
    public string Grade { get; set; } = "C";
    public int Quantity { get; set; }
}

public sealed class ProductStockEntry
{
    public string ProductKey { get; set; } = "";
    public int Quality { get; set; }
    public string Grade { get; set; } = "";
    public int Quantity { get; set; }
}

public sealed class ContractTemplateDefinition
{
    public string Key { get; set; } = "";
    public string ClientKey { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ProductKey { get; set; } = "";
    public int BaseQuantity { get; set; } = 8;
    public int BaseUnitReward { get; set; } = 120;
    public int RequiredCompanyLevel { get; set; } = 1;
    public int MinDeadlineDays { get; set; } = 3;
    public int MaxDeadlineDays { get; set; } = 5;
}

public sealed class CompanyContract
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TemplateKey { get; set; } = "";
    public string ClientKey { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ProductKey { get; set; } = "";
    public string ContractKind { get; set; } = "일반";
    public int RequiredQuantity { get; set; }
    public int DeliveredQuantity { get; set; }
    public int MinimumQuality { get; set; }
    public int RewardGold { get; set; }
    public int ReputationReward { get; set; }
    public int FailureReputationPenalty { get; set; } = 1;
    public int CreatedDayNumber { get; set; }
    public int DeadlineDayNumber { get; set; }
}

public sealed class ClientProfileDefinition
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "거래처";
    public string Description { get; set; } = "";
    public string PreferredProductKey { get; set; } = "";
    public int RequiredCompanyLevel { get; set; } = 1;
    public int CompletionTrust { get; set; } = 6;
    public int FailureTrustPenalty { get; set; } = 7;
}

public sealed class ClientRelationship
{
    public string ClientKey { get; set; } = "";
    public int Trust { get; set; }
    public int CompletedContracts { get; set; }
    public int FailedContracts { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int HighQualityDeliveries { get; set; }
    public long LifetimeRevenue { get; set; }
    public long DeliveredUnits { get; set; }
    public int LastContractDayNumber { get; set; }
}

public sealed class ProductBrandStats
{
    public string ProductKey { get; set; } = "";
    public int Score { get; set; }
    public int ContractsCompleted { get; set; }
    public int HighQualityContracts { get; set; }
    public long UnitsSold { get; set; }
    public long LifetimeRevenue { get; set; }
}

public sealed class BrandCampaignDefinition
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int Cost { get; set; }
    public int BrandGain { get; set; }
    public int RequiredCompanyLevel { get; set; } = 1;
    public int RequiredBrandPoints { get; set; }
}
