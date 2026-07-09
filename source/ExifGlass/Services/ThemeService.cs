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

        app.RequestedThemeVariant = mode switch
        {
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }
}
