using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ExifGlass.Composition;
using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;
using ExifGlass.ViewModels;

namespace ExifGlass;

public partial class App : Application
{
    private AppServices? _services;

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
            // is built so config-derived state is correct on first render. The file is tiny
            // and this runs before any UI is shown; ConfigureAwait(false) keeps it deadlock-free.
            _services.Settings.LoadAsync().GetAwaiter().GetResult();
            _services.Settings.ApplyOverrides(options.ConfigOverrides);

            var config = _services.Settings.Config;
            ApplyTheme(config.Theme);

            var vm = _services.CreateMainWindowViewModel();
            var window = new MainWindow { DataContext = vm };
            RestoreWindow(window, config);

            desktop.MainWindow = window;
            desktop.ShutdownRequested += (_, _) =>
            {
                _services?.ExifToolService.CleanupTempFiles();
                _services?.Dispose();
            };

            // Kick off the initial read (if launched with a file) without blocking startup.
            if (!string.IsNullOrEmpty(options.InitialFilePath))
            {
                _ = vm.LoadFileAsync(options.InitialFilePath);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ApplyTheme(ThemeMode mode)
    {
        if (Current is null) return;
        Current.RequestedThemeVariant = mode switch
        {
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }

    private static void RestoreWindow(Window window, AppConfig config)
    {
        var bounds = config.Window;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
        window.Position = new PixelPoint(Math.Max(0, bounds.X), Math.Max(0, bounds.Y));
        window.Topmost = config.AlwaysOnTop;
        if (bounds.Maximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }
}
