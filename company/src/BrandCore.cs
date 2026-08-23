using StardewModdingAPI;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class BrandCore
{
    private readonly ModEntry Mod;

    internal static readonly IReadOnlyList<BrandCampaignDefinition> Campaigns = new List<BrandCampaignDefinition>
    {
        new()
        {
            Key = "LocalTasting",
            DisplayName = "지역 시식회",
            Description = "마을 주민에게 대표 상품을 알리는 소규모 홍보 행사입니다.",
            Cost = 2500,
            BrandGain = 10,
            RequiredCompanyLevel = 1,
            RequiredBrandPoints = 0
        },
        new()
        {
            Key = "ValleyPromotion",
            DisplayName = "계곡 홍보전",
            Description = "계곡 전역의 상점과 거래처에 브랜드를 집중 홍보합니다.",
            Cost = 8000,
            BrandGain = 28,
            RequiredCompanyLevel = 2,
            RequiredBrandPoints = 40
        },
        new()
        {
            Key = "ZuzuExpo",
            DisplayName = "주주시티 식품 박람회",
            Description = "대도시 유통 바이어에게 회사를 알리는 대형 홍보 행사입니다.",
            Cost = 25000,
            BrandGain = 70,
            RequiredCompanyLevel = 4,
            RequiredBrandPoints = 150
        }
    };

    internal BrandCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void EnsureState()
    {
        Mod.State.BrandPoints = Math.Max(0, Mod.State.BrandPoints);
        Mod.State.BrandCampaignsRun = Math.Max(0, Mod.State.BrandCampaignsRun);
        Mod.State.ProductBrands ??= new Dictionary<string, ProductBrandStats>(StringComparer.OrdinalIgnoreCase);

        foreach (ProductionRecipeDefinition recipe in Mod.Recipes)
        {
            if (string.IsNullOrWhiteSpace(recipe.Key))
                continue;

            if (!Mod.State.ProductBrands.TryGetValue(recipe.Key, out ProductBrandStats? stats) || stats is null)
            {
                stats = new ProductBrandStats { ProductKey = recipe.Key };
                Mod.State.ProductBrands[recipe.Key] = stats;
            }
            else if (string.IsNullOrWhiteSpace(stats.ProductKey))
            {
                stats.ProductKey = recipe.Key;
            }

            stats.Score = Math.Clamp(stats.Score, 0, 100);
            stats.ContractsCompleted = Math.Max(0, stats.ContractsCompleted);
            stats.HighQualityContracts = Math.Max(0, stats.HighQualityContracts);
            stats.UnitsSold = Math.Max(0, stats.UnitsSold);
            stats.LifetimeRevenue = Math.Max(0, stats.LifetimeRevenue);
        }
    }

    internal ProductBrandStats GetProductStats(string productKey)
    {
        EnsureState();
        if (!Mod.State.ProductBrands.TryGetValue(productKey, out ProductBrandStats? stats) || stats is null)
        {
            stats = new ProductBrandStats { ProductKey = productKey };
            Mod.State.ProductBrands[productKey] = stats;
        }
        return stats;
    }

    internal int GetTierIndex(int points)
        => points >= 700 ? 4 : points >= 350 ? 3 : points >= 150 ? 2 : points >= 50 ? 1 : 0;

    internal string GetTierName(int points) => GetTierIndex(points) switch
    {
        1 => "계곡 인기 브랜드",
        2 => "지역 유통 브랜드",
        3 => "프리미엄 지역 브랜드",
        4 => "주주시티 진출 브랜드",
        _ => "로컬 농장 브랜드"
    };

    internal int GetNextTierPoints(int points)
        => points < 50 ? 50 : points < 150 ? 150 : points < 350 ? 350 : points < 700 ? 700 : 700;

    internal int GetContractRewardBonusPercent()
        => GetTierIndex(Mod.State.BrandPoints) switch
        {
            1 => 3,
            2 => 7,
            3 => 12,
            4 => 18,
            _ => 0
        };

    internal int GetContractQuantityBonusPercent()
        => GetTierIndex(Mod.State.BrandPoints) switch
        {
            2 => 4,
            3 => 8,
            4 => 12,
            _ => 0
        };

    internal int GetProductRewardBonusPercent(string productKey)
    {
        int score = GetProductStats(productKey).Score;
        return score >= 80 ? 10 : score >= 60 ? 7 : score >= 40 ? 4 : score >= 20 ? 2 : 0;
    }

    internal string GetProductTierName(int score)
        => score >= 80 ? "대표 상품" : score >= 60 ? "인기 상품" : score >= 40 ? "성장 상품" : score >= 20 ? "인지 상품" : "신규 상품";

    internal void RecordCompletedContract(CompanyContract contract)
    {
        EnsureState();
        int relationTier = string.IsNullOrWhiteSpace(contract.ClientKey)
            ? 0
            : Mod.Clients.GetTierIndex(Mod.Clients.GetRelationship(contract.ClientKey).Trust);

        int brandGain = 4 + Math.Min(8, Math.Max(0, contract.RequiredQuantity) / 10) + relationTier;
        if (contract.MinimumQuality >= 4)
            brandGain += 5;
        else if (contract.MinimumQuality >= 2)
            brandGain += 3;
        else if (contract.MinimumQuality >= 1)
            brandGain += 1;
        Mod.State.BrandPoints += Math.Max(1, brandGain);

        ProductBrandStats stats = GetProductStats(contract.ProductKey);
        int productGain = 4 + Math.Min(6, Math.Max(0, contract.RequiredQuantity) / 12);
        if (contract.MinimumQuality >= 4)
            productGain += 7;
        else if (contract.MinimumQuality >= 2)
            productGain += 4;
        else if (contract.MinimumQuality >= 1)
            productGain += 2;

        stats.Score = Math.Clamp(stats.Score + productGain, 0, 100);
        stats.ContractsCompleted++;
        if (contract.MinimumQuality >= 2)
            stats.HighQualityContracts++;
        stats.UnitsSold += Math.Max(0, contract.RequiredQuantity);
        stats.LifetimeRevenue += Math.Max(0, contract.RewardGold);
    }

    internal void RecordFailedContract(CompanyContract contract)
    {
        EnsureState();
        int penalty = string.Equals(contract.ContractKind, "핵심", StringComparison.OrdinalIgnoreCase) ? 3
            : string.Equals(contract.ContractKind, "우선", StringComparison.OrdinalIgnoreCase) ? 2
            : 1;
        Mod.State.BrandPoints = Math.Max(0, Mod.State.BrandPoints - penalty);

        ProductBrandStats stats = GetProductStats(contract.ProductKey);
        stats.Score = Math.Max(0, stats.Score - 1);
    }

    internal bool TryRunCampaign(string campaignKey, out string message)
    {
        EnsureState();
        if (Context.IsMultiplayer && !Context.IsMainPlayer)
        {
            if (!Mod.Multiplayer.IsSynchronized)
            {
                message = "회사 데이터를 동기화하는 중입니다.";
                Mod.Multiplayer.RequestSync();
                return false;
            }

            Mod.Multiplayer.RequestBrandCampaign(campaignKey);
            message = "브랜드 캠페인을 공동 회사에 반영 중입니다.";
            return true;
        }

        bool ok = TryRunCampaignAuthoritative(campaignKey, out message);
        if (ok)
            Mod.Multiplayer.BroadcastState();
        return ok;
    }

    internal bool TryRunCampaignAuthoritative(string campaignKey, out string message)
    {
        EnsureState();
        BrandCampaignDefinition? campaign = Campaigns.FirstOrDefault(p => string.Equals(p.Key, campaignKey, StringComparison.OrdinalIgnoreCase));
        if (campaign is null)
        {
            message = "브랜드 캠페인을 찾을 수 없습니다.";
            return false;
        }

        int today = ContractCore.GetCurrentDayNumber();
        if (Mod.State.LastBrandCampaignDayNumber == today)
        {
            message = "브랜드 캠페인은 하루에 한 번만 진행할 수 있습니다.";
            return false;
        }

        if (Mod.State.Level < campaign.RequiredCompanyLevel)
        {
            message = $"회사 Lv.{campaign.RequiredCompanyLevel}부터 진행할 수 있습니다.";
            return false;
        }

        if (Mod.State.BrandPoints < campaign.RequiredBrandPoints)
        {
            message = $"브랜드 인지도 {campaign.RequiredBrandPoints} 이상이 필요합니다.";
            return false;
        }

        if (Mod.State.CompanyFunds < campaign.Cost)
        {
            message = $"회사 자금이 부족합니다. {campaign.Cost:N0}G 필요";
            return false;
        }

        Mod.State.CompanyFunds -= campaign.Cost;
        Mod.State.BrandPoints += campaign.BrandGain;
        Mod.State.BrandCampaignsRun++;
        Mod.State.LastBrandCampaignDayNumber = today;

        // Marketing lifts every currently known product slightly, but sales performance remains the main way to build product reputation.
        foreach (ProductBrandStats stats in Mod.State.ProductBrands.Values.Where(p => p is not null))
            stats.Score = Math.Clamp(stats.Score + 1, 0, 100);

        message = $"{campaign.DisplayName} 완료 · 브랜드 +{campaign.BrandGain} · 회사 자금 -{campaign.Cost:N0}G";
        return true;
    }
}
