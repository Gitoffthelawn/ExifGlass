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
using ExifGlass.Core.Models;
using System.Text.Json;

namespace ExifGlass.Core.Services;

/// <summary>
/// File-backed settings store using source-generated JSON. The layered load is
/// defaults → file → CLI overrides, all reflection-free.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    public AppConfig Config { get; private set; } = new();

    public void Load()
    {
        try
        {
            var path = AppPaths.ConfigFilePath;
            if (!File.Exists(path))
            {
                Config = new AppConfig();
                return;
            }

            var json = File.ReadAllText(path);
            Config = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig) ?? new AppConfig();
        }
        catch
        {
            // Missing or corrupt file — start from defaults rather than failing launch.
            Config = new AppConfig();
        }
    }

    public void Save()
    {
        AppPaths.EnsureConfigDir();
        var path = AppPaths.ConfigFilePath;

        // Write to a temp file then move, so a crash can't leave a truncated config.
        var tmp = path + ".tmp";
        var json = JsonSerializer.Serialize(Config, AppJsonContext.Default.AppConfig);
        File.WriteAllText(tmp, json);
        File.Move(tmp, path, overwrite: true);
    }

    public void ApplyOverrides(IReadOnlyDictionary<string, string> overrides)
    {
        // Keys mirror the config property names via nameof (rename-safe, no raw strings) and
        // match case-insensitively. Window keys are composed from the property path, e.g.
        // nameof(Window) + nameof(Width) => "WindowWidth".
        foreach (var (key, value) in overrides)
        {
            if (Matches(key, nameof(AppConfig.Theme)))
            {
                if (TryParseTheme(value, out var theme)) Config.Theme = theme;
            }
            else if (Matches(key, nameof(AppConfig.AlwaysOnTop)))
            {
                if (TryParseBool(value, out var alwaysOnTop)) Config.AlwaysOnTop = alwaysOnTop;
            }
            else if (Matches(key, nameof(AppConfig.ExifToolPath)))
            {
                Config.ExifToolPath = value;
            }
            else if (Matches(key, nameof(AppConfig.ExifToolArguments)))
            {
                Config.ExifToolArguments = value;
            }
            else if (Matches(key, nameof(AppConfig.ShowCommandPreview)))
            {
                if (TryParseBool(value, out var showPreview)) Config.ShowCommandPreview = showPreview;
            }
            else if (Matches(key, nameof(AppConfig.ShowIndex)))
            {
                if (TryParseBool(value, out var showIndex)) Config.ShowIndex = showIndex;
            }
            else if (Matches(key, nameof(AppConfig.ShowTagId)))
            {
                if (TryParseBool(value, out var showTagId)) Config.ShowTagId = showTagId;
            }
            else if (Matches(key, nameof(AppConfig.ShowTagName)))
            {
                if (TryParseBool(value, out var showTagName)) Config.ShowTagName = showTagName;
            }
            else if (Matches(key, nameof(AppConfig.ShowValue)))
            {
                if (TryParseBool(value, out var showValue)) Config.ShowValue = showValue;
            }
            else if (Matches(key, nameof(AppConfig.CheckForUpdates)))
            {
                if (TryParseBool(value, out var checkForUpdates)) Config.CheckForUpdates = checkForUpdates;
            }
            else if (Matches(key, nameof(AppConfig.Window) + nameof(WindowBounds.X)))
            {
                if (int.TryParse(value, out var windowX)) Config.Window.X = windowX;
            }
            else if (Matches(key, nameof(AppConfig.Window) + nameof(WindowBounds.Y)))
            {
                if (int.TryParse(value, out var windowY)) Config.Window.Y = windowY;
            }
            else if (Matches(key, nameof(AppConfig.Window) + nameof(WindowBounds.Width)))
            {
                if (int.TryParse(value, out var windowWidth)) Config.Window.Width = windowWidth;
            }
            else if (Matches(key, nameof(AppConfig.Window) + nameof(WindowBounds.Height)))
            {
                if (int.TryParse(value, out var windowHeight)) Config.Window.Height = windowHeight;
            }
            // Unknown keys are ignored.
        }
    }

    private static bool Matches(string key, string name)
        => string.Equals(key, name, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "true" or "1" or "yes" or "on":
                result = true;
                return true;
            case "false" or "0" or "no" or "off":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    private static bool TryParseTheme(string value, out ThemeMode result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "default" or "0":
                result = ThemeMode.Default;
                return true;
            case "dark" or "1":
                result = ThemeMode.Dark;
                return true;
            case "light" or "2":
                result = ThemeMode.Light;
                return true;
            default:
                result = ThemeMode.Default;
                return false;
        }
    }
}
