namespace ExifGlass.Core.Models;

/// <summary>
/// Result of validating the resolved ExifTool executable.
/// </summary>
/// <param name="IsAvailable"><c>true</c> when ExifTool was found and responded.</param>
/// <param name="ResolvedPath">The path that was probed.</param>
/// <param name="Version">The reported ExifTool version, when available.</param>
/// <param name="ErrorMessage">A friendly message when <paramref name="IsAvailable"/> is <c>false</c>.</param>
public sealed record ExifToolStatus(
    bool IsAvailable,
    string ResolvedPath,
    string? Version,
    string? ErrorMessage);
