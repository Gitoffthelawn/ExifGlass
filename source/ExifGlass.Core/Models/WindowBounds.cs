namespace ExifGlass.Core.Models;

/// <summary>
/// Persisted position, size, and state of the main window.
/// </summary>
public sealed class WindowBounds
{
    public int X { get; set; } = 200;
    public int Y { get; set; } = 200;
    public int Width { get; set; } = 600;
    public int Height { get; set; } = 800;

    /// <summary>
    /// <c>true</c> when the window was maximized at last save.
    /// </summary>
    public bool Maximized { get; set; }
}
