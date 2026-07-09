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
using System.Text.Json;
using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;

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
        foreach (var (key, value) in overrides)
        {
            switch (key.ToLowerInvariant())
            {
                case "theme":
                    if (TryParseTheme(value, out var theme)) Config.Theme = theme;
                    break;
                case "alwaysontop":
                    if (TryParseBool(value, out var aot)) Config.AlwaysOnTop = aot;
                    break;
                case "exiftoolpath":
                    Config.ExifToolPath = value;
                    break;
                case "exiftoolarguments":
                    Config.ExifToolArguments = value;
                    break;
                case "showcommandpreview":
                    if (TryParseBool(value, out var scp)) Config.ShowCommandPreview = scp;
                    break;
                case "showindex":
                    if (TryParseBool(value, out var si)) Config.ShowIndex = si;
                    break;
                case "showtagid":
                    if (TryParseBool(value, out var sti)) Config.ShowTagId = sti;
                    break;
                case "showtagname":
                    if (TryParseBool(value, out var stn)) Config.ShowTagName = stn;
                    break;
                case "showvalue":
                    if (TryParseBool(value, out var sv)) Config.ShowValue = sv;
                    break;
                case "checkforupdates":
                    if (TryParseBool(value, out var cfu)) Config.CheckForUpdates = cfu;
                    break;
                case "windowx":
                    if (int.TryParse(value, out var wx)) Config.Window.X = wx;
                    break;
                case "windowy":
                    if (int.TryParse(value, out var wy)) Config.Window.Y = wy;
                    break;
                case "windowwidth":
                    if (int.TryParse(value, out var ww)) Config.Window.Width = ww;
                    break;
                case "windowheight":
                    if (int.TryParse(value, out var wh)) Config.Window.Height = wh;
                    break;
                // Unknown keys are ignored.
            }
        }
    }

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
