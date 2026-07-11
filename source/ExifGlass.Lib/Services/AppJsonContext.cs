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
using ExifGlass.Core.Models;
using System.Text.Json.Serialization;

namespace ExifGlass.Core.Services;

/// <summary>
/// Source-generated <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> for
/// all persisted/serialized types. Using the generated metadata keeps serialization
/// reflection-free and AOT-safe — never call the reflection-based
/// <c>JsonSerializer</c> overloads.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    UseStringEnumConverter = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(List<ExifTagItem>))]
[JsonSerializable(typeof(ExifTagItem))]
[JsonSerializable(typeof(UpdateInfo))]
public partial class AppJsonContext : JsonSerializerContext
{
}
