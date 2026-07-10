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
