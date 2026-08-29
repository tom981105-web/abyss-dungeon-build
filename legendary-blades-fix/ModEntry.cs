using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Shops;
using StardewValley.GameData.Weapons;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;

namespace LegendaryBlades;

public sealed class ModEntry : Mod
{
    internal const string VersionText = "1.1.0";
    internal const string ModPrefix = "Deyvid.LegendaryBlades";
    internal const string TextureAsset = ModPrefix + "/Weapons";
    internal const string GalaxySoulId = "(O)896";
    internal const string CinderShardId = "(O)848";
    internal const string GalaxySoulDataKey = ModPrefix + "/GalaxySouls";
    internal const string Mine100Key = ModPrefix + "/Mine100Reached";
    internal const string Skull150Key = ModPrefix + "/Skull150Reached";
    internal const string Mail1Id = ModPrefix + "_Marlon4Hearts"; // legacy ID retained for save compatibility; progression is NOT friendship-based.
    internal const string Mail2Id = ModPrefix + "_Marlon8Hearts"; // legacy ID retained for save compatibility; progression is NOT friendship-based.

    internal static ModEntry? Instance { get; private set; }
    internal static readonly Dictionary<string, BladeData> Blades = BladeData.CreateAll().ToDictionary(p => p.SetId, StringComparer.OrdinalIgnoreCase);

    private bool pendingCinderCharge;
    private int pendingCinderBefore;

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        helper.ConsoleCommands.Add("legendaryblades_give", "Give a Legendary Blade. Usage: legendaryblades_give <emberfang|frostveil|voidreaver|stormcaller|verdant|bloodmoon|sunforged|soulrender|all>", GiveCommand);
        helper.ConsoleCommands.Add("legendaryblades_give_evolved", "Give evolved Legendary Blades. Usage: legendaryblades_give_evolved <set id|all>", GiveEvolvedCommand);
        helper.ConsoleCommands.Add("legendaryblades_visual", "Give all 16 Legendary Blades for visual comparison.", GiveVisualCommand);
        helper.ConsoleCommands.Add("legendaryblades_forgekit", "Give 24 Galaxy Souls and 480 Cinder Shards for testing.", ForgeKitCommand);
        helper.ConsoleCommands.Add("legendaryblades_diag", "Print Legendary Blades diagnostics.", DiagCommand);

        new Harmony(ModManifest.UniqueID).PatchAll();
        Monitor.Log($"Legendary Blades v{VersionText} loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        ReconcileProgressionState();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        ReconcileProgressionState();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer || e.NewLocation is not MineShaft mine)
            return;

        int level = mine.mineLevel;
        if (level >= 100 && level < 121 && !Game1.player.modData.ContainsKey(Mine100Key))
        {
            Game1.player.modData[Mine100Key] = "true";
            Monitor.Log("Regular Mines floor 100 reached. Marlon's first letter will arrive the next morning.", LogLevel.Info);
            QueueMailForTomorrow(Mail1Id);
        }

        // Skull Cavern starts at internal mine level 121, so floor 150 is internal level 270.
        if (level >= 270 && !Game1.player.modData.ContainsKey(Skull150Key))
        {
            Game1.player.modData[Skull150Key] = "true";
            Monitor.Log("Skull Cavern floor 150 reached. Marlon's second letter is now unlocked.", LogLevel.Info);
            ReconcileProgressionState();
        }
    }

    private void ReconcileProgressionState()
    {
        if (!Context.IsWorldReady)
            return;

        // Preserve progress from older builds which used the same mail IDs but had misleading 'heart' names.
        if (Game1.player.mailReceived.Contains(Mail1Id))
            Game1.player.modData[Mine100Key] = "true";
        if (Game1.player.mailReceived.Contains(Mail2Id))
        {
            Game1.player.modData[Mine100Key] = "true";
            Game1.player.modData[Skull150Key] = "true";
        }

        if (Game1.player.modData.ContainsKey(Mine100Key) && !Game1.player.hasOrWillReceiveMail(Mail1Id))
            QueueMailForTomorrow(Mail1Id);

        if (Game1.player.modData.ContainsKey(Skull150Key)
            && Game1.player.mailReceived.Contains(Mail1Id)
            && !Game1.player.hasOrWillReceiveMail(Mail2Id))
            QueueMailForTomorrow(Mail2Id);
    }

