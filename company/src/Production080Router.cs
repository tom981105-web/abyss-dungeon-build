using System.Reflection;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class Production080Router
{
    private readonly ModEntry Mod;
    private static readonly FieldInfo? CompanySelectedTabField = typeof(CompanyMenu).GetField("SelectedTab", BindingFlags.Instance | BindingFlags.NonPublic);

    internal Production080Router(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        // 0.8.2: don't replace menus from ButtonPressed. Stardew still has its own click
        // processing after SMAPI's input event, which can immediately discard a menu that was
        // swapped too early. Instead, let CompanyMenu select the production tab normally, then
        // wait until the mouse button is released and replace it on a later game tick.
        Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not CompanyMenu company)
            return;

        int selectedTab = CompanySelectedTabField?.GetValue(company) is int value ? value : -1;
        if (selectedTab != 1)
            return;

        // Never swap while the click that selected the tab is still held down. This prevents
        // that same click from leaking into the newly created menu and closing/replacing it.
        if (Mod.Helper.Input.IsDown(SButton.MouseLeft))
            return;

        OpenProductionMenu();
    }

    private void OpenProductionMenu()
    {
        if (Game1.activeClickableMenu is Production080Menu)
            return;

        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new Production080Menu(Mod);
    }
}
