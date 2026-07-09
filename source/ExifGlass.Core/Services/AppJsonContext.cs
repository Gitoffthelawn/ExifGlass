using System.Text.Json.Serialization;
using ExifGlass.Core.Models;

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
public partial class AppJsonContext : JsonSerializerContext
{
}