    private void QueueMailForTomorrow(string mailId)
    {
        if (Game1.player.hasOrWillReceiveMail(mailId))
            return;

        Game1.addMailForTomorrow(mailId, noLetter: false, sendToEveryone: false);
        Monitor.Log(mailId == Mail1Id
            ? "Marlon's first Legendary Blades letter queued for tomorrow."
            : "Marlon's second Legendary Blades letter queued for tomorrow.", LogLevel.Info);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(TextureAsset))
        {
            e.LoadFromModFile<Texture2D>("assets/weapons.png", AssetLoadPriority.Exclusive);
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo("Data/Weapons"))
            e.Edit(PatchWeaponData, AssetEditPriority.Late);
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            e.Edit(PatchShopData, AssetEditPriority.Late);
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            e.Edit(PatchMailData, AssetEditPriority.Late);
    }

    private void PatchWeaponData(IAssetData asset)
    {
        var data = asset.AsDictionary<string, WeaponData>().Data;
        foreach (BladeData blade in Blades.Values)
        {
            data[blade.BaseItemId] = CreateWeaponData(blade, evolved: false);
            data[blade.EvolvedItemId] = CreateWeaponData(blade, evolved: true);
        }
    }

    private WeaponData CreateWeaponData(BladeData blade, bool evolved)
    {
        WeaponStats s = evolved ? blade.Evolved : blade.Base;
        return new WeaponData
        {
            Name = evolved ? blade.EvolvedFallbackName : blade.BaseFallbackName,
            DisplayName = T(evolved ? blade.EvolvedNameKey : blade.BaseNameKey, evolved ? blade.EvolvedFallbackName : blade.BaseFallbackName),
            Description = T(evolved ? blade.EvolvedDescKey : blade.BaseDescKey, evolved ? "An awakened Legendary Blade." : "A Legendary Blade."),
            Type = 0,
            Texture = TextureAsset,
            SpriteIndex = evolved ? blade.EvolvedSpriteIndex : blade.BaseSpriteIndex,
            MinDamage = s.MinDamage,
            MaxDamage = s.MaxDamage,
            Knockback = s.Knockback,
            Speed = s.Speed,
            Precision = s.Precision,
            Defense = s.Defense,
            AreaOfEffect = s.AreaOfEffect,
            CritChance = s.CritChance,
            CritMultiplier = s.CritMultiplier,
            CanBeLostOnDeath = false,
            MineBaseLevel = -1,
            MineMinLevel = -1
        };
    }

    private void PatchShopData(IAssetData asset)
    {
        var shops = asset.AsDictionary<string, ShopData>().Data;
        if (!shops.TryGetValue("AdventureShop", out ShopData? shop) || shop.Items is null)
            return;

        shop.Items.RemoveAll(p => p.Id?.StartsWith(ModPrefix + "_", StringComparison.Ordinal) is true);
        string condition = $"PLAYER_HAS_MAIL Current {Mail1Id} Received";
        foreach (BladeData blade in Blades.Values)
        {
            shop.Items.Add(new ShopItemData
            {
                Id = blade.BaseItemId,
                ItemId = "(W)" + blade.BaseItemId,
                Price = blade.Price,
                Condition = condition
            });
        }
    }

    private void PatchMailData(IAssetData asset)
    {
        var mail = asset.AsDictionary<string, string>().Data;
        mail[Mail1Id] = T("mail.marlon4.text", "@,^I have been watching your progress as a member of the Adventurer's Guild.^^When the time comes, I will tell you the rest.^^— Marlon");
        mail[Mail2Id] = T("mail.marlon8.text", "@,^I think I have waited long enough.^^The form you know is not the final form of those weapons.^^— Marlon");
    }

    internal static string T(string key, string fallback, object? tokens = null)
    {
        ModEntry? mod = Instance;
        if (mod is null)
            return fallback;

        Translation value = tokens is null ? mod.Helper.Translation.Get(key) : mod.Helper.Translation.Get(key, tokens);
        string text = value.ToString();
        return string.IsNullOrWhiteSpace(text) || string.Equals(text, key, StringComparison.OrdinalIgnoreCase) ? fallback : text;
    }

    internal static BladeData? GetBladeForWeapon(MeleeWeapon weapon, out bool evolved)
    {
        evolved = false;
        string id = weapon.ItemId;
        foreach (BladeData blade in Blades.Values)
        {
            if (string.Equals(id, blade.BaseItemId, StringComparison.Ordinal))
                return blade;
            if (string.Equals(id, blade.EvolvedItemId, StringComparison.Ordinal))
            {
                evolved = true;
                return blade;
            }
        }
        return null;
    }

    internal static int GetGalaxySoulCount(MeleeWeapon weapon)
    {
        if (weapon.modData.TryGetValue(GalaxySoulDataKey, out string? raw) && int.TryParse(raw, out int value))
            return Math.Clamp(value, 0, 3);
        return 0;
    }

    internal static MeleeWeapon MakeWeapon(BladeData blade, bool evolved)
    {
        string qid = "(W)" + (evolved ? blade.EvolvedItemId : blade.BaseItemId);
        return ItemRegistry.Create<MeleeWeapon>(qid);
    }

    internal void ScheduleCinderCharge(int before)
    {
        pendingCinderBefore = before;
        pendingCinderCharge = true;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!pendingCinderCharge || !Context.IsWorldReady)
            return;

        pendingCinderCharge = false;
        int current = Game1.player.Items.CountId(CinderShardId);
        int targetMaximum = Math.Max(0, pendingCinderBefore - 20);

        // Vanilla ForgeMenu normally removes the shard cost outside CraftItem. This fallback only removes
        // the missing amount, so the player is charged exactly 20 and is never double-charged by this mod.
        if (current > targetMaximum)
            Game1.player.Items.ReduceId(CinderShardId, current - targetMaximum);
    }

    internal static void CopyWeaponState(MeleeWeapon source, MeleeWeapon target)
    {
        foreach (var pair in source.modData.Pairs)
        {
            if (!string.Equals(pair.Key, GalaxySoulDataKey, StringComparison.Ordinal))
                target.modData[pair.Key] = pair.Value;
        }

        int copied = 0;
        try
        {
            copied += CopyListField(typeof(Tool), source, target, "enchantments");
            copied += CopyListField(typeof(Tool), source, target, "previousEnchantments");

            // Fallback for game/internal field renames: find compatible enchantment-related IList fields.
            if (copied == 0)
            {
                foreach (FieldInfo field in typeof(Tool).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!field.Name.Contains("enchant", StringComparison.OrdinalIgnoreCase) || !typeof(IList).IsAssignableFrom(field.FieldType))
                        continue;
                    copied += CopyListField(field, source, target);
                }
            }

            MethodInfo? recalc = AccessTools.Method(typeof(MeleeWeapon), "RecalculateAppliedForges", new[] { typeof(bool) });
            recalc?.Invoke(target, new object[] { true });

            Instance?.Monitor.Log(T("forge.preserve.ok", $"Preserved {copied} forge/enchantment effects during evolution.", new { count = copied }), LogLevel.Trace);
        }
        catch (Exception ex)
        {
            Instance?.Monitor.Log(T("forge.preserve.fail", $"Could not copy one forge/enchantment effect: {ex.GetType().Name}.", new { type = ex.GetType().Name }), LogLevel.Warn);
        }
    }

    private static int CopyListField(Type owner, object source, object target, string name)
    {
        FieldInfo? field = AccessTools.Field(owner, name);
        return field is null ? 0 : CopyListField(field, source, target);
    }

    private static int CopyListField(FieldInfo field, object source, object target)
    {
        if (field.GetValue(source) is not IEnumerable sourceValues || field.GetValue(target) is not IList targetList)
            return 0;

        var values = new List<object?>();
        foreach (object? value in sourceValues)
            values.Add(value);

        targetList.Clear();
        foreach (object? value in values)
            targetList.Add(value);
        return values.Count;
    }

    private void GiveCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log(T("cmd.world.required", "Load a save before using this command."), LogLevel.Warn);
            return;
        }

        string id = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? string.Empty;
        if (id == "all")
        {
            foreach (BladeData blade in Blades.Values)
                GiveItem(MakeWeapon(blade, false));
            Monitor.Log(T("cmd.base.all", "All 8 base Legendary Blades were added for testing."), LogLevel.Info);
            return;
        }

        if (!Blades.TryGetValue(id, out BladeData? selected))
        {
            Monitor.Log(T("cmd.notfound", "Weapon not found."), LogLevel.Warn);
            return;
        }

        MeleeWeapon weapon = MakeWeapon(selected, false);
        GiveItem(weapon);
        Monitor.Log(T("cmd.give.ok", $"Weapon added: {weapon.DisplayName}", new { name = weapon.DisplayName }), LogLevel.Info);
    }

    private void GiveEvolvedCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
            return;
        string id = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "all";
        if (id == "all")
        {
            foreach (BladeData blade in Blades.Values)
                GiveItem(MakeWeapon(blade, true));
            Monitor.Log(T("cmd.evolved.all", "All 8 evolved Legendary Blades were added for testing."), LogLevel.Info);
            return;
        }
        if (Blades.TryGetValue(id, out BladeData? blade))
            GiveItem(MakeWeapon(blade, true));
        else
            Monitor.Log(T("cmd.notfound", "Weapon not found."), LogLevel.Warn);
    }

    private void GiveVisualCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
            return;
        foreach (BladeData blade in Blades.Values)
        {
            GiveItem(MakeWeapon(blade, false));
            GiveItem(MakeWeapon(blade, true));
        }
        Monitor.Log(T("cmd.visual.all", "All 16 Legendary Blades were added for comparison."), LogLevel.Info);
    }

    private void ForgeKitCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
            return;
        GiveItem(ItemRegistry.Create(GalaxySoulId, 24));
        GiveItem(ItemRegistry.Create(CinderShardId, 480));
        Monitor.Log(T("cmd.forgekit", "Forge test kit added: 24 Galaxy Souls and 480 Cinder Shards."), LogLevel.Info);
    }

    private void DiagCommand(string command, string[] args)
    {
        Monitor.Log("Legendary Blades v1.1.0 - diagnostics", LogLevel.Info);
        Monitor.Log("Target shop: AdventureShop", LogLevel.Info);
        Monitor.Log($"Texture: {TextureAsset}", LogLevel.Info);
        Monitor.Log("Forge evolution: 3 Galaxy Souls, 20 Cinder Shards per forge, 60 total per sword.", LogLevel.Info);
        Monitor.Log("Progression: Mines floor 100 -> first letter; Skull Cavern floor 150 -> second letter; no friendship requirement.", LogLevel.Info);
        if (Context.IsWorldReady)
            Monitor.Log($"Mail1 received={Game1.player.mailReceived.Contains(Mail1Id)}, Mail2 received={Game1.player.mailReceived.Contains(Mail2Id)}", LogLevel.Info);
    }

    private static void GiveItem(Item item)
    {
        if (!Game1.player.addItemToInventoryBool(item))
            Utility.CollectOrDrop(item);
    }
}

