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
