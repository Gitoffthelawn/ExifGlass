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
using CommunityToolkit.Mvvm.Input;
using ExifGlass.Core.Models;
using ExifGlass.Helpers;
using ExifGlass.Services;
using System.Globalization;

namespace ExifGlass.ViewModels;

/// <summary>
/// Backs the notify-only update window: shows the available version and notes, and opens the
/// release page (no self-replacing binary).
/// </summary>
public sealed partial class UpdateViewModel(UpdateInfo info, string currentVersion, IDialogService dialogs) : ViewModelBase
{
    private readonly IDialogService _dialogs = dialogs;
    private readonly string _downloadUrl = string.IsNullOrWhiteSpace(info.DownloadUrl) ? AppInfo.ReleasesUrl : info.DownloadUrl;

    public event Action? CloseRequested;

    public string CurrentVersion { get; } = currentVersion;
    public string NewVersion { get; } = info.Version;
    public string Title { get; } = string.IsNullOrWhiteSpace(info.Title) ? $"ExifGlass {info.Version}" : info.Title;
    public string Notes { get; } = info.Description;
    public string PublishedDate { get; } = FormatDate(info.PublishedDate);
    public bool HasChangelog { get; } = !string.IsNullOrWhiteSpace(info.ChangelogUrl);

    private readonly string _changelogUrl = info.ChangelogUrl;


    [RelayCommand]
    private Task Download() => _dialogs.OpenUrlAsync(_downloadUrl);

    [RelayCommand]
    private Task OpenChangelog() => _dialogs.OpenUrlAsync(_changelogUrl);

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private static string FormatDate(string isoDate)
    {
        if (DateTimeOffset.TryParse(isoDate, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var date))
        {
            // InvariantGlobalization is on; a fixed, unambiguous form avoids culture surprises.
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        return isoDate;
    }
}
