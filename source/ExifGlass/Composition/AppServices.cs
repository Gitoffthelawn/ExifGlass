using ExifGlass.Core.Services;
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

    public AppServices()
    {
        Settings = new SettingsService();
        ExifToolPathResolver = new ExifToolPathResolver();
        ProcessRunner = new CliWrapProcessRunner();
        ExifToolService = new ExifToolService(ExifToolPathResolver, ProcessRunner, Settings);
    }

    /// <summary>Creates the main window's view model bound to the shared services.</summary>
    public MainWindowViewModel CreateMainWindowViewModel()
        => new(ExifToolService, Settings);

    public void Dispose()
    {
        ExifToolService.Dispose();
    }
}
