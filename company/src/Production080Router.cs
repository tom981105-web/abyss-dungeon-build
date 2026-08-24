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
        // 0.9.2 keeps the proven post-click timing, but now opens the image-backed
        // production screen. The authored PNG itself is the visible UI.
        Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not CompanyMenu company)
            return;

        int selectedTab = CompanySelectedTabField?.GetValue(company) is int value ? value : -1;
        if (selectedTab != 1)
            return;

        if (Mod.Helper.Input.IsDown(SButton.MouseLeft))
            return;

        OpenProductionMenu();
    }

    private void OpenProductionMenu()
    {
        if (Game1.activeClickableMenu is Production092Menu)
            return;

        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new Production092Menu(Mod);
    }
}
