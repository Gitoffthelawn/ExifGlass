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
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ExifGlass.Core.Models;
using ExifGlass.Core.Services;
using ExifGlass.ViewModels;
using ExifGlass.Views;

namespace ExifGlass.Services;

/// <summary>
/// UI-side services that need a live window: file/save pickers, clipboard, the OS launcher,
/// and hosting the modal dialogs. Everything is reached through <see cref="TopLevel"/> so no
/// legacy dialog APIs are used (Avalonia 12 removed <c>OpenFileDialog</c>).
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// The window that owns pickers and dialogs; set once the main window exists.
    /// </summary>
    Window? Owner { get; set; }

    Task<string?> PickImageFileAsync();
    Task<string?> PickExecutableAsync();
    Task<string?> PickBinaryDestinationAsync(string suggestedFileName);

    /// <summary>
    /// Shows a save picker for the format, then writes the rows. Returns <c>true</c> if saved.
    /// </summary>
    Task<bool> ExportAsync(ExportFileType type, IReadOnlyList<ExifTagItem> rows, string suggestedBaseName);

    Task CopyTextAsync(string? text);
    Task OpenUrlAsync(string url);

    Task ShowMessageAsync(string heading, string message, string? title = null);

    /// <summary>
    /// Shows the settings dialog; returns <c>true</c> when the user pressed OK.
    /// </summary>
    Task<bool> ShowSettingsDialogAsync();
    Task ShowAboutDialogAsync();
}

public sealed class DialogService(
    ISettingsService settings,
    IExifToolPathResolver resolver,
    IExportService export,
    IThemeService theme) : IDialogService
{
    public Window? Owner { get; set; }

    private TopLevel? Top => Owner is null ? null : TopLevel.GetTopLevel(Owner);
    private IStorageProvider? Storage => Top?.StorageProvider;

    public async Task<string?> PickImageFileAsync()
    {
        if (Storage is not { } sp) return null;

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open image",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll, FilePickerFileTypes.All],
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickExecutableAsync()
    {
        if (Storage is not { } sp) return null;

        var executable = new FilePickerFileType("ExifTool executable")
        {
            Patterns = OperatingSystem.IsWindows() ? ["*.exe"] : ["*"],
        };

        var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select the ExifTool executable",
            AllowMultiple = false,
            FileTypeFilter = [executable, FilePickerFileTypes.All],
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickBinaryDestinationAsync(string suggestedFileName)
    {
        if (Storage is not { } sp) return null;

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Extract binary data",
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true,
            FileTypeChoices = [FilePickerFileTypes.All],
        });

        return file?.TryGetLocalPath();
    }

    public async Task<bool> ExportAsync(ExportFileType type, IReadOnlyList<ExifTagItem> rows, string suggestedBaseName)
    {
        if (Storage is not { } sp) return false;

        var (extension, choice) = DescribeFormat(type);

        var file = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export metadata",
            SuggestedFileName = suggestedBaseName + extension,
            DefaultExtension = extension.TrimStart('.'),
            ShowOverwritePrompt = true,
            FileTypeChoices = [choice],
        });
        if (file is null) return false;

        try
        {
            // Prefer a truncating file handle so a shorter export can't leave stale trailing bytes.
            var localPath = file.TryGetLocalPath();
            var stream = localPath is not null ? File.Create(localPath) : await file.OpenWriteAsync();
            await using (stream)
            {
                await export.ExportAsync(type, stream, rows);
            }
            return true;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Export failed", ex.Message);
            return false;
        }
    }

    public async Task CopyTextAsync(string? text)
    {
        if (text is null) return;
        if (Top?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public async Task OpenUrlAsync(string url)
    {
        if (Top is not { } top) return;
        try
        {
            await top.Launcher.LaunchUriAsync(new Uri(url));
        }
        catch
        {
            // Launching is best-effort; a missing handler must not crash the app.
        }
    }

    public async Task ShowMessageAsync(string heading, string message, string? title = null)
    {
        if (Owner is not { } owner) return;

        var vm = new MessageBoxViewModel(heading, message);
        var window = new MessageBox { DataContext = vm, Title = title ?? "ExifGlass", Topmost = owner.Topmost };
        vm.CloseRequested += window.Close;
        await window.ShowDialog(owner);
    }

    public async Task<bool> ShowSettingsDialogAsync()
    {
        if (Owner is not { } owner) return false;

        var vm = new SettingsViewModel(settings, resolver, theme, this);
        var window = new SettingsWindow { DataContext = vm, Topmost = owner.Topmost };
        vm.CloseRequested += result => window.Close(result);

        var ok = await window.ShowDialog<bool>(owner);
        if (ok)
        {
            // Always-on-top is a window property; apply the freshly saved value to the owner.
            owner.Topmost = settings.Config.AlwaysOnTop;
        }
        return ok;
    }

    public async Task ShowAboutDialogAsync()
    {
        if (Owner is not { } owner) return;

        var vm = new AboutViewModel(this);
        var window = new AboutWindow { DataContext = vm, Topmost = owner.Topmost };
        vm.CloseRequested += window.Close;
        await window.ShowDialog(owner);
    }

    private static (string Extension, FilePickerFileType Choice) DescribeFormat(ExportFileType type) => type switch
    {
        ExportFileType.Text => (".txt", new FilePickerFileType("Text file") { Patterns = ["*.txt"], MimeTypes = ["text/plain"] }),
        ExportFileType.Csv => (".csv", new FilePickerFileType("CSV file") { Patterns = ["*.csv"], MimeTypes = ["text/csv"] }),
        ExportFileType.Json => (".json", new FilePickerFileType("JSON file") { Patterns = ["*.json"], MimeTypes = ["application/json"] }),
        _ => (".txt", FilePickerFileTypes.All),
    };
}
