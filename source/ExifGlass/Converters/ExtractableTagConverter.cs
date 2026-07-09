using System.Globalization;
using Avalonia.Data.Converters;
using ExifGlass.Core.Models;

namespace ExifGlass.Converters;

/// <summary>
/// Converts a bound <see cref="ExifTagItem"/> (or <c>null</c>) to a bool indicating whether
/// its value is extractable binary data — used to show/hide the "Extract data" menu item.
/// </summary>
public sealed class ExtractableTagConverter : IValueConverter
{
    public static readonly ExtractableTagConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ExifTagItem item && item.CanExtractBinary;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
