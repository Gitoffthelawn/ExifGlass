/*
ExifGlass - EXIF Metadata Viewing Tool
Copyright (C) 2023 - 2026 DUONG DIEU PHAP
Project homepage: https://github.com/d2phap/ExifGlass

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
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
