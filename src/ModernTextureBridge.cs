using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace WatermelonGeneticsCore;

/// <summary>Loads the reorganized v0.9+ crop textures referenced by bundled content.json.</summary>
internal sealed class ModernTextureBridge
{
    private static readonly string[] Paths =
    {
        "assets/crops/watermelon/base_growth.png",
        "assets/crops/watermelon/base_items.png",
        "assets/crops/watermelon/giant.png",
        "assets/crops/watermelon/variety_growth.png",
        "assets/crops/watermelon/variety_items.png",
        "assets/crops/korean_melon/base_growth.png",
        "assets/crops/korean_melon/base_items.png",
        "assets/crops/korean_melon/white_growth.png",
        "assets/crops/korean_melon/white_items.png",
        "assets/crops/korean_melon/golden_growth.png",
        "assets/crops/korean_melon/golden_items.png",
        "assets/crops/korean_melon/mini_growth.png",
        "assets/crops/korean_melon/mini_items.png",
        "assets/crops/napa_cabbage/base_growth.png",
        "assets/crops/napa_cabbage/base_items.png",
        "assets/machines/seed_hybridizer.png"
    };

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        foreach (string path in Paths)
        {
            if (!e.NameWithoutLocale.IsEquivalentTo(path))
                continue;

            e.LoadFromModFile<Texture2D>(path, AssetLoadPriority.Exclusive);
            return;
        }
    }
}
