using CommunityToolkit.Mvvm.Input;
using ExifGlass.Helpers;
using ExifGlass.Services;

namespace ExifGlass.ViewModels;

/// <summary>Backs the About dialog: version, links, and a notify-only update check.</summary>
public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;

    public event Action? CloseRequested;

    public string Version => AppInfo.Version;
    public string WebsiteUrl => AppInfo.WebsiteUrl;

    public AboutViewModel(IDialogService dialogs) => _dialogs = dialogs;

    [RelayCommand]
    private Task OpenWebsiteAsync() => _dialogs.OpenUrlAsync(AppInfo.WebsiteUrl);

    [RelayCommand]
    private Task OpenStoreAsync() => _dialogs.OpenUrlAsync(AppInfo.StoreUrl);

    [RelayCommand]
    private Task CheckForUpdateAsync() => _dialogs.OpenUrlAsync(AppInfo.ReleasesUrl);

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
