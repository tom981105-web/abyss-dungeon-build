using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AgriculturalCompany;

internal sealed class Production080Router
{
    private readonly ModEntry Mod;
    private static readonly FieldInfo? CompanyTabsField = typeof(CompanyMenu).GetField("Tabs", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CompanySelectedTabField = typeof(CompanyMenu).GetField("SelectedTab", BindingFlags.Instance | BindingFlags.NonPublic);

    internal Production080Router(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        // The legacy CompanyMenu handles its own click before SMAPI's external input route can
        // reliably replace it on every setup. Keep the direct click intercept for fast routing,
        // and also watch SelectedTab every update tick. If the legacy menu ever changes to the
        // production tab, replace it immediately with the 0.8.x reference-style production menu.
        Mod.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || e.Button != SButton.MouseLeft || Game1.activeClickableMenu is not CompanyMenu company)
            return;

        if (CompanyTabsField?.GetValue(company) is List<(string Name, Rectangle Bounds)> tabs
            && tabs.Count > 1
            && tabs[1].Bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
        {
            Mod.Helper.Input.Suppress(e.Button);
            OpenProductionMenu();
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not CompanyMenu company)
            return;

        int selectedTab = CompanySelectedTabField?.GetValue(company) is int value ? value : -1;
        if (selectedTab == 1)
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
