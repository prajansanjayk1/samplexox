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
            SetBrush(app, "ThemeWindowBg", Color.FromRgb(15, 23, 42));       // #0F172A
            SetBrush(app, "ThemeSidebarBg", Color.FromRgb(17, 24, 39));      // #111827
            SetBrush(app, "ThemeCardBg", Color.FromRgb(30, 41, 59));         // #1E293B
            SetBrush(app, "ThemeCardBorder", Color.FromRgb(51, 65, 85));     // #334155
            SetBrush(app, "ThemeTextPrimary", Color.FromRgb(248, 250, 252)); // #F8FAFC
            SetBrush(app, "ThemeTextSecondary", Color.FromRgb(148, 163, 184));// #94A3B8
            SetBrush(app, "ThemeInputBg", Color.FromRgb(15, 23, 42));        // #0F172A
            SetBrush(app, "ThemeInputBorder", Color.FromRgb(71, 85, 105));   // #475569
            SetBrush(app, "ThemeAccent", Color.FromRgb(99, 102, 241));       // #6366F1
            SetBrush(app, "ThemeAccentSubtle", Color.FromRgb(49, 46, 129));  // #312E81
            SetBrush(app, "ThemeHoverBg", Color.FromRgb(51, 65, 85));        // #334155
        }
        else
        {
            SetBrush(app, "ThemeWindowBg", Color.FromRgb(248, 250, 252));    // #F8FAFC
            SetBrush(app, "ThemeSidebarBg", Color.FromRgb(17, 29, 52));      // #111D34 (Night brand)
            SetBrush(app, "ThemeCardBg", Color.FromRgb(255, 255, 255));      // #FFFFFF
            SetBrush(app, "ThemeCardBorder", Color.FromRgb(226, 232, 240));  // #E2E8F0
            SetBrush(app, "ThemeTextPrimary", Color.FromRgb(15, 23, 42));    // #0F172A
            SetBrush(app, "ThemeTextSecondary", Color.FromRgb(100, 116, 139));// #64748B
            SetBrush(app, "ThemeInputBg", Color.FromRgb(255, 255, 255));     // #FFFFFF
            SetBrush(app, "ThemeInputBorder", Color.FromRgb(215, 222, 234)); // #D7DEEA
            SetBrush(app, "ThemeAccent", Color.FromRgb(79, 70, 229));        // #4F46E5
            SetBrush(app, "ThemeAccentSubtle", Color.FromRgb(238, 242, 255));// #EEF2FF
            SetBrush(app, "ThemeHoverBg", Color.FromRgb(241, 245, 249));     // #F1F5F9
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
