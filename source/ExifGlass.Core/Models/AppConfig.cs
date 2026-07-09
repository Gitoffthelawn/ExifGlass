namespace ExifGlass.Core.Models;

/// <summary>
/// User-facing application settings, persisted as JSON.
/// </summary>
/// <remarks>
/// A plain mutable class so the source-generated JSON serializer can round-trip it
/// without reflection. Defaults here define the baseline layer of the layered load.
/// </remarks>
public sealed class AppConfig
{
    /// <summary>Color theme mode.</summary>
    public ThemeMode Theme { get; set; } = ThemeMode.Default;

    /// <summary>Whether the main window stays above other windows.</summary>
    public bool AlwaysOnTop { get; set; }

    /// <summary>Optional explicit path to the ExifTool executable; empty uses the bundled/PATH copy.</summary>
    public string ExifToolPath { get; set; } = "";

    /// <summary>Extra command-line arguments appended to every ExifTool invocation.</summary>
    public string ExifToolArguments { get; set; } = "";

    /// <summary>Whether the footer shows the live ExifTool command preview.</summary>
    public bool ShowCommandPreview { get; set; } = true;

    /// <summary>Column visibility flags.</summary>
    public bool ShowIndex { get; set; } = true;
    public bool ShowTagId { get; set; } = true;
    public bool ShowTagName { get; set; } = true;
    public bool ShowValue { get; set; } = true;

    /// <summary>Persisted main-window bounds.</summary>
    public WindowBounds Window { get; set; } = new();

    /// <summary>Whether to check for updates on startup.</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>UTC timestamp of the last update check (ISO-8601), or empty if never.</summary>
    public string LastUpdateCheck { get; set; } = "";

    /// <summary>Returns a shallow copy so callers can mutate without touching the live instance.</summary>
    public AppConfig Clone() => new()
    {
        Theme = Theme,
        AlwaysOnTop = AlwaysOnTop,
        ExifToolPath = ExifToolPath,
        ExifToolArguments = ExifToolArguments,
        ShowCommandPreview = ShowCommandPreview,
        ShowIndex = ShowIndex,
        ShowTagId = ShowTagId,
        ShowTagName = ShowTagName,
        ShowValue = ShowValue,
        Window = new WindowBounds
        {
            X = Window.X,
            Y = Window.Y,
            Width = Window.Width,
            Height = Window.Height,
            Maximized = Window.Maximized,
        },
        CheckForUpdates = CheckForUpdates,
        LastUpdateCheck = LastUpdateCheck,
    };
}
