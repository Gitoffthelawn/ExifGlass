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
namespace ExifGlass.Core.Helpers;

/// <summary>
/// Resolves OS-correct locations for configuration and working files.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// Product name used for the per-user config directory.
    /// </summary>
    public const string AppName = "ExifGlass";

    private const string ConfigFileName = "exifglass.config.json";

    /// <summary>
    /// Per-user configuration directory:
    /// <c>%LOCALAPPDATA%\ExifGlass</c> on Windows, XDG / Application Support elsewhere.
    /// </summary>
    public static string ConfigDir
    {
        get
        {
            // SpecialFolder.LocalApplicationData maps to the right per-user location on
            // every OS: %LOCALAPPDATA% (Windows), ~/.config (Linux/XDG),
            // ~/Library/Application Support (macOS).
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config");
            }
            return Path.Combine(baseDir, AppName);
        }
    }

    /// <summary>
    /// Full path to the JSON config file.
    /// </summary>
    public static string ConfigFilePath => Path.Combine(ConfigDir, ConfigFileName);

    /// <summary>
    /// Directory for transient files (ANSI path copies, binary-extract staging).
    /// </summary>
    public static string TempDir => Path.Combine(ConfigDir, "Temp");

    /// <summary>
    /// Directory the running executable lives in (where bundled ExifTool sits).
    /// </summary>
    public static string AppDir => AppContext.BaseDirectory;

    /// <summary>
    /// Ensures the config directory exists; safe to call repeatedly.
    /// </summary>
    public static void EnsureConfigDir() => Directory.CreateDirectory(ConfigDir);

    /// <summary>
    /// Ensures the temp directory exists; safe to call repeatedly.
    /// </summary>
    public static void EnsureTempDir() => Directory.CreateDirectory(TempDir);
}
