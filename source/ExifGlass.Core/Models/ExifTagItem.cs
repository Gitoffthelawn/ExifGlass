using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ExifGlass.Core.Models;

/// <summary>
/// A single row of metadata read from ExifTool.
/// </summary>
/// <remarks>
/// Annotated so the trimmer preserves its public properties and parameterless
/// constructor: the data grid enumerates them via <c>Type.GetProperties()</c>.
/// </remarks>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties
                            | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public sealed record ExifTagItem
{
    /// <summary>1-based position of the tag within the read result.</summary>
    public int Index { get; init; }

    /// <summary>Hexadecimal tag id (from the <c>-H</c> flag).</summary>
    public string TagId { get; init; } = "";

    /// <summary>Family-0 group name (EXIF, File, XMP, ...).</summary>
    public string TagGroup { get; init; } = "";

    /// <summary>Human-readable tag name.</summary>
    public string TagName { get; init; } = "";

    /// <summary>Formatted tag value.</summary>
    public string TagValue { get; init; } = "";

    /// <summary>
    /// <c>true</c> when the value is binary data that ExifTool can extract with the <c>-b</c> option.
    /// </summary>
    /// <remarks>Derived, UI-only; excluded from the JSON export which carries just the metadata fields.</remarks>
    [JsonIgnore]
    public bool CanExtractBinary
        => TagValue.Contains(", use -b option to extract", StringComparison.Ordinal);
}
