using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace WatermelonGeneticsCore;

/// <summary>
/// Applies the bundled content.json directly through SMAPI's content API.
/// This lets the mod stay self-contained without requiring Content Patcher.
/// </summary>
internal sealed class ContentBridge
{
    private const string LegacyContentId = "Saebyeol.WatermelonCrop";
    private readonly ModEntry Mod;
    private readonly List<JObject> Changes = new();

    private static readonly Dictionary<string, (string AssetName, string FilePath)> TextureAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["assets/objects.png"] = ("Mods/Saebyeol.WatermelonGenetics/Objects", "assets/objects.png"),
        ["assets/crops.png"] = ("Mods/Saebyeol.WatermelonGenetics/Crops", "assets/crops.png"),
        ["assets/giant.png"] = ("Mods/Saebyeol.WatermelonGenetics/Giant", "assets/giant.png"),
        ["assets/hybridizer.png"] = ("Mods/Saebyeol.WatermelonGenetics/Hybridizer", "assets/hybridizer.png"),
        ["assets/varieties.png"] = ("Mods/Saebyeol.WatermelonGenetics/Varieties", "assets/varieties.png"),
        ["assets/variety_crops.png"] = ("Mods/Saebyeol.WatermelonGenetics/VarietyCrops", "assets/variety_crops.png")
    };

    public ContentBridge(ModEntry mod)
    {
        Mod = mod;
        try
        {
            string path = Path.Combine(mod.Helper.DirectoryPath, "content.json");
            JObject root = JObject.Parse(File.ReadAllText(path));
            foreach (JObject change in root["Changes"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                if (string.Equals((string?)change["Action"], "EditData", StringComparison.OrdinalIgnoreCase))
                    Changes.Add(change);
            }
        }
        catch (Exception ex)
        {
            mod.Monitor.Log($"Failed loading bundled content.json: {ex}", LogLevel.Error);
        }
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        foreach ((string _, (string AssetName, string FilePath) info) in TextureAssets)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(info.AssetName))
            {
                e.LoadFromModFile<Texture2D>(info.FilePath, AssetLoadPriority.Exclusive);
                return;
            }
        }

        foreach (JObject change in Changes)
        {
            string? target = (string?)change["Target"];
            if (string.IsNullOrWhiteSpace(target) || !e.NameWithoutLocale.IsEquivalentTo(target))
                continue;

            e.Edit(asset =>
            {
                try
                {
                    ApplyChange(asset.Data, change);
                }
                catch (Exception ex)
                {
                    Mod.Monitor.Log($"Failed applying standalone data patch to {target}: {ex}", LogLevel.Error);
                }
            }, AssetEditPriority.Default);
        }
    }

    private void ApplyChange(object rootData, JObject change)
    {
        JObject entries = change["Entries"] as JObject ?? new JObject();
        JArray? targetField = change["TargetField"] as JArray;

        if (targetField is null || targetField.Count == 0)
        {
            ApplyDictionaryEntries(rootData, entries);
            return;
        }

        object current = rootData;
        for (int i = 0; i < targetField.Count; i++)
        {
            string field = targetField[i]!.ToString();
            if (i == 0 && current is IDictionary dictionary)
            {
                current = dictionary[field] ?? throw new InvalidOperationException($"Target field root '{field}' wasn't found.");
                continue;
            }

            PropertyInfo? prop = current.GetType().GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            FieldInfo? fld = current.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            current = prop?.GetValue(current) ?? fld?.GetValue(current) ?? throw new InvalidOperationException($"Target field '{field}' wasn't found on {current.GetType().Name}.");
        }

        if (current is IDictionary nestedDictionary)
        {
            ApplyDictionaryEntries(nestedDictionary, entries);
            return;
        }

        if (current is IList list)
        {
            ApplyListEntries(list, entries);
            return;
        }

        throw new InvalidOperationException($"Unsupported target field type {current.GetType().FullName}.");
    }

    private void ApplyDictionaryEntries(object dictionaryObject, JObject entries)
    {
        if (dictionaryObject is not IDictionary dictionary)
            throw new InvalidOperationException($"Asset root {dictionaryObject.GetType().FullName} isn't an IDictionary.");

        Type dictType = dictionaryObject.GetType();
        Type valueType = GetDictionaryValueType(dictType) ?? typeof(object);

        foreach (JProperty entry in entries.Properties())
        {
            string key = ExpandTokens(entry.Name);
            if (entry.Value.Type == JTokenType.Null)
            {
                dictionary.Remove(key);
                continue;
            }

            JToken expanded = ExpandTokens(entry.Value);
            object? value = expanded.ToObject(valueType, JsonSerializer.CreateDefault());
            if (value is not null)
                dictionary[key] = value;
        }
    }

    private void ApplyListEntries(IList list, JObject entries)
    {
        Type listType = list.GetType();
        Type elementType = GetListElementType(listType) ?? list.Cast<object?>().FirstOrDefault()?.GetType() ?? typeof(object);
        PropertyInfo? idProperty = elementType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        foreach (JProperty entry in entries.Properties())
        {
            string id = ExpandTokens(entry.Name);
            JToken expanded = ExpandTokens(entry.Value);
            object? value = expanded.ToObject(elementType, JsonSerializer.CreateDefault());
            if (value is null)
                continue;

            if (idProperty is not null && string.IsNullOrWhiteSpace(idProperty.GetValue(value)?.ToString()) && idProperty.CanWrite)
                idProperty.SetValue(value, id);

            int existingIndex = -1;
            if (idProperty is not null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    object? existing = list[i];
                    if (existing is not null && string.Equals(idProperty.GetValue(existing)?.ToString(), id, StringComparison.OrdinalIgnoreCase))
                    {
                        existingIndex = i;
                        break;
                    }
                }
            }

            if (existingIndex >= 0)
                list[existingIndex] = value;
            else
                list.Add(value);
        }
    }

    private JToken ExpandTokens(JToken token)
    {
        if (token.Type == JTokenType.String)
            return new JValue(ExpandTokens(token.ToString()));

        if (token is JObject obj)
        {
            JObject copy = new();
            foreach (JProperty prop in obj.Properties())
                copy[ExpandTokens(prop.Name)] = ExpandTokens(prop.Value);
            return copy;
        }

        if (token is JArray arr)
            return new JArray(arr.Select(ExpandTokens));

        return token.DeepClone();
    }

    private string ExpandTokens(string value)
    {
        string result = value.Replace("{{ModId}}", LegacyContentId, StringComparison.OrdinalIgnoreCase);

        result = Regex.Replace(result, @"\{\{i18n:([^}]+)\}\}", match =>
        {
            string key = match.Groups[1].Value.Trim();
            return Mod.Helper.Translation.Get(key).ToString();
        }, RegexOptions.IgnoreCase);

        result = Regex.Replace(result, @"\{\{InternalAssetKey:\s*([^}]+)\}\}", match =>
        {
            string path = match.Groups[1].Value.Trim().Replace('\\', '/');
            return TextureAssets.TryGetValue(path, out var info) ? info.AssetName : path;
        }, RegexOptions.IgnoreCase);

        return result;
    }

    private static Type? GetDictionaryValueType(Type type)
    {
        Type? candidate = type.GetInterfaces()
            .Concat(new[] { type })
            .FirstOrDefault(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        return candidate?.GetGenericArguments()[1];
    }

    private static Type? GetListElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();
        Type? candidate = type.GetInterfaces()
            .Concat(new[] { type })
            .FirstOrDefault(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IList<>));
        return candidate?.GetGenericArguments()[0];
    }
}
