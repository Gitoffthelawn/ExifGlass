using ExifGlass.Core.Models;

namespace ExifGlass.Core.Services;

/// <summary>
/// Holds the live <see cref="AppConfig"/> and persists it via source-generated JSON.
/// Registered as a singleton in the composition root.
/// </summary>
public interface ISettingsService
{
    /// <summary>The current, mutable configuration.</summary>
    AppConfig Config { get; }

    /// <summary>
    /// Loads configuration from disk, tolerating a missing or corrupt file
    /// (falls back to defaults).
    /// </summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the current configuration to disk.</summary>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies CLI <c>/Key=Value</c> overrides on top of the loaded configuration
    /// using an explicit, reflection-free mapping.
    /// </summary>
    void ApplyOverrides(IReadOnlyDictionary<string, string> overrides);
}
