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
using System.Reflection;

namespace ExifGlass.Helpers;

/// <summary>
/// Static application identity used by the About window and update checks.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// The running build version, used for display and for the update comparison.
    /// Prefers the assembly informational version, stripped of any build metadata.
    /// </summary>
    public static string Version { get; } = ResolveVersion();

    /// <summary>
    /// Project home / release page.
    /// </summary>
    public const string WebsiteUrl = "https://github.com/d2phap/ExifGlass";

    /// <summary>
    /// Release list, opened by "Check for update" until the update service lands.
    /// </summary>
    public const string ReleasesUrl = "https://github.com/d2phap/ExifGlass/releases";

    /// <summary>
    /// Microsoft Store ID.
    /// </summary>
    public const string MsStoreId = "9MX8S9HZ57W8";

    private static string ResolveVersion()
    {
        var assembly = typeof(AppInfo).Assembly;

        // Both sources below read *embedded managed assembly metadata*, so they work identically
        // on Windows, macOS and Linux — and inside single-file / NativeAOT bundles. We deliberately
        // avoid Assembly.Location + FileVersionInfo: Location is empty in single-file/AOT builds,
        // and Win32 file-version resources don't exist in a Linux ELF or macOS Mach-O host.
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Defensive trim of any "+<commit>" suffix (disabled in the csproj, but harmless).
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        // Fallback: the assembly version is always embedded in the image metadata.
        var version = assembly.GetName().Version;
        return version is null
            ? "1.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