[HarmonyPatch]
internal static class ForgeCraftPatch
{
    private static MethodBase TargetMethod()
        => AccessTools.Method(typeof(ForgeMenu), "CraftItem", new[] { typeof(Item), typeof(Item), typeof(bool) })
           ?? throw new MissingMethodException("ForgeMenu.CraftItem(Item, Item, bool) was not found.");

    private static bool Prefix(Item left_item, Item right_item, bool forReal, ref Item? __result)
    {
        if (left_item is not MeleeWeapon weapon || right_item?.QualifiedItemId != ModEntry.GalaxySoulId)
            return true;

        BladeData? blade = ModEntry.GetBladeForWeapon(weapon, out bool alreadyEvolved);
        if (blade is null || alreadyEvolved)
            return true;

        if (!Game1.player.mailReceived.Contains(ModEntry.Mail2Id))
        {
            if (forReal)
                Game1.showRedMessage(ModEntry.T("story.forge.locked", "These blades have not revealed their secret to you yet."));
            __result = null;
            return false;
        }

        int stage = ModEntry.GetGalaxySoulCount(weapon);
        if (stage >= 3)
            return true;

        if (!Game1.player.Items.ContainsId(ModEntry.CinderShardId, 20))
        {
            if (forReal)
                Game1.showRedMessage("20 Cinder Shards are required.");
            __result = null;
            return false;
        }

        if (!forReal)
        {
            __result = weapon;
            return false;
        }

        int cinderBefore = Game1.player.Items.CountId(ModEntry.CinderShardId);
        ModEntry.Instance?.ScheduleCinderCharge(cinderBefore);

        int nextStage = stage + 1;
        if (nextStage >= 3)
        {
            MeleeWeapon evolved = ModEntry.MakeWeapon(blade, true);
            ModEntry.CopyWeaponState(weapon, evolved);
            __result = evolved;

            string message = ModEntry.T("forge.complete", $"{weapon.DisplayName} has awakened into {evolved.DisplayName}!", new { name = weapon.DisplayName, evolved = evolved.DisplayName });
            Game1.showGlobalMessage(message);
            ModEntry.Instance?.Monitor.Log(message, LogLevel.Info);
        }
        else
        {
            weapon.modData[ModEntry.GalaxySoulDataKey] = nextStage.ToString(System.Globalization.CultureInfo.InvariantCulture);
            __result = weapon;

            string message = ModEntry.T("forge.stage", $"{weapon.DisplayName} absorbed Galaxy Soul {nextStage} of 3.", new { name = weapon.DisplayName, stage = nextStage });
            Game1.showGlobalMessage(message);
            ModEntry.Instance?.Monitor.Log(message, LogLevel.Info);
        }

        return false;
    }
}

