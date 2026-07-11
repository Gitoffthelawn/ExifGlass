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
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using ExifGlass.Core.Models;

namespace ExifGlass.Services;

/// <summary>
/// Applies the configured <see cref="ThemeMode"/> to the running application.
/// </summary>
public interface IThemeService
{
    void Apply(ThemeMode mode);
}

/// <summary>
/// Maps <see cref="ThemeMode"/> onto Avalonia's <see cref="ThemeVariant"/>. <c>Default</c>
/// follows the operating-system theme.
/// </summary>
public sealed class ThemeService : IThemeService
{
    public void Apply(ThemeMode mode)
    {
        if (Application.Current is not { } app) return;

        var settings = app.PlatformSettings;

        // Follow the OS at runtime only while in Default mode; drop the subscription otherwise.
        // Unsubscribe first so repeated Apply calls (e.g. from Settings) don't stack handlers.
        if (settings is not null)
        {
            settings.ColorValuesChanged -= OnColorValuesChanged;
            if (mode == ThemeMode.Default)
            {
                settings.ColorValuesChanged += OnColorValuesChanged;
            }
        }

        app.RequestedThemeVariant = mode switch
        {
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            // Resolve the OS preference to a concrete variant up front. Leaving this as
            // ThemeVariant.Default lets Avalonia resolve the platform variant a frame late
            // on some backends (X11 queries the desktop portal asynchronously): the first
            // frame paints Light and then flips to Dark, a jarring startup "flash". Pinning
            // the concrete variant paints it correctly from the first frame, and
            // ColorValuesChanged keeps it in sync afterwards.
            _ => ResolveSystemVariant(settings),
        };
    }

    /// <summary>
    /// Determines the concrete variant for <see cref="ThemeMode.Default"/>. A platform-provided
    /// synchronous probe wins where it is available (the Linux head, whose Avalonia platform
    /// settings resolve too late for the first frame); otherwise Avalonia's platform settings
    /// decide, which are synchronous and correct on Windows/macOS.
    /// </summary>
    private static ThemeVariant ResolveSystemVariant(IPlatformSettings? settings)
    {
        if (App.SystemDarkModeProbe?.Invoke() is bool dark)
        {
            return dark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        return ToVariant(settings?.GetColorValues().ThemeVariant);
    }

    /// <summary>
    /// Tracks OS light/dark changes while in <see cref="ThemeMode.Default"/> so the app keeps
    /// following the system, matching the behaviour of an unpinned <c>Default</c> variant.
    /// </summary>
    private void OnColorValuesChanged(object? sender, PlatformColorValues e)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = ToVariant(e.ThemeVariant);
        }
    }

    /// <summary>
    /// Maps a platform light/dark preference onto an Avalonia <see cref="ThemeVariant"/>,
    /// defaulting to light when the platform preference is unavailable.
    /// </summary>
    private static ThemeVariant ToVariant(PlatformThemeVariant? platform)
        => platform == PlatformThemeVariant.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
}
