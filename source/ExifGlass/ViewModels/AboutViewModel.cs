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
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Input;
using ExifGlass.Helpers;
using ExifGlass.Services;

namespace ExifGlass.ViewModels;

/// <summary>
/// Backs the About dialog: version, links, and a notify-only update check.
/// </summary>
public sealed partial class AboutViewModel(IDialogService dialogs) : ViewModelBase
{
    public event Action? CloseRequested;

    // Instance (not static) members: the About dialog binds to them via compiled bindings,
    // which resolve against the DataType instance.
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Bound in XAML via compiled bindings; must remain instance members.")]
    public string Version => AppInfo.Version;

    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Bound in XAML via compiled bindings; must remain instance members.")]
    public string WebsiteUrl => AppInfo.WebsiteUrl;

    [RelayCommand]
    private Task OpenWebsiteAsync() => dialogs.OpenUrlAsync(AppInfo.WebsiteUrl);

    [RelayCommand]
    private Task OpenStoreAsync() => dialogs.OpenUrlAsync(AppInfo.StoreUrl);

    [RelayCommand]
    private Task CheckForUpdateAsync() => dialogs.OpenUrlAsync(AppInfo.ReleasesUrl);

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
