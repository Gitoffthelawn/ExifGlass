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
using Avalonia.Collections;
using ExifGlass.Core.Models;
using System.Globalization;

namespace ExifGlass.Integration;

/// <summary>
/// Reflection-free grouping for the metadata grid. The path-based
/// <c>DataGridPathGroupDescription("TagGroup")</c> resolves the property by reflection,
/// which is trim-fragile; this description casts directly to <see cref="ExifTagItem"/>
/// and reads <see cref="ExifTagItem.TagGroup"/>.
/// </summary>
public sealed class TagGroupDescription : DataGridGroupDescription
{
    public override string PropertyName => nameof(ExifTagItem.TagGroup);

    public override object GroupKeyFromItem(object item, int level, CultureInfo culture)
        => ((ExifTagItem)item).TagGroup;
}
