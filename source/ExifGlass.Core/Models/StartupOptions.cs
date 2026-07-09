namespace ExifGlass.Core.Models;

/// <summary>
/// Parsed command-line startup state, derived once at launch.
/// </summary>
/// <param name="Mode">Whether the app runs standalone or integrated with ImageGlass.</param>
/// <param name="InitialFilePath">First non-flag argument resolved to a file path, if any.</param>
/// <param name="ConfigOverrides">CLI <c>/Key=Value</c> overrides applied on top of the config file.</param>
public sealed record StartupOptions(
    AppMode Mode,
    string? InitialFilePath,
    IReadOnlyDictionary<string, string> ConfigOverrides)
{
    public static StartupOptions Empty { get; } =
        new(AppMode.Standalone, null, new Dictionary<string, string>());
}
