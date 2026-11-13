using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Converters;

public sealed class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            if (parameter is string p && string.Equals(p, "Inverse", StringComparison.OrdinalIgnoreCase))
            {
                b = !b;
            }
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public sealed class PriorityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Urgent => new SolidColorBrush(Color.FromRgb(239, 68, 68)),   // Red
                TaskPriority.High => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // Orange
                TaskPriority.Medium => new SolidColorBrush(Color.FromRgb(99, 102, 241)), // Indigo
                _ => new SolidColorBrush(Color.FromRgb(16, 185, 129))                    // Green
            };
        }
        return new SolidColorBrush(Color.FromRgb(100, 116, 139));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public sealed class EqualityToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBg = new(Color.FromRgb(42, 24, 21)); // Coral subtle tint
    private static readonly SolidColorBrush InactiveBg = Brushes.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return InactiveBg;
        bool match = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return match ? ActiveBg : InactiveBg;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public sealed class EqualityToForegroundConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveFg = new(Color.FromRgb(248, 250, 252));
    private static readonly SolidColorBrush InactiveFg = new(Color.FromRgb(159, 162, 170));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return InactiveFg;
        bool match = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return match ? ActiveFg : InactiveFg;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public sealed class EqualityToBorderBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ActiveBorder = new(Color.FromRgb(229, 83, 55)); // #E55337
    private static readonly SolidColorBrush InactiveBorder = Brushes.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return InactiveBorder;
        bool match = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return match ? ActiveBorder : InactiveBorder;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public sealed class EqualityToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return FontWeights.Normal;
        bool match = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return match ? FontWeights.Bold : FontWeights.Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
