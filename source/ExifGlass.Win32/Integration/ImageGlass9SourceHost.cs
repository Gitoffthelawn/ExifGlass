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
using ImageGlass.Tools;

namespace ExifGlass.Integration;

/// <summary>
/// ImageGlass 9 integrated entry mode. Wraps the Windows-only ImageGlass.Tools
/// <see cref="ImageGlassTool"/> named-pipe client to receive <c>IMAGE_LOADING</c> events for live
/// navigation. The pipe says <em>which file and when</em>; ExifTool remains the source of truth, so
/// every request just carries the path and the view model runs its own local read.
/// </summary>
/// <remarks>
/// The pipe client raises its events on a read-loop thread, never the UI thread, so every raise
/// here marshals onto the UI thread via <see cref="Dispatcher"/>. Because this host depends on the
/// Windows-only ImageGlass.Tools package it lives in the executable head rather than the
/// cross-platform library, and is wired in through <see cref="App.SourceHostFactory"/>.
/// </remarks>
public sealed class ImageGlass9SourceHost(string? initialFilePath) : IImageSourceHost
{
    private readonly ImageGlassTool _tool = new();

    public event EventHandler<FileRequestedEventArgs>? FileRequested;
    public event EventHandler? CloseRequested;

    public void Start()
    {
        _tool.ToolMessageReceived += OnToolMessageReceived;
        _tool.ToolClosingRequest += OnToolClosingRequest;

        // Emit the file ImageGlass passed on the command line (if any) so the first view is
        // populated immediately, before the pipe delivers its first IMAGE_LOADING event.
        if (!string.IsNullOrEmpty(initialFilePath))
        {
            RaiseFileRequested(initialFilePath, activate: false);
        }

        // ConnectAsync blocks on the pipe handshake; a failure just leaves the window idle, so it
        // is fire-and-forget with the exception swallowed (there is nothing actionable to surface).
        _ = ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        try
        {
            await _tool.ConnectAsync().ConfigureAwait(false);
        }
        catch
        {
            // Connection/handshake failure: stay quietly idle.
        }
    }

    private void OnToolClosingRequest(object? sender, DisconnectedEventArgs e) =>
        Dispatcher.UIThread.Post(() => CloseRequested?.Invoke(this, EventArgs.Empty));

    private void OnToolMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.MessageData)) return;

        // The live-update trigger: ImageGlass 9 raises IMAGE_LOADING as the user navigates images.
        if (!e.MessageName.Equals(ImageGlassEvents.IMAGE_LOADING, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var args = IgImageLoadingEventArgs.Deserialize(e.MessageData);
        if (args?.FilePath is not { Length: > 0 } filePath) return;

        RaiseFileRequested(filePath, activate: false);
    }

    private void RaiseFileRequested(string filePath, bool activate) => Dispatcher.UIThread.Post(() =>
        FileRequested?.Invoke(this, new FileRequestedEventArgs(filePath, activate)));

    public void Dispose()
    {
        _tool.ToolMessageReceived -= OnToolMessageReceived;
        _tool.ToolClosingRequest -= OnToolClosingRequest;
        _tool.Dispose();
    }
}
