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
        // 0.11.0 workshop reboot:
        // 1) intercept the configured company key before the legacy CompanyCore handler;
        // 2) route any legacy CompanyMenu production-tab click back into the new workshop flow.
        Mod.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null)
            return;
        if (!Enum.TryParse<SButton>(Mod.Config.CompanyKey, true, out SButton key) || e.Button != key)
            return;

        Mod.Company.EnsureState();
        Mod.Production.EnsureState();
        Game1.activeClickableMenu = new CompanyWorkshopMenu(Mod);
        Game1.playSound("bigSelect");
        Mod.Helper.Input.Suppress(e.Button);
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

        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new ProductionLineSelectMenu(Mod, new CompanyWorkshopMenu(Mod));
    }
}
