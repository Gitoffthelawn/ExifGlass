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
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ExifGlass.Composition;
using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;
using ExifGlass.Integration;
using ExifGlass.Views;

namespace ExifGlass;

public partial class App : Application
{
    private AppServices? _services;
    private IImageSourceHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = CommandLine.Parse(desktop.Args ?? []);

            _services = new AppServices();

            // Layered load: defaults -> file -> CLI overrides. Done before the view model
            // is built so config-derived state is correct on first render. Synchronous:
            // the file is tiny and this must complete before any UI is shown.
            _services.Settings.Load();
            _services.Settings.ApplyOverrides(options.ConfigOverrides);

            var config = _services.Settings.Config;
            _services.ThemeService.Apply(config.Theme);

            var vm = _services.CreateMainWindowViewModel();
            var window = new MainWindow { DataContext = vm };
            RestoreWindow(window, config);

            // The dialog service reaches pickers/clipboard/launcher through this window.
            _services.Dialogs.Owner = window;

            desktop.MainWindow = window;

            // One seam, two entry modes: --pipe => ImageGlass integrated tool (live PHOTO_CHANGED
            // updates), otherwise standalone (CLI file). Both funnel into LoadFileAsync.
            _host = options.Mode == AppMode.ImageGlass
                ? new ImageGlassSourceHost(desktop.Args ?? [])
                : new StandaloneSourceHost(options.InitialFilePath);

            _host.FileRequested += (_, e) =>
            {
                if (e.Activate) BringToFront(window);
                _ = vm.LoadFileAsync(e.FilePath);
            };
            _host.CloseRequested += (_, _) => desktop.Shutdown();

            desktop.ShutdownRequested += (_, _) =>
            {
                _host?.Dispose();
                _services?.ExifToolService.CleanupTempFiles();
                _services?.Dispose();
            };

            // Emit the initial file / connect the pipe. The UI thread is never blocked by pipe IO.
            _host.Start();

            // Notify-only startup update check: standalone only (an integrated tool must not pop a
            // modal over ImageGlass), gated by config + a 5-day throttle, and best-effort so a
            // network failure never disrupts launch. Deferred to first open so the owner exists.
            if (options.Mode == AppMode.Standalone)
            {
                window.Opened += OnMainWindowOpenedForUpdateCheck;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnMainWindowOpenedForUpdateCheck(object? sender, EventArgs e)
    {
        // Run once.
        if (sender is Window window)
        {
            window.Opened -= OnMainWindowOpenedForUpdateCheck;
        }
        _ = _services?.Dialogs.CheckForUpdatesOnStartupAsync();
    }

    /// <summary>
    /// Brings the main window to the foreground (used on an ImageGlass hotkey re-invoke).
    /// </summary>
    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }
        window.Show();
        window.Activate();
    }

    private static void RestoreWindow(Window window, AppConfig config)
    {
        var bounds = config.Window;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
        window.Position = new PixelPoint(Math.Max(0, bounds.X), Math.Max(0, bounds.Y));
        window.Topmost = config.AlwaysOnTop;
        // The maximized state is applied in MainWindow.OnOpened — setting WindowState before
        // the platform window exists is unreliable in Avalonia.
    }
}
