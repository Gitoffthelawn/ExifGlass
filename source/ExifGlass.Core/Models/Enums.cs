namespace ExifGlass.Core.Models;

/// <summary>
/// Color theme mode for the application window.
/// </summary>
public enum ThemeMode
{
    /// <summary>Follow the operating-system theme.</summary>
    Default = 0,

    /// <summary>Force the dark variant.</summary>
    Dark = 1,

    /// <summary>Force the light variant.</summary>
    Light = 2,
}

/// <summary>
/// How the application was launched.
/// </summary>
public enum AppMode
{
    /// <summary>Launched directly by the user (CLI arg, drag-drop, or file picker).</summary>
    Standalone = 0,

    /// <summary>Launched by ImageGlass as an integrated tool (pipe connection).</summary>
    ImageGlass = 1,
}

/// <summary>
/// Supported metadata export formats.
/// </summary>
public enum ExportFileType
{
    Text = 0,
    Csv = 1,
    Json = 2,
}
