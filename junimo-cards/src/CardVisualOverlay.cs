using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace JunimoCards;

internal sealed class CardVisualOverlay
{
    private readonly ModEntry Mod;
    private Texture2D? Atlas;
    private IClickableMenu? LastPackMenu;
    private int LastRevealed;
    private double FlashStartedMs = -10000;
    private int FlashRank;
    private Rectangle FlashCard;

    private static readonly Dictionary<string, int> Frames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["potato"] = 0,
        ["copper_ore"] = 1,
        ["green_slime"] = 2,
        ["stardrop"] = 3,
        ["witch"] = 4
    };

    private static readonly FieldInfo? PullsField = typeof(PackOpeningMenu).GetField("Pulls", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? RevealedField = typeof(PackOpeningMenu).GetField("Revealed", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CollectionPageField = typeof(CardCollectionMenu).GetField("Page", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CollectionSelectedField = typeof(CardCollectionMenu).GetField("SelectedKey", BindingFlags.Instance | BindingFlags.NonPublic);

    internal CardVisualOverlay(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize(IModHelper helper)
    {
        try
        {
            Atlas = helper.ModContent.Load<Texture2D>("assets/featured_cards.png");
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            Mod.Monitor.Log("Junimo Cards 0.2.0 featured-card visual overlay loaded (5 cards).", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Mod.Monitor.Log($"Featured card art could not be loaded; Junimo Cards will keep the 0.1.x fallback card faces. {ex.Message}", LogLevel.Warn);
        }
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Atlas is null || Game1.activeClickableMenu is null)
            return;

        if (Game1.activeClickableMenu is PackOpeningMenu pack)
            DrawPackOpening(e.SpriteBatch, pack);
        else
        {
            LastPackMenu = null;
            LastRevealed = 0;
            if (Game1.activeClickableMenu is CardCollectionMenu collection)
                DrawCollection(e.SpriteBatch, collection);
            else if (Game1.activeClickableMenu is SaleShelfMenu shelf)
                DrawSaleShelf(e.SpriteBatch, shelf);
        }
    }

    private void DrawPackOpening(SpriteBatch b, PackOpeningMenu menu)
    {
        List<CardPull>? pulls = PullsField?.GetValue(menu) as List<CardPull>;
        int revealed = RevealedField?.GetValue(menu) is int value ? value : 0;
        if (pulls is null || pulls.Count == 0)
            return;

        int gap = 16;
        int cw = Math.Min(190, (menu.width - 120 - gap * 4) / 5);
        int ch = 290;
        int total = cw * 5 + gap * 4;
        int startX = menu.xPositionOnScreen + (menu.width - total) / 2;
        int y = menu.yPositionOnScreen + 160;

        if (!ReferenceEquals(LastPackMenu, menu))
        {
            LastPackMenu = menu;
            LastRevealed = revealed;
        }
        else if (revealed > LastRevealed)
        {
            int newIndex = Math.Clamp(revealed - 1, 0, pulls.Count - 1);
            CardDefinition? newCard = Mod.FindCard(pulls[newIndex].CardKey);
            if (newCard is not null)
            {
                FlashRank = ModEntry.GetRarityRank(newCard.Rarity);
                FlashStartedMs = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
                FlashCard = new Rectangle(startX + newIndex * (cw + gap), y, cw, ch);
            }
            LastRevealed = revealed;
        }

        for (int i = 0; i < revealed && i < pulls.Count; i++)
        {
            CardPull pull = pulls[i];
            if (!Frames.ContainsKey(pull.CardKey))
                continue;

            CardDefinition? card = Mod.FindCard(pull.CardKey);
            if (card is null)
                continue;

            Rectangle cardRect = new(startX + i * (cw + gap), y, cw, ch);
            Rectangle artPanel = new(cardRect.X + 18, cardRect.Y + 49, cardRect.Width - 36, 98);
            b.Draw(Game1.fadeToBlackRect, artPanel, CardUi.RarityColor(card.Rarity) * 0.22f);
            Rectangle art = FitSquare(artPanel, 94);
            DrawArt(b, pull.CardKey, art);
            DrawArtFrame(b, art, card.Rarity, pull.Variant);
            DrawVariantEffect(b, art, pull.Variant);

            int rank = ModEntry.GetRarityRank(card.Rarity);
            if (rank >= 2)
                DrawPulseBorder(b, cardRect, CardUi.RarityColor(card.Rarity), 0.45f + rank * 0.06f, 4 + rank);
        }

        DrawRevealFlash(b);
    }

    private void DrawRevealFlash(SpriteBatch b)
    {
        if (FlashRank < 2)
            return;

        double elapsed = Game1.currentGameTime.TotalGameTime.TotalMilliseconds - FlashStartedMs;
        if (elapsed < 0 || elapsed > 760)
            return;

        float t = 1f - (float)(elapsed / 760.0);
        Color color = FlashRank >= 4 ? new Color(255, 205, 70) : FlashRank == 3 ? new Color(184, 95, 226) : new Color(86, 164, 255);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), color * (0.08f * t));

        int expand = (int)((1f - t) * 26f);
        Rectangle outer = new(FlashCard.X - expand, FlashCard.Y - expand, FlashCard.Width + expand * 2, FlashCard.Height + expand * 2);
        CardUi.Border(b, outer, color * Math.Max(0.18f, t), Math.Max(2, 7 - expand / 6));
        if (FlashRank >= 4)
        {
            Rectangle outer2 = new(outer.X - 9, outer.Y - 9, outer.Width + 18, outer.Height + 18);
            CardUi.Border(b, outer2, Color.White * (0.55f * t), 2);
        }
    }

    private void DrawCollection(SpriteBatch b, CardCollectionMenu menu)
    {
        var rows = Mod.GetCollectionRows()
            .OrderByDescending(p => ModEntry.GetRarityRank(p.Card.Rarity))
            .ThenBy(p => p.Card.SetNo)
            .ThenBy(p => p.Variant)
            .ToList();
        if (rows.Count == 0)
            return;

        int page = CollectionPageField?.GetValue(menu) is int value ? value : 0;
        int start = Math.Clamp(page, 0, Math.Max(0, (rows.Count - 1) / 6)) * 6;
        int cardW = 220;
        int cardH = 205;
        int baseX = menu.xPositionOnScreen + 58;
        int baseY = menu.yPositionOnScreen + 138;

        for (int i = 0; i < 6 && start + i < rows.Count; i++)
        {
            var row = rows[start + i];
            if (!Frames.ContainsKey(row.Card.Key))
                continue;
            int col = i % 3;
            int rr = i / 3;
            Rectangle r = new(baseX + col * 236, baseY + rr * 222, cardW, cardH);
            DrawCollectionCardReplacement(b, r, row.Card, row.Variant, row.Condition, row.Count, row.Value);
        }

        string selectedKey = CollectionSelectedField?.GetValue(menu) as string ?? "";
        if (!string.IsNullOrWhiteSpace(selectedKey))
        {
            var selected = rows.FirstOrDefault(p => string.Equals(p.CollectionKey, selectedKey, StringComparison.OrdinalIgnoreCase));
            if (selected.Card is not null && Frames.ContainsKey(selected.Card.Key))
            {
                Rectangle detail = new(menu.xPositionOnScreen + 790, menu.yPositionOnScreen + 138, menu.width - 848, 370);
                DrawDetailReplacement(b, detail, selected.CollectionKey, selected.Card, selected.Variant, selected.Condition, selected.Count, selected.Value);
            }
        }
    }

    private void DrawCollectionCardReplacement(SpriteBatch b, Rectangle r, CardDefinition card, string variant, string condition, int count, int value)
    {
        Color rarity = CardUi.RarityColor(card.Rarity);
        Rectangle body = new(r.X + 10, r.Y + 48, r.Width - 20, r.Height - 58);
        b.Draw(Game1.fadeToBlackRect, body, new Color(247, 229, 185));

        Rectangle art = new(r.X + 18, r.Y + 58, 82, 82);
        DrawArt(b, card.Key, art);
        DrawArtFrame(b, art, card.Rarity, variant);
        DrawVariantEffect(b, art, variant);

        CardUi.CenterText(b, Game1.dialogueFont, card.Name, new Rectangle(r.X + 108, r.Y + 56, r.Width - 120, 42), CardUi.Ink, 0.62f);
        CardUi.CenterText(b, Game1.smallFont, $"{ModEntry.VariantName(variant)} · {condition}", new Rectangle(r.X + 105, r.Y + 98, r.Width - 116, 28), rarity, 0.85f);
        CardUi.CenterText(b, Game1.smallFont, $"보유 {count}장", new Rectangle(r.X + 106, r.Y + 126, r.Width - 118, 25), CardUi.Ink, 0.94f);
        CardUi.CenterText(b, Game1.smallFont, $"시세 {value:N0}G", new Rectangle(r.X + 15, r.Bottom - 38, r.Width - 30, 26), CardUi.Green, 0.98f);
    }

    private void DrawDetailReplacement(SpriteBatch b, Rectangle r, string collectionKey, CardDefinition card, string variant, string condition, int count, int value)
    {
        Color rarity = CardUi.RarityColor(card.Rarity);
        Rectangle inner = new(r.X + 9, r.Y + 9, r.Width - 18, r.Height - 18);
        b.Draw(Game1.fadeToBlackRect, inner, new Color(247, 229, 185));
        CardUi.Border(b, inner, rarity, 3);

        CardUi.CenterText(b, Game1.dialogueFont, card.Name, new Rectangle(r.X + 20, r.Y + 18, r.Width - 40, 43), CardUi.Ink, 0.78f);
        CardUi.CenterText(b, Game1.smallFont, $"{card.SetNo} · {card.Category} · {ModEntry.RarityName(card.Rarity)}", new Rectangle(r.X + 20, r.Y + 62, r.Width - 40, 28), rarity, 0.98f);

        Rectangle art = new(r.X + 24, r.Y + 102, 126, 126);
        DrawArt(b, card.Key, art);
        DrawArtFrame(b, art, card.Rarity, variant);
        DrawVariantEffect(b, art, variant);

        int tx = r.X + 170;
        CardUi.Text(b, $"변형 {ModEntry.VariantName(variant)}", new Vector2(tx, r.Y + 105), CardUi.Ink, 0.98f);
        CardUi.Text(b, $"상태 {condition}", new Vector2(tx, r.Y + 139), CardUi.Ink, 0.98f);
        CardUi.Text(b, $"보유 {count}장", new Vector2(tx, r.Y + 173), CardUi.Ink, 0.98f);
        CardUi.Text(b, $"진열 {Mod.GetListedCount(collectionKey)}장", new Vector2(tx, r.Y + 207), CardUi.Ink, 0.98f);
        CardUi.CenterText(b, Game1.dialogueFont, $"{value:N0}G", new Rectangle(r.X + 24, r.Y + 240, r.Width - 48, 48), CardUi.Green, 0.78f);
        CardUi.CenterText(b, Game1.smallFont, card.Flavor, new Rectangle(r.X + 24, r.Bottom - 64, r.Width - 48, 44), CardUi.Muted, 0.86f);
    }

    private void DrawSaleShelf(SpriteBatch b, SaleShelfMenu menu)
    {
        int y = menu.yPositionOnScreen + 140;
        for (int i = 0; i < Mod.State.SaleShelf.Count && i < Mod.Config.SaleShelfSlots; i++)
        {
            SaleListing listing = Mod.State.SaleShelf[i];
            if (!CardKeys.TryParse(listing.CollectionKey, out string cardKey, out string variant, out string condition) || !Frames.ContainsKey(cardKey))
                continue;
            CardDefinition? card = Mod.FindCard(cardKey);
            if (card is null)
                continue;

            Rectangle row = new(menu.xPositionOnScreen + 62, y + i * 58, menu.width - 124, 50);
            Rectangle art = new(row.X + 8, row.Y + 6, 38, 38);
            DrawArt(b, cardKey, art);
            DrawArtFrame(b, art, card.Rarity, variant);
            Rectangle labelCover = new(row.X + 52, row.Y + 8, row.Width - 290, 34);
            b.Draw(Game1.fadeToBlackRect, labelCover, new Color(247, 229, 185));
            CardUi.Text(b, $"{card.Name} · {ModEntry.VariantName(variant)} · {condition}", new Vector2(labelCover.X + 6, labelCover.Y + 5), CardUi.Ink, 0.98f);
        }
    }

    private void DrawArt(SpriteBatch b, string cardKey, Rectangle dest)
    {
        if (Atlas is null || !Frames.TryGetValue(cardKey, out int frame))
            return;
        Rectangle source = new(frame * 64, 0, 64, 64);
        b.Draw(Atlas, dest, source, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.995f);
    }

    private static Rectangle FitSquare(Rectangle area, int maxSize)
    {
        int size = Math.Min(maxSize, Math.Min(area.Width, area.Height));
        return new Rectangle(area.Center.X - size / 2, area.Center.Y - size / 2, size, size);
    }

    private static void DrawArtFrame(SpriteBatch b, Rectangle art, string rarity, string variant)
    {
        Color color = variant switch
        {
            "Gold" => new Color(244, 184, 42),
            "Rainbow" => new Color(230, 106, 224),
            "Holo" => new Color(116, 211, 232),
            _ => CardUi.RarityColor(rarity)
        };
        CardUi.Border(b, new Rectangle(art.X - 3, art.Y - 3, art.Width + 6, art.Height + 6), color, variant == "Normal" ? 2 : 4);
    }

    private static void DrawVariantEffect(SpriteBatch b, Rectangle art, string variant)
    {
        if (variant == "Normal")
            return;

        double seconds = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 1000.0;
        int band = Math.Max(8, art.Width / 7);
        int travel = art.Width + band * 2;
        int x = art.X - band + (int)((seconds * 42) % Math.Max(1, travel));
        Color shine = variant switch
        {
            "Gold" => new Color(255, 219, 91),
            "Rainbow" => new Color(255, 118, 224),
            _ => new Color(131, 227, 255)
        };
        Rectangle clipped = Rectangle.Intersect(art, new Rectangle(x, art.Y, band, art.Height));
        if (clipped.Width > 0)
            b.Draw(Game1.fadeToBlackRect, clipped, shine * 0.22f);
        if (variant == "Rainbow")
        {
            int x2 = art.X - band + (int)(((seconds * 42) + travel / 2.0) % Math.Max(1, travel));
            Rectangle clipped2 = Rectangle.Intersect(art, new Rectangle(x2, art.Y, band, art.Height));
            if (clipped2.Width > 0)
                b.Draw(Game1.fadeToBlackRect, clipped2, new Color(104, 242, 182) * 0.18f);
        }
    }

    private static void DrawPulseBorder(SpriteBatch b, Rectangle r, Color color, float baseAlpha, int thickness)
    {
        double seconds = Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 1000.0;
        float pulse = MathHelper.Clamp(baseAlpha + (float)Math.Sin(seconds * 5.2) * 0.17f, 0.18f, 0.92f);
        CardUi.Border(b, new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), color * pulse, thickness);
    }
}
