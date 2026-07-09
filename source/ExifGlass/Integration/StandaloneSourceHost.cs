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
using Avalonia.Threading;

namespace ExifGlass.Integration;

/// <summary>
/// Standalone entry mode: the app was launched directly by the user. It emits the initial
/// CLI file (if any) on <see cref="Start"/>. It never touches <c>ToolBase.RunAsync</c>, so the
/// SDK's "missing --pipe" guard can't fire. Drag-drop and the file picker load through the view
/// model directly; this host only carries the launch file.
/// </summary>
public sealed class StandaloneSourceHost(string? initialFilePath) : IImageSourceHost
{
    public event EventHandler<FileRequestedEventArgs>? FileRequested;

    // Required by IImageSourceHost; standalone mode quits through the window, never this event.
#pragma warning disable CS0067
    public event EventHandler? CloseRequested;
#pragma warning restore CS0067

    public void Start()
    {
        if (!string.IsNullOrEmpty(initialFilePath))
        {
            RequestFile(initialFilePath, activate: false);
        }
    }

    /// <summary>Requests a file load; safe to call from any thread.</summary>
    public void RequestFile(string? filePath, bool activate = true)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            FileRequested?.Invoke(this, new FileRequestedEventArgs(filePath, activate));
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
                FileRequested?.Invoke(this, new FileRequestedEventArgs(filePath, activate)));
        }
    }

    public void Dispose()
    {
        // Nothing to release; the standalone host holds no unmanaged resources.
    }
}
