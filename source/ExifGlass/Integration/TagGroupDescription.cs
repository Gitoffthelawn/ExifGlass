using System.Globalization;
using Avalonia.Collections;
using ExifGlass.Core.Models;

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
