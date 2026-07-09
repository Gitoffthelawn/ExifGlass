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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;
using ExifGlass.Core.Services;
using ExifGlass.Services;

namespace ExifGlass.ViewModels;

/// <summary>
/// Backs the settings dialog: theme, always-on-top, and the ExifTool path/arguments with a
/// live command preview. On OK it writes back to <see cref="ISettingsService"/>, applies the
/// theme, and persists; the owner reloads the current file afterwards.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IExifToolPathResolver _resolver;
    private readonly IThemeService _theme;
    private readonly IDialogService _dialogs;

    // Raised with true (OK) or false (Cancel) so the window can close with a result.
    public event Action<bool>? CloseRequested;

    [ObservableProperty] public partial int SelectedThemeIndex { get; set; }
    [ObservableProperty] public partial bool AlwaysOnTop { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommandPreview))]
    public partial string ExifToolPath { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommandPreview))]
    public partial string ExifToolArguments { get; set; } = "";

    /// <summary>
    /// Live preview of the command the current settings would produce.
    /// </summary>
    public string CommandPreview
    {
        get
        {
            var explicitPath = string.IsNullOrWhiteSpace(ExifToolPath) ? null : ExifToolPath.Trim();
            var exe = _resolver.Resolve(explicitPath);
            return ExifToolCommand.BuildPreview(exe, "C:\\path\\to\\photo.jpg", ExifToolArguments);
        }
    }

    public SettingsViewModel(
        ISettingsService settings,
        IExifToolPathResolver resolver,
        IThemeService theme,
        IDialogService dialogs)
    {
        _settings = settings;
        _resolver = resolver;
        _theme = theme;
        _dialogs = dialogs;

        var cfg = settings.Config;
        SelectedThemeIndex = (int)cfg.Theme;
        AlwaysOnTop = cfg.AlwaysOnTop;
        ExifToolPath = cfg.ExifToolPath;
        ExifToolArguments = cfg.ExifToolArguments;
    }

    [RelayCommand]
    private async Task BrowseExecutableAsync()
    {
        if (await _dialogs.PickExecutableAsync() is { } path)
        {
            ExifToolPath = path;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        var cfg = _settings.Config;
        cfg.Theme = (ThemeMode)SelectedThemeIndex;
        cfg.AlwaysOnTop = AlwaysOnTop;
        cfg.ExifToolPath = ExifToolPath.Trim();
        cfg.ExifToolArguments = ExifToolArguments.Trim();

        _theme.Apply(cfg.Theme);
        _settings.Save();

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
