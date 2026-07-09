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
namespace ExifGlass.Helpers;

/// <summary>
/// Static application identity used by the About window and (later) update checks.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// The running assembly version as <c>Major.Minor.Build</c>.
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
    /// Microsoft Store product page (browser-safe form).
    /// </summary>
    public const string StoreUrl = "https://www.microsoft.com/store/productId/9MX8S9HZ57W8";

    private static string ResolveVersion()
    {
        var version = typeof(AppInfo).Assembly.GetName().Version;
        return version is null
            ? "1.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
