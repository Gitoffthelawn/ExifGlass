using ExifGlass.Core.Helpers;

namespace ExifGlass.Core.Services;

/// <summary>
/// Cross-platform ExifTool resolution: explicit override → bundled → PATH.
/// </summary>
public sealed class ExifToolPathResolver : IExifToolPathResolver
{
    public string? BundledPath
    {
        get
        {
            var candidate = Path.Combine(AppPaths.AppDir, PlatformInfo.ExifToolExecutableName);
            return File.Exists(candidate) ? candidate : null;
        }
    }

    public string Resolve(string? explicitPath)
    {
        // 1) Explicit Settings override, when it points at a real file.
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            return explicitPath;
        }

        // 2) Copy bundled next to the application.
        if (BundledPath is { } bundled)
        {
            return bundled;
        }

        // 3) Fall back to the bare name and let the OS resolve it on PATH.
        return PlatformInfo.ExifToolExecutableName;
    }
}
