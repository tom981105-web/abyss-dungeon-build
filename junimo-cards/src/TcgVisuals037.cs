using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

internal static class TcgVisuals037
{
    private static Texture2D? ArtAtlas;
    private static Texture2D? BoosterPack;

    private static readonly string[] ArtKeys =
    {
        "parsnip","potato","green_slime","joja_cola","sardine",
        "dandelion","stone","copper_ore","field_snack","chicken",
        "strawberry","blue_jazz","cave_fly","dust_sprite","purple_shorts",
        "junimo","minecart","ancient_seed","skull_cavern","shadow_brute",
        "krobus","galaxy_sword","dinosaur","witch","mermaid",
        "golden_pumpkin","mr_qi","stardrop","prismatic_shard","grandpas_shrine"
    };

    private static readonly Dictionary<string, int> ArtIndex =
        ArtKeys.Select((key, i) => (key, i))
            .ToDictionary(p => p.key, p => p.i, StringComparer.OrdinalIgnoreCase);

    private static readonly Color Paper = new(248, 232, 194);
    private static readonly Color Paper2 = new(237, 214, 167);
    private static readonly Color Deep = new(61, 41, 27);

    internal static void Initialize(IModHelper helper, IMonitor monitor)
    {
        try
        {
            ArtAtlas = helper.ModContent.Load<Texture2D>("assets/tcg_art_atlas.png");
            BoosterPack = helper.ModContent.Load<Texture2D>("assets/tcg_booster_pack.png");
            monitor.Log("Junimo Cards 0.3.7 TCG illustration assets loaded (30-card atlas + booster pack).", LogLevel.Info);
        }
        catch (Exception ex)
        {
            monitor.Log($"TCG art assets could not be loaded; generated fallback panels will be used. {ex.Message}", LogLevel.Warn);
            ArtAtlas = null;
            BoosterPack = null;
        }
    }

    internal static void DrawBoosterPack(SpriteBatch b, Rectangle dest)
    {
        if (BoosterPack is not null)
        {
            b.Draw(BoosterPack, dest, Color.White);
            CardUi.Border(b, dest, new Color(226, 178, 68), 3);
            return;
        }

        CardUi.DrawCardBack(b, dest);
    }

    internal static void DrawCardBack(SpriteBatch b, Rectangle r, bool pulse = false)
    {
        Color outer = new(224, 171, 62);
        Color inner = new(245, 222, 147);
        Rectangle shadow = new(r.X + 4, r.Y + 5, r.Width, r.Height);
        b.Draw(Game1.fadeToBlackRect, shadow, Color.Black * 0.18f);
        b.Draw(Game1.fadeToBlackRect, r, new Color(22, 58, 42));
        CardUi.Border(b, r, outer, pulse ? 6 : 4);
        Rectangle innerRect = new(r.X + 9, r.Y + 9, r.Width - 18, r.Height - 18);
        CardUi.Border(b, innerRect, inner, 2);

        int artH = Math.Max(70, (int)(r.Height * 0.52f));
        Rectangle emblem = new(r.X + 16, r.Y + 28, r.Width - 32, artH);
        b.Draw(Game1.fadeToBlackRect, emblem, new Color(38, 93, 58));
        CardUi.Border(b, emblem, new Color(84, 143, 89), 2);
        CardUi.CenterText(b, Game1.dialogueFont, "✦", emblem, new Color(244, 209, 107), 1.18f);
        CardUi.CenterText(b, Game1.smallFont, "JUNIMO CARDS",
            new Rectangle(r.X + 9, r.Bottom - 42, r.Width - 18, 28), Color.White, 0.95f);
    }

