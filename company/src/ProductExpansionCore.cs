using StardewModdingAPI;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ProductExpansionCore
{
    private readonly ModEntry Mod;

    internal ProductExpansionCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void EnsureState()
    {
        Mod.Production.EnsureState();
    }

    internal IReadOnlyList<ProductionRecipeDefinition> GetFinishedRecipes(bool includeLocked = true)
        => Mod.Production.GetCatalogRecipes(includeLocked)
            .Where(p => !string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase))
            .ToList();

    internal IReadOnlyList<ProductionRecipeDefinition> GetIntermediateRecipes(bool includeLocked = true)
        => Mod.Production.GetCatalogRecipes(includeLocked)
            .Where(p => string.Equals(p.OutputKind, "Intermediate", StringComparison.OrdinalIgnoreCase))
            .ToList();

    internal void AddDailyExpansionContracts()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        EnsureState();
        int today = ContractCore.GetCurrentDayNumber();
        List<ProductionRecipeDefinition> eligible = GetFinishedRecipes(false)
            .Where(IsRecipeContentAvailable)
            .Where(p => !string.Equals(p.Key, "TomatoJuice", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(p.Key, "WatermelonJuice", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(p.Key, "ChamoeGiftSet", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(p.Key, "SaltedNapaCabbage", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (eligible.Count == 0)
            return;

        int desiredExtra = Mod.State.Level >= 4 ? 3 : Mod.State.Level >= 2 ? 2 : 1;
        int existingExtras = Mod.State.AvailableContracts.Count(p => p.TemplateKey.StartsWith("P22:", StringComparison.OrdinalIgnoreCase));
        if (existingExtras >= desiredExtra)
            return;

        int seed = HashCode.Combine(today, Mod.State.Level, Mod.State.BrandPoints, Mod.State.Reputation, 722);
        Random random = new(seed);
        HashSet<string> existingProducts = Mod.State.AvailableContracts.Select(p => p.ProductKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (ProductionRecipeDefinition recipe in eligible.OrderBy(_ => random.Next()))
        {
            if (existingExtras >= desiredExtra)
                break;
            if (existingProducts.Contains(recipe.Key))
                continue;

            (string clientKey, string clientName) = ClientFor(recipe.ProductFamily, recipe.RequiredBrandPoints);
            ClientRelationship relation = Mod.Clients.GetRelationship(clientKey);
            int baseQuantity = BaseQuantity(recipe);
            int scale = 100 + Math.Max(0, Mod.State.Level - 1) * 10 + random.Next(0, 21)
                + Mod.Clients.GetQuantityBonusPercent(clientKey)
                + Mod.Brand.GetContractQuantityBonusPercent();
            int quantity = Math.Max(1, baseQuantity * scale / 100);

            int quality = RollQuality(random, recipe.RequiredBrandPoints, Mod.State.Level);
            float qualityMultiplier = quality switch { 1 => 1.18f, 2 => 1.42f, 4 => 1.9f, _ => 1f };
            int rewardBonus = Mod.Clients.GetRewardBonusPercent(clientKey)
                + Mod.Brand.GetContractRewardBonusPercent()
                + Mod.Brand.GetProductRewardBonusPercent(recipe.Key);
            int reward = (int)Math.Round(quantity * BaseUnitReward(recipe) * qualityMultiplier * (100 + rewardBonus) / 100f);
            int deadline = today + (recipe.RequiredBrandPoints >= 150 ? 6 : recipe.RequiredBrandPoints >= 50 ? 5 : 4) + (relation.Trust >= 50 ? 1 : 0);

            Mod.State.AvailableContracts.Add(new CompanyContract
            {
                TemplateKey = $"P22:{recipe.Key}:{today}",
                ClientKey = clientKey,
                ClientName = clientName,
                ProductKey = recipe.Key,
                ContractKind = Mod.Clients.GetContractKind(clientKey),
                RequiredQuantity = quantity,
                DeliveredQuantity = 0,
                MinimumQuality = quality,
                RewardGold = Math.Max(1, reward),
                ReputationReward = quality >= 2 ? 3 : quality >= 1 ? 2 : 1,
                FailureReputationPenalty = quality >= 2 ? 2 : 1,
                CreatedDayNumber = today,
                DeadlineDayNumber = deadline
            });
            existingProducts.Add(recipe.Key);
            existingExtras++;
        }
    }

    private bool IsRecipeContentAvailable(ProductionRecipeDefinition recipe)
        => !recipe.RequiresCropGenetics || Mod.Helper.ModRegistry.IsLoaded("Saebyeol.WatermelonGenetics");

    private static int BaseQuantity(ProductionRecipeDefinition recipe)
    {
        if (recipe.RequiredBrandPoints >= 150) return 4;
        if (recipe.LineType == "Packaging") return 6;
        if (recipe.LineType == "Fermentation") return 10;
        return 10;
    }

    private static int BaseUnitReward(ProductionRecipeDefinition recipe)
    {
        int value = recipe.ProductFamily switch
        {
            "Tomato" => 230,
            "Watermelon" => 320,
            "KoreanMelon" => 360,
            "NapaCabbage" => 290,
            _ => 220
        };
        value += Math.Max(0, recipe.RequiredCompanyLevel - 1) * 45;
        value += recipe.RequiredBrandPoints >= 150 ? 220 : recipe.RequiredBrandPoints >= 50 ? 100 : recipe.RequiredBrandPoints >= 20 ? 45 : 0;
        if (recipe.LineType == "Packaging") value += 80;
        return value;
    }

    private static (string Key, string Name) ClientFor(string family, int requiredBrand)
    {
        if (requiredBrand >= 150)
            return ("ZuzuFoodDistribution", "주주시티 식품유통");
        return family switch
        {
            "Tomato" => ("ValleyDiner", "계곡 식당"),
            "Watermelon" => ("CoastalCafe", "해안 카페"),
            "KoreanMelon" => ("RegionalSpecialtyStore", "지역 특산품 상회"),
            "NapaCabbage" => ("ValleyKimchiWorkshop", "계곡 김치공방"),
            _ => ("PelicanMarket", "펠리컨 마트")
        };
    }

    private static int RollQuality(Random random, int requiredBrand, int level)
    {
        int roll = random.Next(100);
        if (requiredBrand >= 150 && level >= 4)
            return roll < 18 ? 4 : roll < 68 ? 2 : 1;
        if (requiredBrand >= 50 || level >= 3)
            return roll < 8 ? 4 : roll < 45 ? 2 : roll < 78 ? 1 : 0;
        if (level >= 2)
            return roll < 22 ? 2 : roll < 58 ? 1 : 0;
        return 0;
    }
}
