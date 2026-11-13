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
            // Quietora Labs Flagship Obsidian & Coral Palette (Inspo High-Precision Dark Mode)
            SetBrush(app, "ThemeWindowBg", Color.FromRgb(10, 12, 17));          // #0A0C11 (Deep Obsidian Canvas)
            SetBrush(app, "ThemeSidebarBg", Color.FromRgb(14, 17, 24));         // #0E1118 (Tactile Sidebar)
            SetBrush(app, "ThemeCardBg", Color.FromRgb(19, 23, 33));            // #131721 (Bento Container Surface)
            SetBrush(app, "ThemeCardBorder", Color.FromRgb(32, 39, 55));        // #202737 (Crisp Razor-Sharp Border)
            SetBrush(app, "ThemeTextPrimary", Color.FromRgb(248, 250, 252));    // #F8FAFC (Clean High-Contrast Text)
            SetBrush(app, "ThemeTextSecondary", Color.FromRgb(148, 163, 184));  // #94A3B8 (Subtle Slate Muted)
            SetBrush(app, "ThemeInputBg", Color.FromRgb(14, 17, 24));           // #0E1118
            SetBrush(app, "ThemeInputBorder", Color.FromRgb(36, 44, 64));       // #242C40
            SetBrush(app, "ThemeAccent", Color.FromRgb(229, 83, 55));           // #E55337 (Quietora Signature Terracotta Coral)
            SetBrush(app, "ThemeAccentHover", Color.FromRgb(242, 102, 75));     // #F2664B
            SetBrush(app, "ThemeAccentSubtle", Color.FromRgb(38, 20, 18));      // #261412 (Coral soft glow tint)
            SetBrush(app, "ThemeHoverBg", Color.FromRgb(27, 33, 48));           // #1B2130 (Elevated Hover)
            SetBrush(app, "ThemeLine", Color.FromRgb(32, 39, 55));              // #202737
            SetBrush(app, "ThemeMint", Color.FromRgb(16, 185, 129));            // #10B981 (Mint/Success)
            SetBrush(app, "ThemeMintBg", Color.FromRgb(12, 35, 26));            // #0C231A
            SetBrush(app, "ThemeMintBorder", Color.FromRgb(16, 185, 129));      // #10B981
            SetBrush(app, "ThemeNavActiveBg", Color.FromRgb(38, 20, 18));       // #261412 (Coral active nav)
            SetBrush(app, "ThemeNavActiveBorder", Color.FromRgb(229, 83, 55));   // #E55337
            SetBrush(app, "ThemeIndigo", Color.FromRgb(99, 102, 241));          // #6366F1
            SetBrush(app, "ThemeIndigoBg", Color.FromRgb(24, 25, 52));          // #181934
            SetBrush(app, "ThemeIndigoBorder", Color.FromRgb(67, 56, 202));     // #4338CA
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
            SetBrush(app, "ThemeAccent", Color.FromRgb(229, 83, 55));           // #E55337 (Quietora Signature Terracotta Coral)
            SetBrush(app, "ThemeAccentHover", Color.FromRgb(204, 67, 41));      // #CC4329
            SetBrush(app, "ThemeAccentSubtle", Color.FromRgb(253, 238, 235));   // #FDEEEB
            SetBrush(app, "ThemeHoverBg", Color.FromRgb(234, 229, 218));        // #EAE5DA
            SetBrush(app, "ThemeLine", Color.FromRgb(222, 216, 203));
            SetBrush(app, "ThemeMint", Color.FromRgb(36, 87, 68));              // #245744
            SetBrush(app, "ThemeMintBg", Color.FromRgb(230, 243, 235));         // #E6F3EB
            SetBrush(app, "ThemeMintBorder", Color.FromRgb(191, 225, 206));     // #BFE1CE
            SetBrush(app, "ThemeNavActiveBg", Color.FromRgb(253, 238, 235));
            SetBrush(app, "ThemeNavActiveBorder", Color.FromRgb(229, 83, 55));
            SetBrush(app, "ThemeIndigo", Color.FromRgb(79, 70, 229));           // #4F46E5
            SetBrush(app, "ThemeIndigoBg", Color.FromRgb(238, 242, 255));       // #EEF2FF
            SetBrush(app, "ThemeIndigoBorder", Color.FromRgb(199, 210, 254));   // #C7D2FE
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
