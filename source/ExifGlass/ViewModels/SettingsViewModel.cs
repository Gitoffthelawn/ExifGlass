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

    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private bool _alwaysOnTop;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommandPreview))]
    private string _exifToolPath = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CommandPreview))]
    private string _exifToolArguments = "";

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