internal sealed record WeaponStats(
    int MinDamage,
    int MaxDamage,
    int Speed,
    int Defense,
    float CritChance,
    float CritMultiplier,
    float Knockback = 1f,
    int Precision = 0,
    int AreaOfEffect = 0);

internal sealed record BladeData(
    string SetId,
    string BaseItemId,
    string EvolvedItemId,
    string BaseNameKey,
    string BaseDescKey,
    string EvolvedNameKey,
    string EvolvedDescKey,
    string BaseFallbackName,
    string EvolvedFallbackName,
    int BaseSpriteIndex,
    int EvolvedSpriteIndex,
    int Price,
    WeaponStats Base,
    WeaponStats Evolved)
{
    public static IEnumerable<BladeData> CreateAll()
    {
        yield return B("emberfang", "Emberfang", "InfernoFang", "Emberfang", "Inferno Fang", 0, 8, 30000,
            new(65, 80, -2, 0, .05f, 3.0f), new(85, 110, -1, 1, .08f, 3.2f));
        yield return B("frostveil", "Frostveil", "EternalFrost", "Frostveil", "Eternal Frost", 1, 9, 60000,
            new(70, 90, 3, 2, .05f, 3.0f), new(95, 120, 4, 3, .07f, 3.2f));
        yield return B("voidreaver", "Voidreaver", "AbyssReaver", "Voidreaver", "Abyss Reaver", 2, 10, 750000,
            new(140, 175, 0, 0, .15f, 3.8f), new(175, 215, 0, 0, .18f, 4.0f));
        yield return B("stormcaller", "Stormcaller", "TempestSovereign", "Stormcaller", "Tempest Sovereign", 3, 11, 200000,
            new(90, 115, 4, 0, .10f, 3.2f), new(120, 150, 5, 1, .13f, 3.5f));
        yield return B("verdant", "VerdantEdge", "PrimalEdge", "Verdant Edge", "Primal Edge", 4, 12, 120000,
            new(80, 100, 0, 3, .05f, 3.0f), new(110, 140, 1, 5, .08f, 3.2f));
        yield return B("bloodmoon", "Bloodmoon", "CrimsonEclipse", "Bloodmoon", "Crimson Eclipse", 5, 13, 350000,
            new(105, 135, 0, 0, .12f, 3.5f), new(135, 170, 1, 1, .16f, 3.8f));
        yield return B("sunforged", "Sunforged", "SolarAscendant", "Sunforged", "Solar Ascendant", 6, 14, 1000000,
            new(160, 200, 2, 2, .10f, 3.6f), new(190, 235, 3, 4, .13f, 3.8f));
        yield return B("soulrender", "Soulrender", "EternalSoul", "Soulrender", "Eternal Soul", 7, 15, 500000,
            new(120, 150, 2, 2, .10f, 3.4f), new(150, 190, 3, 4, .13f, 3.6f));
    }

    private static BladeData B(string set, string baseSuffix, string evoSuffix, string baseName, string evoName, int baseSprite, int evoSprite, int price, WeaponStats baseStats, WeaponStats evoStats)
    {
        string normalized = set == "verdant" ? "verdant" : set;
        return new BladeData(
            set,
            ModEntry.ModPrefix + "_" + baseSuffix,
            ModEntry.ModPrefix + "_" + evoSuffix,
            $"weapon.{normalized}.name",
            $"weapon.{normalized}.desc",
            $"weapon.{normalized}_evolved.name",
            $"weapon.{normalized}_evolved.desc",
            baseName,
            evoName,
            baseSprite,
            evoSprite,
            price,
            baseStats,
            evoStats);
    }
}
