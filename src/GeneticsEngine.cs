namespace WatermelonGeneticsCore;

public static class GeneticsEngine
{
    public static int GetSuccessChance(VarietyDefinition a, VarietyDefinition b)
    {
        int baseChance = 72;
        int rarityPenalty = Math.Max(0, a.Rarity + b.Rarity - 4) * 4;
        int resistanceBonus = (a.Resistance + b.Resistance - 6) * 2;
        return Math.Clamp(baseChance - rarityPenalty + resistanceBonus, 35, 92);
    }

    public static VarietyDefinition RollResult(VarietyDefinition a, VarietyDefinition b, IReadOnlyList<VarietyDefinition> all, Random random)
    {
        VarietyDefinition common = all.First(v => v.Key == "Common");
        VarietyDefinition honey = all.First(v => v.Key == "Honey");
        VarietyDefinition mini = all.First(v => v.Key == "Mini");
        VarietyDefinition golden = all.First(v => v.Key == "Golden");
        VarietyDefinition starlight = all.First(v => v.Key == "Starlight");

        double r = random.NextDouble();

        if (a.Key == "Starlight" || b.Key == "Starlight")
            return r < 0.50 ? starlight : r < 0.80 ? golden : honey;

        if (a.Key == "Golden" && b.Key == "Golden")
            return r < 0.75 ? golden : r < 0.95 ? starlight : honey;

        if (a.Key == "Honey" && b.Key == "Honey")
            return r < 0.60 ? honey : r < 0.85 ? golden : starlight;

        if (a.Key == "Mini" && b.Key == "Mini")
            return r < 0.75 ? mini : r < 0.90 ? honey : r < 0.98 ? golden : starlight;

        if (a.Key == "Common" && b.Key == "Common")
            return r < 0.70 ? honey : r < 0.90 ? mini : r < 0.98 ? golden : starlight;

        int maxRarity = Math.Max(a.Rarity, b.Rarity);
        if (maxRarity >= 4)
            return r < 0.55 ? golden : r < 0.75 ? starlight : r < 0.90 ? honey : mini;
        if (a.Key == "Mini" || b.Key == "Mini")
            return r < 0.50 ? mini : r < 0.85 ? honey : golden;
        return r < 0.65 ? honey : r < 0.90 ? mini : golden;
    }
}
