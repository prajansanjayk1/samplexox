using System.Windows;
using System.Windows.Media;

namespace QuietoraFocus.Services;

public static class ThemeManager
{
    public static bool IsDarkMode { get; private set; }

    public static void ApplyTheme(bool isDark)
    {
        IsDarkMode = isDark;
        var app = Application.Current;
        if (app == null) return;

        if (isDark)
        {
            // Quietora Labs Flagship Obsidian & Coral Palette
            SetBrush(app, "ThemeWindowBg", Color.FromRgb(14, 16, 21));          // #0E1015 (Deep Obsidian Canvas)
            SetBrush(app, "ThemeSidebarBg", Color.FromRgb(18, 20, 26));         // #12141A (Obsidian Sidebar)
            SetBrush(app, "ThemeCardBg", Color.FromRgb(24, 26, 34));            // #181A22 (Tactile Obsidian Card)
            SetBrush(app, "ThemeCardBorder", Color.FromRgb(39, 43, 54));        // #272B36 (Crisp Line)
            SetBrush(app, "ThemeTextPrimary", Color.FromRgb(248, 250, 252));    // #F8FAFC (Clean High-Contrast Text)
            SetBrush(app, "ThemeTextSecondary", Color.FromRgb(159, 162, 170));  // #9FA2AA (Subtle Muted)
            SetBrush(app, "ThemeInputBg", Color.FromRgb(18, 20, 26));           // #12141A
            SetBrush(app, "ThemeInputBorder", Color.FromRgb(45, 49, 60));       // #2D313C
            SetBrush(app, "ThemeAccent", Color.FromRgb(229, 83, 55));           // #E55337 (Quietora Signature Terracotta Coral!)
            SetBrush(app, "ThemeAccentHover", Color.FromRgb(240, 101, 72));     // #F06548
            SetBrush(app, "ThemeAccentSubtle", Color.FromRgb(42, 24, 21));      // #2A1815 (Coral soft glow tint)
            SetBrush(app, "ThemeHoverBg", Color.FromRgb(34, 37, 46));           // #22252E (Elevated Hover)
            SetBrush(app, "ThemeLine", Color.FromRgb(39, 43, 54));              // #272B36
            SetBrush(app, "ThemeMint", Color.FromRgb(16, 185, 129));            // #10B981 (Mint/Success)
            SetBrush(app, "ThemeMintBg", Color.FromRgb(16, 45, 34));            // #102D22
            SetBrush(app, "ThemeMintBorder", Color.FromRgb(5, 150, 105));       // #059669
            SetBrush(app, "ThemeNavActiveBg", Color.FromRgb(39, 23, 20));       // #271714 (Coral tinted active nav pill)
            SetBrush(app, "ThemeNavActiveBorder", Color.FromRgb(229, 83, 55));   // #E55337
        }
        else
        {
            // Quietora Labs Warm Editorial Paper Palette
            SetBrush(app, "ThemeWindowBg", Color.FromRgb(248, 246, 240));       // #F8F6F0 (Warm Editorial Paper)
            SetBrush(app, "ThemeSidebarBg", Color.FromRgb(241, 237, 227));      // #F1EDE3 (Paper Subtle)
            SetBrush(app, "ThemeCardBg", Color.FromRgb(255, 255, 255));         // #FFFFFF (Paper Card)
            SetBrush(app, "ThemeCardBorder", Color.FromRgb(222, 216, 203));     // #DED8CB (Editorial Line)
            SetBrush(app, "ThemeTextPrimary", Color.FromRgb(18, 19, 23));       // #121317 (Deep Obsidian Ink)
            SetBrush(app, "ThemeTextSecondary", Color.FromRgb(94, 96, 102));    // #5E6066 (Editorial Muted)
            SetBrush(app, "ThemeInputBg", Color.FromRgb(255, 255, 255));        // #FFFFFF
            SetBrush(app, "ThemeInputBorder", Color.FromRgb(222, 216, 203));    // #DED8CB
            SetBrush(app, "ThemeAccent", Color.FromRgb(229, 83, 55));           // #E55337 (Quietora Signature Terracotta Coral!)
            SetBrush(app, "ThemeAccentHover", Color.FromRgb(204, 67, 41));      // #CC4329
            SetBrush(app, "ThemeAccentSubtle", Color.FromRgb(253, 238, 235));   // #FDEEEB
            SetBrush(app, "ThemeHoverBg", Color.FromRgb(234, 229, 218));        // #EAE5DA
            SetBrush(app, "ThemeLine", Color.FromRgb(222, 216, 203));
            SetBrush(app, "ThemeMint", Color.FromRgb(36, 87, 68));              // #245744
            SetBrush(app, "ThemeMintBg", Color.FromRgb(230, 243, 235));         // #E6F3EB
            SetBrush(app, "ThemeMintBorder", Color.FromRgb(191, 225, 206));     // #BFE1CE
            SetBrush(app, "ThemeNavActiveBg", Color.FromRgb(253, 238, 235));
            SetBrush(app, "ThemeNavActiveBorder", Color.FromRgb(229, 83, 55));
        }
    }

    private static void SetBrush(Application app, string key, Color color)
    {
        if (app.Resources.Contains(key))
        {
            if (app.Resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
                return;
            }
        }
        app.Resources[key] = new SolidColorBrush(color);
    }
}
