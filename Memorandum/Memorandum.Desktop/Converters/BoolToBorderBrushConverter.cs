using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Memorandum.Desktop.Converters;

/// <summary>
/// true — акцентная кисть (подсветка), false — обычная рамка графа.
/// </summary>
public class BoolToBorderBrushConverter : IValueConverter
{
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.Parse("#7c3aed"));
    private static readonly IBrush NormalBrush = new SolidColorBrush(Color.Parse("#4a4a6a"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? HighlightBrush : NormalBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
