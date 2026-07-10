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
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace ExifGlass.Views;

/// <summary>
/// Base window with system-integrated translucency. It requests Mica, then Acrylic, then a
/// solid fallback. When the platform can actually honor a translucent level (Windows 11) the
/// background is transparent so the effect shows through; otherwise (Linux, older Windows) a
/// solid, theme-aware background is painted — so no per-OS <c>#if</c> is needed.
/// </summary>
public class StyledWindow : Window
{
    public StyledWindow()
    {
        TransparencyLevelHint =
        [
            WindowTransparencyLevel.Mica,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.None,
        ];

        // Paint the correct theme-aware background up front, before the first frame is
        // composited. Deferring this to OnOpened/ActualThemeVariantChanged let the window
        // render one light frame and then flip to dark once its variant resolved — a
        // jarring light->dark "flash" when launching under a dark theme.
        UpdateBackground();

        ActualThemeVariantChanged += (_, _) => UpdateBackground();
    }


    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UpdateBackground();
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualTransparencyLevelProperty
            || change.Property == IsActiveProperty)
        {
            UpdateBackground();
        }
    }


    private void UpdateBackground()
    {
        if (ActualTransparencyLevel != WindowTransparencyLevel.None
            && ActualTransparencyLevel != WindowTransparencyLevel.Transparent
            && IsActive)
        {
            // A translucent level is active — let it show through.
            Background = Brushes.Transparent;
            return;
        }

        // Solid, theme-aware fallback matching the Fluent light/dark surface colors.
        var dark = ResolvedThemeVariant() == ThemeVariant.Dark;
        Background = new SolidColorBrush(dark ? Color.FromRgb(0x20, 0x20, 0x20) : Color.FromRgb(0xF3, 0xF3, 0xF3));
    }


    /// <summary>
    /// Resolves the theme variant this window should paint for. Prefers the application's
    /// variant, which is applied before the window is created and so is already resolved
    /// during construction — the window's own <see cref="ActualThemeVariant"/> still reports
    /// the default (Light) until it is attached, which would mispaint the first frame.
    /// </summary>
    private ThemeVariant ResolvedThemeVariant()
        => Application.Current?.ActualThemeVariant ?? ActualThemeVariant;
}
