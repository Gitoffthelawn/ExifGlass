namespace ExifGlass.Integration;

/// <summary>
/// Describes a file the view model should load, raised by an <see cref="IImageSourceHost"/>.
/// </summary>
/// <param name="FilePath">The file to load; <c>null</c> clears the current view.</param>
/// <param name="Activate">Whether the window should be brought to the foreground.</param>
public sealed record FileRequestedEventArgs(string? FilePath, bool Activate);

/// <summary>
/// A source of "load this file" requests. Both entry modes (standalone launch and the
/// ImageGlass pipe) funnel through this single seam into
/// <see cref="ViewModels.MainWindowViewModel.LoadFileAsync"/>. Implementations always raise
/// <see cref="FileRequested"/> and <see cref="CloseRequested"/> on the UI thread.
/// </summary>
public interface IImageSourceHost : IDisposable
{
    /// <summary>
    /// Raised (on the UI thread) when a file should be loaded or cleared.
    /// </summary>
    event EventHandler<FileRequestedEventArgs>? FileRequested;

    /// <summary>
    /// Raised (on the UI thread) when the host asks the app to quit.
    /// </summary>
    event EventHandler? CloseRequested;

    /// <summary>
    /// Begins producing requests (e.g. emits the initial file, or connects the pipe).
    /// </summary>
    void Start();
}
