using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Memorandum.Desktop.Themes;

namespace Memorandum.Desktop.Converters;

public class TagNameToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name)
            return GetDefaultBrush();
        var key = PaletteConstants.DefaultTagNameToKey.TryGetValue(name.Trim(), out var k)
            ? k
            : PaletteConstants.TagPillResourceKeys[Math.Abs(name.GetHashCode()) % PaletteConstants.TagPillResourceKeys.Length];
        return TryGetAppResource(key, out var brush) ? brush : GetDefaultBrush();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();

    private static IBrush GetDefaultBrush()
    {
        var key = PaletteConstants.TagPillResourceKeys.Length > 0 ? PaletteConstants.TagPillResourceKeys[0] : null;
        if (key != null && TryGetAppResource(key, out var brush))
            return brush;
        return new SolidColorBrush(Color.Parse(PaletteConstants.DefaultTagPillFallbackHex));
    }

    private static bool TryGetAppResource(object key, out IBrush? brush)
    {
        brush = null;
        if (Avalonia.Application.Current?.Resources?.TryGetResource(key, null, out var value) == true && value is IBrush b)
        {
            brush = b;
            return true;
        }
        return false;
    }
}
