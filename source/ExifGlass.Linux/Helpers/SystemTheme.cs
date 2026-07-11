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
using System.Diagnostics;

namespace ExifGlass.Helpers;

/// <summary>
/// Reads the Linux desktop's light/dark preference synchronously, so the first window frame
/// can be painted in the correct variant.
/// </summary>
/// <remarks>
/// This lives in the Linux head because it is Linux-specific: the X11 Avalonia backend resolves
/// the desktop portal's color-scheme asynchronously, so the value isn't available when the first
/// frame is painted — leaving <c>ThemeVariant.Default</c> to resolve then flashes light→dark.
/// The library consumes this through <c>App.SystemDarkModeProbe</c>. Windows and macOS report the
/// preference synchronously through Avalonia's platform settings and so need no probe.
/// </remarks>
public static class SystemTheme
{
    /// <summary>
    /// Returns the OS dark-mode preference (<c>true</c> = dark, <c>false</c> = light), or
    /// <c>null</c> when it can't be determined (the library then falls back to Avalonia's
    /// platform settings).
    /// </summary>
    public static bool? OsPrefersDark()
    {
        // GNOME-family desktops (including Zorin and Ubuntu) expose the preference through
        // gsettings: "prefer-dark" is an explicit dark preference; a dark GTK theme name is a
        // weaker secondary signal used only when no explicit color-scheme is set.
        return ColorSchemeIsDark() ?? GtkThemeIsDark();
    }

    private static bool? ColorSchemeIsDark()
    {
        var value = ReadGSetting("org.gnome.desktop.interface", "color-scheme");
        if (value is null) return null;
        if (value.Contains("dark", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Contains("light", StringComparison.OrdinalIgnoreCase)) return false;
        return null;   // 'default' / no explicit preference
    }

    private static bool? GtkThemeIsDark()
    {
        var value = ReadGSetting("org.gnome.desktop.interface", "gtk-theme");
        if (value is null) return null;
        return value.Contains("dark", StringComparison.OrdinalIgnoreCase) ? true : null;
    }

    /// <summary>
    /// Runs <c>gsettings get &lt;schema&gt; &lt;key&gt;</c> and returns its trimmed output, or
    /// <c>null</c> if gsettings is missing, errors, or does not finish promptly. Best-effort:
    /// this sits on the startup path, so it never throws and never blocks for long.
    /// </summary>
    private static string? ReadGSetting(string schema, string key)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("gsettings", $"get {schema} {key}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (proc is null) return null;

            var output = proc.StandardOutput.ReadToEnd();
            if (!proc.WaitForExit(500))
            {
                try { proc.Kill(); } catch { /* already gone */ }
                return null;
            }

            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
    }
}
