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
using ExifGlass.Core.Services;
using ExifGlass.Services;
using ExifGlass.ViewModels;

namespace ExifGlass.Composition;

/// <summary>
/// Manual composition root. Wires the object graph with plain <c>new</c> calls —
/// no reflection DI container — for the fastest startup and zero trim/AOT warnings.
/// </summary>
public sealed class AppServices : IDisposable
{
    public ISettingsService Settings { get; }
    public IExifToolPathResolver ExifToolPathResolver { get; }
    public IProcessRunner ProcessRunner { get; }
    public IExifToolService ExifToolService { get; }
    public IExportService ExportService { get; }
    public IThemeService ThemeService { get; }
    public IDialogService Dialogs { get; }

    public AppServices()
    {
        Settings = new SettingsService();
        ExifToolPathResolver = new ExifToolPathResolver();
        ProcessRunner = new CliWrapProcessRunner();
        ExifToolService = new ExifToolService(ExifToolPathResolver, ProcessRunner, Settings);
        ExportService = new ExportService();
        ThemeService = new ThemeService();
        Dialogs = new DialogService(Settings, ExifToolPathResolver, ExportService, ThemeService);
    }

    /// <summary>
    /// Creates the main window's view model bound to the shared services.
    /// </summary>
    public MainWindowViewModel CreateMainWindowViewModel()
        => new(ExifToolService, Settings, Dialogs);

    public void Dispose()
    {
        ExifToolService.Dispose();
    }
}
