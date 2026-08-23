using StardewModdingAPI;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class ContractCore
{
    private readonly ModEntry Mod;

    internal ContractCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void EnsureState()
    {
        Mod.State.AvailableContracts ??= new List<CompanyContract>();
        Mod.State.AcceptedContracts ??= new List<CompanyContract>();
        Mod.State.AvailableContracts.RemoveAll(p => p is null || string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.ProductKey));
        Mod.State.AcceptedContracts.RemoveAll(p => p is null || string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.ProductKey));
        Mod.Clients.EnsureState();
        Mod.Brand.EnsureState();

        foreach (CompanyContract contract in Mod.State.AvailableContracts.Concat(Mod.State.AcceptedContracts))
            MigrateClientKey(contract);

        Mod.State.ActiveContracts = Mod.State.AcceptedContracts.Count;
    }

    internal void OnDayStarted()
    {
        EnsureState();
        if (!Context.IsMainPlayer)
            return;

        int today = GetCurrentDayNumber();
        List<CompanyContract> expired = Mod.State.AcceptedContracts
            .Where(p => today > p.DeadlineDayNumber)
            .ToList();

        foreach (CompanyContract contract in expired)
        {
            Mod.State.AcceptedContracts.Remove(contract);
            Mod.State.ContractsFailed++;
            Mod.State.Reputation = Math.Max(0, Mod.State.Reputation - Math.Max(0, contract.FailureReputationPenalty));
            Mod.Clients.RecordFailedContract(contract, today);
            Mod.Brand.RecordFailedContract(contract);
        }
        Mod.State.ActiveContracts = Mod.State.AcceptedContracts.Count;

        if (expired.Count > 0)
        {
            string notice = $"납기 초과 계약 {expired.Count}건이 실패 처리되었습니다.";
            Game1.addHUDMessage(new HUDMessage(notice, HUDMessage.error_type));
            Mod.Multiplayer.BroadcastNotice(notice);
        }

        GenerateDailyBoard();
    }

    internal int GetActiveCapacity() => Mod.State.Level switch
    {
        <= 1 => 1,
        2 => 2,
        3 => 3,
        4 => 4,
        _ => 5
    };

    internal bool TryAccept(string contractId, out string message)
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

            Mod.Multiplayer.RequestContract(contractId, "accept");
            message = "계약 수락을 공동 회사에 반영 중입니다.";
            return true;
        }

        bool ok = TryAcceptAuthoritative(contractId, out message);
        if (ok)
            Mod.Multiplayer.BroadcastState();
        return ok;
    }

    internal bool TryAcceptAuthoritative(string contractId, out string message)
    {
        EnsureState();
        CompanyContract? contract = Mod.State.AvailableContracts.FirstOrDefault(p => string.Equals(p.Id, contractId, StringComparison.Ordinal));
        if (contract is null)
        {
            message = "이미 처리되었거나 만료된 계약입니다.";
            return false;
        }

        if (Mod.State.AcceptedContracts.Count >= GetActiveCapacity())
        {
            message = $"동시에 진행할 수 있는 계약은 {GetActiveCapacity()}건입니다.";
            return false;
        }

        if (GetCurrentDayNumber() > contract.DeadlineDayNumber)
        {
            Mod.State.AvailableContracts.Remove(contract);
            message = "이미 납기가 지난 계약입니다.";
            return false;
        }

        Mod.State.AvailableContracts.Remove(contract);
        Mod.State.AcceptedContracts.Add(contract);
        Mod.State.ActiveContracts = Mod.State.AcceptedContracts.Count;
        message = $"[{contract.ContractKind}] {contract.ClientName} 계약을 수락했습니다.";
        return true;
    }

    internal bool TryDeliver(string contractId, out string message)
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

            Mod.Multiplayer.RequestContract(contractId, "deliver");
            message = "납품을 공동 회사에 반영 중입니다.";
            return true;
        }

        bool ok = TryDeliverAuthoritative(contractId, out message);
        if (ok)
            Mod.Multiplayer.BroadcastState();
        return ok;
    }

    internal bool TryDeliverAuthoritative(string contractId, out string message)
    {
        EnsureState();
        CompanyContract? contract = Mod.State.AcceptedContracts.FirstOrDefault(p => string.Equals(p.Id, contractId, StringComparison.Ordinal));
        if (contract is null)
        {
            message = "진행 중인 계약을 찾을 수 없습니다.";
            return false;
        }

        if (GetCurrentDayNumber() > contract.DeadlineDayNumber)
        {
            message = "납기가 지나 더 이상 납품할 수 없습니다.";
            return false;
        }

        int remaining = Math.Max(0, contract.RequiredQuantity - contract.DeliveredQuantity);
        if (remaining <= 0)
        {
            message = "이미 납품이 완료된 계약입니다.";
            return false;
        }

        int available = GetQualifyingFinishedQuantity(contract.ProductKey, contract.MinimumQuality);
        if (available <= 0)
        {
            message = $"{QualityRequirementText(contract.MinimumQuality)} 완제품 재고가 없습니다.";
            return false;
        }

        int amount = Math.Min(remaining, available);
        int consumed = ConsumeFinishedGoods(contract.ProductKey, contract.MinimumQuality, amount);
        if (consumed <= 0)
        {
            message = "완제품 재고가 변경되어 납품하지 못했습니다.";
            return false;
        }

        contract.DeliveredQuantity += consumed;
        if (contract.DeliveredQuantity < contract.RequiredQuantity)
        {
            message = $"{GetProductName(contract.ProductKey)} {consumed}개 납품 · {contract.DeliveredQuantity}/{contract.RequiredQuantity}";
            return true;
        }

        CompleteContract(contract);
        ClientRelationship relation = Mod.Clients.GetRelationship(contract.ClientKey);
        message = $"계약 완료! {contract.ClientName} · 회사 +{contract.RewardGold:N0}G · 신뢰 {relation.Trust}/100 · 브랜드 {Mod.State.BrandPoints}";
        return true;
    }

    internal int GetQualifyingFinishedQuantity(string productKey, int minimumQuality)
        => Mod.State.FinishedGoods.Values
            .Where(p => p is not null
                && p.Quantity > 0
                && p.Quality >= minimumQuality
                && string.Equals(p.ProductKey, productKey, StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Quantity);

    internal string GetProductName(string productKey)
        => Mod.Recipes.FirstOrDefault(p => string.Equals(p.Key, productKey, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? productKey;

    internal int GetDaysRemaining(CompanyContract contract)
        => Math.Max(0, contract.DeadlineDayNumber - GetCurrentDayNumber());

    internal bool IsProductAvailable(string productKey)
    {
        if (string.Equals(productKey, "TomatoJuice", StringComparison.OrdinalIgnoreCase))
            return true;

        return Mod.Helper.ModRegistry.IsLoaded("Saebyeol.WatermelonGenetics");
    }

    private int ConsumeFinishedGoods(string productKey, int minimumQuality, int requested)
    {
        int remaining = Math.Max(0, requested);
        int moved = 0;
        foreach ((string key, ProductStockEntry entry) in Mod.State.FinishedGoods
            .Where(p => p.Value is not null
                && p.Value.Quantity > 0
                && p.Value.Quality >= minimumQuality
                && string.Equals(p.Value.ProductKey, productKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Value.Quality)
            .ToList())
        {
            if (remaining <= 0)
                break;

            int take = Math.Min(remaining, entry.Quantity);
            entry.Quantity -= take;
            remaining -= take;
            moved += take;
            if (entry.Quantity <= 0)
                Mod.State.FinishedGoods.Remove(key);
        }
        return moved;
    }

    private void CompleteContract(CompanyContract contract)
    {
        int today = GetCurrentDayNumber();
        Mod.State.AcceptedContracts.Remove(contract);
        Mod.State.ActiveContracts = Mod.State.AcceptedContracts.Count;
        Mod.State.ContractsCompleted++;
        Mod.State.CompanyFunds += Math.Max(0, contract.RewardGold);
        Mod.State.LifetimeRevenue += Math.Max(0, contract.RewardGold);
        Mod.State.SeasonRevenue += Math.Max(0, contract.RewardGold);
        Mod.State.Reputation += Math.Max(0, contract.ReputationReward);
        Mod.Clients.RecordCompletedContract(contract, today);
        Mod.Brand.RecordCompletedContract(contract);
        Mod.Company.AddCompanyExperience(10 + Math.Max(1, contract.RequiredQuantity / 2));
    }

    private void GenerateDailyBoard()
    {
        string dayKey = $"{Game1.year}:{Game1.currentSeason}:{Game1.dayOfMonth}";
        if (string.Equals(Mod.State.ContractBoardDayKey, dayKey, StringComparison.Ordinal))
            return;

        Mod.State.ContractBoardDayKey = dayKey;
        Mod.State.AvailableContracts.Clear();

        List<ContractTemplateDefinition> eligible = Mod.ContractTemplates
            .Where(p => p.RequiredCompanyLevel <= Mod.State.Level && IsProductAvailable(p.ProductKey))
            .ToList();
        if (eligible.Count == 0)
            return;

        int today = GetCurrentDayNumber();
        int seed = HashCode.Combine(today, Mod.State.Level, Mod.State.Reputation, Mod.State.ContractsCompleted, Mod.State.BrandPoints);
        Random random = new(seed);

        // Established clients get a predictable place on the board; the remaining slots stay varied.
        List<ContractTemplateDefinition> selected = new();
        ContractTemplateDefinition? relationshipPick = eligible
            .Where(p => !string.IsNullOrWhiteSpace(p.ClientKey) && Mod.Clients.GetRelationship(p.ClientKey).Trust >= 20)
            .OrderByDescending(p => Mod.Clients.GetRelationship(p.ClientKey).Trust)
            .ThenBy(_ => random.Next())
            .FirstOrDefault();
        if (relationshipPick is not null)
            selected.Add(relationshipPick);

        foreach (ContractTemplateDefinition template in eligible
            .Where(p => relationshipPick is null || !string.Equals(p.Key, relationshipPick.Key, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => random.NextDouble() - Math.Min(100, Mod.Clients.GetRelationship(p.ClientKey).Trust) / 250.0))
        {
            if (selected.Count >= Math.Min(4, eligible.Count))
                break;
            selected.Add(template);
        }

        foreach (ContractTemplateDefinition template in selected)
        {
            string clientKey = ResolveClientKey(template);
            ClientRelationship relation = Mod.Clients.GetRelationship(clientKey);
            int quantityBonus = Mod.Clients.GetQuantityBonusPercent(clientKey) + Mod.Brand.GetContractQuantityBonusPercent();
            int rewardBonus = Mod.Clients.GetRewardBonusPercent(clientKey)
                + Mod.Brand.GetContractRewardBonusPercent()
                + Mod.Brand.GetProductRewardBonusPercent(template.ProductKey);

            int quantityScale = 100 + Math.Max(0, Mod.State.Level - 1) * 12 + random.Next(0, 26) + quantityBonus;
            int quantity = Math.Max(1, template.BaseQuantity * quantityScale / 100);
            int quality = RollMinimumQuality(random, Mod.State.Level, relation.Trust);
            float qualityMultiplier = quality switch { 1 => 1.18f, 2 => 1.42f, 4 => 1.85f, _ => 1f };
            int reward = (int)Math.Round(quantity * template.BaseUnitReward * qualityMultiplier * (100 + rewardBonus) / 100f);
            int minDays = Math.Max(1, template.MinDeadlineDays);
            int maxDays = Math.Max(minDays, template.MaxDeadlineDays);
            int deadlineDays = random.Next(minDays, maxDays + 1);

            // Long-term partners get a small scheduling advantage so regular supply feels dependable rather than punitive.
            if (relation.Trust >= 50)
                deadlineDays += 1;

            ClientProfileDefinition? profile = Mod.Clients.GetProfile(clientKey);
            Mod.State.AvailableContracts.Add(new CompanyContract
            {
                TemplateKey = template.Key,
                ClientKey = clientKey,
                ClientName = profile?.DisplayName ?? template.ClientName,
                ProductKey = template.ProductKey,
                ContractKind = Mod.Clients.GetContractKind(clientKey),
                RequiredQuantity = quantity,
                DeliveredQuantity = 0,
                MinimumQuality = quality,
                RewardGold = Math.Max(1, reward),
                ReputationReward = quality >= 2 ? 3 : quality >= 1 ? 2 : 1,
                FailureReputationPenalty = quality >= 2 ? 2 : 1,
                CreatedDayNumber = today,
                DeadlineDayNumber = today + deadlineDays
            });
        }
    }

    private void MigrateClientKey(CompanyContract contract)
    {
        if (!string.IsNullOrWhiteSpace(contract.ClientKey))
            return;

        ContractTemplateDefinition? template = Mod.ContractTemplates.FirstOrDefault(p => string.Equals(p.Key, contract.TemplateKey, StringComparison.OrdinalIgnoreCase));
        if (template is not null)
            contract.ClientKey = ResolveClientKey(template);

        if (string.IsNullOrWhiteSpace(contract.ClientKey))
        {
            ClientProfileDefinition? profile = Mod.ClientProfiles.FirstOrDefault(p => string.Equals(p.DisplayName, contract.ClientName, StringComparison.CurrentCultureIgnoreCase));
            contract.ClientKey = profile?.Key ?? contract.ClientName;
        }

        if (string.IsNullOrWhiteSpace(contract.ContractKind))
            contract.ContractKind = Mod.Clients.GetContractKind(contract.ClientKey);
    }

    private string ResolveClientKey(ContractTemplateDefinition template)
    {
        if (!string.IsNullOrWhiteSpace(template.ClientKey))
            return template.ClientKey;

        ClientProfileDefinition? profile = Mod.ClientProfiles.FirstOrDefault(p => string.Equals(p.DisplayName, template.ClientName, StringComparison.CurrentCultureIgnoreCase));
        return profile?.Key ?? template.ClientName;
    }

    private static int RollMinimumQuality(Random random, int level, int trust)
    {
        int roll = random.Next(100);
        int partnerBonus = trust >= 80 ? 8 : trust >= 50 ? 4 : 0;
        if (level >= 5 && roll < 8 + partnerBonus / 2)
            return 4;
        if (level >= 3 && roll < 25 + partnerBonus)
            return 2;
        if (level >= 2 && roll < 48 + partnerBonus)
            return 1;
        return 0;
    }

    internal static string QualityRequirementText(int quality) => quality switch
    {
        1 => "은별 이상",
        2 => "금별 이상",
        4 => "이리듐별",
        _ => "품질 무관"
    };

    internal static int GetCurrentDayNumber()
    {
        int season = Game1.currentSeason switch
        {
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };
        return (Math.Max(1, Game1.year) - 1) * 112 + season * 28 + Math.Max(1, Game1.dayOfMonth);
    }
}
