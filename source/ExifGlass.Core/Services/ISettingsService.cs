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
    /// (falls back to defaults). Synchronous: the config is tiny and both callers
    /// (startup and close) need it done before proceeding.
    /// </summary>
    void Load();

    /// <summary>
    /// Writes the current configuration to disk (atomic temp-file swap). Synchronous
    /// so it is safe to call while blocking the UI thread on window close.
    /// </summary>
    void Save();

    /// <summary>
    /// Applies CLI <c>/Key=Value</c> overrides on top of the loaded configuration
    /// using an explicit, reflection-free mapping.
    /// </summary>
    void ApplyOverrides(IReadOnlyDictionary<string, string> overrides);
}
