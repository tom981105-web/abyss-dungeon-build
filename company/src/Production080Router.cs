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

    internal Production080Router(ModEntry mod)
    {
        Mod = mod;
    }

    internal void Initialize()
    {
        // Register before the legacy 0.7.6 layout controller. Once this replaces the active
        // CompanyMenu, the older production-route handler no longer matches and can't reopen
        // the legacy Production2Menu.
        Mod.Helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || e.Button != SButton.MouseLeft || Game1.activeClickableMenu is not CompanyMenu company)
            return;
        if (CompanyTabsField?.GetValue(company) is not List<(string Name, Rectangle Bounds)> tabs || tabs.Count < 2)
            return;
        if (!tabs[1].Bounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
            return;

        Mod.Helper.Input.Suppress(e.Button);
        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new Production080Menu(Mod);
    }
}