    internal static void DrawCard(
        SpriteBatch b,
        Rectangle r,
        CardDefinition card,
        string variant,
        string condition,
        int value,
        int count = 0,
        bool selected = false,
        bool dramatic = false)
    {
        Color rarity = CardUi.RarityColor(card.Rarity);
        int rank = ModEntry.GetRarityRank(card.Rarity);

        int shadowPad = dramatic ? 7 : 4;
        b.Draw(Game1.fadeToBlackRect,
            new Rectangle(r.X + shadowPad, r.Y + shadowPad, r.Width, r.Height),
            Color.Black * (dramatic ? 0.26f : 0.17f));

        Color body = rank >= 4 ? new Color(255, 239, 191) : Paper;
        b.Draw(Game1.fadeToBlackRect, r, body);

        int border = dramatic ? 6 : selected ? 5 : 3;
        Color frame = variant switch
        {
            "Gold" => new Color(230, 170, 45),
            "Rainbow" => new Color(233, 108, 183),
            _ => rarity
        };
        CardUi.Border(b, r, frame, border);

        if (selected)
        {
            Rectangle sel = new(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6);
            CardUi.Border(b, sel, new Color(255, 214, 87), 2);
        }

        int pad = Math.Max(6, r.Width / 24);
        int headerH = Math.Clamp(r.Height / 9, 20, 34);
        Rectangle header = new(r.X + pad, r.Y + pad, r.Width - pad * 2, headerH);
        b.Draw(Game1.fadeToBlackRect, header, rarity);
        CardUi.CenterText(b, Game1.smallFont,
            $"{ModEntry.RarityName(card.Rarity)}   {card.SetNo}",
            header, Color.White, Math.Min(1.28f, 0.92f + r.Width / 650f));

        int artTop = header.Bottom + Math.Max(5, r.Height / 55);
        int artH = Math.Clamp((int)(r.Height * 0.42f), 55, 118);
        Rectangle art = new(r.X + pad + 3, artTop, r.Width - pad * 2 - 6, artH);
        DrawArt(b, card.Key, art, rarity);
        CardUi.Border(b, art, new Color(255, 245, 215), 2);

        DrawVariantOverlay(b, art, variant, frame);

        int nameY = art.Bottom + Math.Max(3, r.Height / 70);
        int nameH = Math.Clamp(r.Height / 7, 24, 42);
        CardUi.CenterText(b, Game1.dialogueFont, card.Name,
            new Rectangle(r.X + pad, nameY, r.Width - pad * 2, nameH),
            Deep, Math.Min(0.96f, 0.72f + r.Width / 950f));

        int metaY = nameY + nameH;
        int metaH = Math.Clamp(r.Height / 10, 18, 30);
        CardUi.CenterText(b, Game1.smallFont,
            $"{card.Category} · {ModEntry.VariantName(variant)}",
            new Rectangle(r.X + pad, metaY, r.Width - pad * 2, metaH),
            rarity, Math.Min(1.16f, 0.90f + r.Width / 800f));

        int conditionY = metaY + metaH;
        int bottomReserve = Math.Clamp(r.Height / 7, 26, 40);
        if (conditionY + 18 < r.Bottom - bottomReserve)
        {
            CardUi.CenterText(b, Game1.smallFont, condition,
                new Rectangle(r.X + pad, conditionY, r.Width - pad * 2, 22),
                CardUi.Muted, Math.Min(1.12f, 0.88f + r.Width / 850f));
        }

        string bottom = count > 0 ? $"{value:N0}G  ·  보유 {count}장" : $"{value:N0}G";
        Rectangle priceRect = new(r.X + pad, r.Bottom - bottomReserve, r.Width - pad * 2, bottomReserve - 4);
        b.Draw(Game1.fadeToBlackRect, priceRect, Paper2 * 0.70f);
        CardUi.CenterText(b, Game1.smallFont, bottom, priceRect,
            rank >= 4 ? new Color(132, 79, 18) : CardUi.GreenDark,
            Math.Min(1.18f, 0.92f + r.Width / 850f));

        if (rank >= 4)
            DrawCornerSparkles(b, r, variant, dramatic);
    }

    private static void DrawArt(SpriteBatch b, string key, Rectangle dest, Color fallback)
    {
        if (ArtAtlas is not null && ArtIndex.TryGetValue(key, out int index))
        {
            int col = index % 5;
            int row = index / 5;
            Rectangle src = new(col * 256, row * 160, 256, 160);
            b.Draw(ArtAtlas, dest, src, Color.White);
            return;
        }

        b.Draw(Game1.fadeToBlackRect, dest, fallback * 0.36f);
        Rectangle glow = new(dest.X + dest.Width / 6, dest.Y + dest.Height / 5,
            dest.Width * 2 / 3, dest.Height * 3 / 5);
        b.Draw(Game1.fadeToBlackRect, glow, Color.White * 0.13f);
        CardUi.CenterText(b, Game1.dialogueFont, "✦", dest, fallback, 1.25f);
    }

    private static void DrawVariantOverlay(SpriteBatch b, Rectangle art, string variant, Color frame)
    {
        if (variant == "Normal")
            return;

        float time = (float)Game1.currentGameTime.TotalGameTime.TotalSeconds;
        if (variant == "Holo")
        {
            int x = art.X + (int)((time * 90f) % Math.Max(1, art.Width + 70)) - 35;
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x, art.Y, 24, art.Height), Color.White * 0.16f);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x + 18, art.Y, 8, art.Height), new Color(150, 220, 255) * 0.16f);
        }
        else if (variant == "Gold")
        {
            CardUi.Border(b, new Rectangle(art.X - 3, art.Y - 3, art.Width + 6, art.Height + 6),
                new Color(242, 196, 62) * 0.90f, 3);
            b.Draw(Game1.fadeToBlackRect, art, new Color(255, 215, 95) * 0.08f);
        }
        else if (variant == "Rainbow")
        {
            Color[] colors =
            {
                new(255, 86, 92), new(255, 174, 61), new(244, 227, 75),
                new(72, 201, 112), new(76, 163, 246), new(171, 93, 225)
            };
            int t = Math.Max(2, Math.Min(5, art.Width / 40));
            int seg = Math.Max(1, art.Width / colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                b.Draw(Game1.fadeToBlackRect,
                    new Rectangle(art.X + i * seg, art.Y, i == colors.Length - 1 ? art.Right - (art.X + i * seg) : seg, t),
                    colors[i]);
            }
            b.Draw(Game1.fadeToBlackRect, art, frame * 0.07f);
        }
    }

    private static void DrawCornerSparkles(SpriteBatch b, Rectangle r, string variant, bool dramatic)
    {
        float time = (float)Game1.currentGameTime.TotalGameTime.TotalSeconds;
        int count = dramatic ? 8 : 4;
        for (int i = 0; i < count; i++)
        {
            float phase = time * 1.4f + i * 1.71f;
            int x = i % 2 == 0 ? r.X + 10 + (i * 17) % Math.Max(12, r.Width / 3) : r.Right - 12 - (i * 13) % Math.Max(12, r.Width / 3);
            int y = r.Y + 18 + (i * 29) % Math.Max(30, r.Height - 45);
            int s = 2 + (int)((MathF.Sin(phase) + 1f) * 1.5f);
            Color c = variant == "Rainbow"
                ? TcgUi037.RainbowColor(i, time)
                : new Color(255, 236, 162);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x - s * 2, y - 1, s * 4, 2), c * 0.65f);
            b.Draw(Game1.fadeToBlackRect, new Rectangle(x - 1, y - s * 2, 2, s * 4), c * 0.65f);
        }
    }
}
