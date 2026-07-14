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
namespace ExifGlass.Core.Models;

/// <summary>
/// Parsed command-line startup state, derived once at launch.
/// </summary>
/// <param name="Mode">Whether the app runs standalone or integrated with ImageGlass.</param>
/// <param name="InitialFilePath">First non-flag argument resolved to a file path, if any.</param>
/// <param name="ConfigOverrides">CLI <c>-p:Key=Value</c> overrides applied on top of the config file.</param>
public sealed record StartupOptions(
    AppMode Mode,
    string? InitialFilePath,
    IReadOnlyDictionary<string, string> ConfigOverrides)
{
    public static StartupOptions Empty { get; } =
        new(AppMode.Standalone, null, new Dictionary<string, string>());
}
