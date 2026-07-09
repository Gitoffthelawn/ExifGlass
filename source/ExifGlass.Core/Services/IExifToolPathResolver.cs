namespace ExifGlass.Core.Services;

/// <summary>
/// Resolves the ExifTool executable to run, following the order:
/// explicit Settings override → bundled copy next to the app → bare name on PATH.
/// </summary>
public interface IExifToolPathResolver
{
    /// <summary>
    /// Returns the best executable path/name to invoke.
    /// </summary>
    /// <param name="explicitPath">An optional user-configured path (from Settings).</param>
    string Resolve(string? explicitPath);

    /// <summary>
    /// Full path to the copy bundled next to the application, if one exists.
    /// </summary>
    string? BundledPath { get; }
}
