namespace AgriculturalCompany;

internal sealed class ClientCore
{
    private readonly ModEntry Mod;

    internal ClientCore(ModEntry mod)
    {
        Mod = mod;
    }

    internal void EnsureState()
    {
        Mod.State.ClientRelationships ??= new Dictionary<string, ClientRelationship>(StringComparer.OrdinalIgnoreCase);

        foreach (ClientProfileDefinition profile in Mod.ClientProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Key))
                continue;

            if (!Mod.State.ClientRelationships.TryGetValue(profile.Key, out ClientRelationship? relation) || relation is null)
            {
                relation = new ClientRelationship { ClientKey = profile.Key };
                Mod.State.ClientRelationships[profile.Key] = relation;
            }
            else if (string.IsNullOrWhiteSpace(relation.ClientKey))
            {
                relation.ClientKey = profile.Key;
            }

            relation.Trust = Math.Clamp(relation.Trust, 0, 100);
            relation.CompletedContracts = Math.Max(0, relation.CompletedContracts);
            relation.FailedContracts = Math.Max(0, relation.FailedContracts);
            relation.OnTimeDeliveries = Math.Max(0, relation.OnTimeDeliveries);
            relation.HighQualityDeliveries = Math.Max(0, relation.HighQualityDeliveries);
            relation.LifetimeRevenue = Math.Max(0, relation.LifetimeRevenue);
            relation.DeliveredUnits = Math.Max(0, relation.DeliveredUnits);
        }
    }

    internal ClientProfileDefinition? GetProfile(string clientKey)
        => Mod.ClientProfiles.FirstOrDefault(p => string.Equals(p.Key, clientKey, StringComparison.OrdinalIgnoreCase));

    internal ClientRelationship GetRelationship(string clientKey)
    {
        EnsureState();
        if (!Mod.State.ClientRelationships.TryGetValue(clientKey, out ClientRelationship? relation) || relation is null)
        {
            relation = new ClientRelationship { ClientKey = clientKey };
            Mod.State.ClientRelationships[clientKey] = relation;
        }
        return relation;
    }

    internal IReadOnlyList<ClientProfileDefinition> GetVisibleClients()
        => Mod.ClientProfiles
            .OrderBy(p => p.RequiredCompanyLevel)
            .ThenBy(p => p.DisplayName, StringComparer.CurrentCulture)
            .ToList();

    internal int GetTierIndex(int trust)
        => trust >= 80 ? 3 : trust >= 50 ? 2 : trust >= 20 ? 1 : 0;

    internal string GetTierName(int trust) => GetTierIndex(trust) switch
    {
        1 => "단골 거래처",
        2 => "우수 파트너",
        3 => "핵심 파트너",
        _ => "신규 거래처"
    };

    internal int GetRewardBonusPercent(string clientKey)
    {
        int tier = GetTierIndex(GetRelationship(clientKey).Trust);
        return tier switch
        {
            1 => 5,
            2 => 12,
            3 => 20,
            _ => 0
        };
    }

    internal int GetQuantityBonusPercent(string clientKey)
    {
        int tier = GetTierIndex(GetRelationship(clientKey).Trust);
        return tier switch
        {
            1 => 8,
            2 => 18,
            3 => 30,
            _ => 0
        };
    }

    internal string GetContractKind(string clientKey)
    {
        int trust = GetRelationship(clientKey).Trust;
        return trust >= 80 ? "핵심" : trust >= 50 ? "우선" : trust >= 20 ? "정기" : "일반";
    }

    internal int GetNextTierTrust(int trust)
        => trust < 20 ? 20 : trust < 50 ? 50 : trust < 80 ? 80 : 100;

    internal void RecordCompletedContract(CompanyContract contract, int currentDayNumber)
    {
        if (string.IsNullOrWhiteSpace(contract.ClientKey))
            return;

        ClientRelationship relation = GetRelationship(contract.ClientKey);
        ClientProfileDefinition? profile = GetProfile(contract.ClientKey);

        int trustGain = Math.Max(1, profile?.CompletionTrust ?? 6);
        if (contract.MinimumQuality >= 2)
            trustGain += 2;
        else if (contract.MinimumQuality >= 1)
            trustGain += 1;

        int daysEarly = Math.Max(0, contract.DeadlineDayNumber - currentDayNumber);
        if (daysEarly >= 2)
            trustGain += 1;

        relation.Trust = Math.Clamp(relation.Trust + trustGain, 0, 100);
        relation.CompletedContracts++;
        relation.OnTimeDeliveries++;
        if (contract.MinimumQuality >= 2)
            relation.HighQualityDeliveries++;
        relation.LifetimeRevenue += Math.Max(0, contract.RewardGold);
        relation.DeliveredUnits += Math.Max(0, contract.RequiredQuantity);
        relation.LastContractDayNumber = currentDayNumber;
    }

    internal void RecordFailedContract(CompanyContract contract, int currentDayNumber)
    {
        if (string.IsNullOrWhiteSpace(contract.ClientKey))
            return;

        ClientRelationship relation = GetRelationship(contract.ClientKey);
        ClientProfileDefinition? profile = GetProfile(contract.ClientKey);
        int penalty = Math.Max(1, profile?.FailureTrustPenalty ?? 7);

        relation.Trust = Math.Clamp(relation.Trust - penalty, 0, 100);
        relation.FailedContracts++;
        relation.LastContractDayNumber = currentDayNumber;
    }
}
