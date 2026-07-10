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
    /// <summary>
    /// 1-based position of the tag within the read result.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Hexadecimal tag id (from the <c>-H</c> flag).
    /// </summary>
    public string TagId { get; init; } = "";

    /// <summary>
    /// Family-0 group name (EXIF, File, XMP, ...).
    /// </summary>
    public string TagGroup { get; init; } = "";

    /// <summary>
    /// Human-readable tag name.
    /// </summary>
    public string TagName { get; init; } = "";

    /// <summary>
    /// Formatted tag value.
    /// </summary>
    public string TagValue { get; init; } = "";

    /// <summary>
    /// <c>true</c> when the value is binary data that ExifTool can extract with the <c>-b</c> option.
    /// </summary>
    /// <remarks>Derived, UI-only; excluded from the JSON export which carries just the metadata fields.</remarks>
    [JsonIgnore]
    public bool CanExtractBinary => TagValue.Contains(", use -b option to extract", StringComparison.Ordinal);
}
