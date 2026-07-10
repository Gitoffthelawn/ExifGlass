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
using ImageGlass.SDK.Tools;

namespace ExifGlass.Integration;

/// <summary>
/// ImageGlass 10 integrated entry mode. Subclasses the SDK's <see cref="ToolBase"/> to attach to
/// the host's named pipe and receive automatic <c>PHOTO_CHANGED</c> updates for live navigation.
/// The pipe says <em>which file and when</em>; ExifTool remains the source of truth, so every
/// request just carries the path and the view model runs its own local read.
/// </summary>
/// <remarks>
/// The SDK dispatches lifecycle/event callbacks on the pipe read-loop thread, never the UI
/// thread. Every raise here marshals onto the UI thread via <see cref="Dispatcher"/>. The event
/// callbacks are synchronous <c>void</c>s and must never throw or block, so exceptions are
/// swallowed and the actual read happens asynchronously on the UI side.
/// </remarks>
public sealed class ImageGlassSourceHost(string[] args) : ToolBase, IImageSourceHost
{
    public override string ToolId => "Tool_ExifGlass";

    public event EventHandler<FileRequestedEventArgs>? FileRequested;
    public event EventHandler? CloseRequested;

    public void Start()
    {
        // RunAsync blocks in the pipe message loop until SHUTDOWN, so run it off the UI thread.
        // A pipe failure just leaves the window idle — there is nothing actionable to surface.
        _ = Task.Run(async () =>
        {
            try
            {
                await RunAsync(args).ConfigureAwait(false);
            }
            catch
            {
                // Connection/handshake failure: stay quietly idle.
            }
        });
    }

    /// <summary>
    /// Fetch the photo already loaded in ImageGlass when the tool first attaches.
    /// </summary>
    protected override Task OnInitializedAsync() => RequestCurrentPhotoAsync(activate: false);

    /// <summary>
    /// Re-invoked via the ImageGlass hotkey: refresh and bring the window forward.
    /// </summary>
    protected override Task OnExecuteAsync(CancellationToken ct) => RequestCurrentPhotoAsync(activate: true);

    /// <summary>
    /// The live-update trigger: fired automatically as the user navigates images.
    /// </summary>
    protected override void OnPhotoChanged(PhotoChangedEventArgs e) => RaiseFileRequested(e.FilePath, activate: false);

    protected override Task OnShutdownAsync()
    {
        Dispatcher.UIThread.Post(() => CloseRequested?.Invoke(this, EventArgs.Empty));
        return Task.CompletedTask;
    }

    private async Task RequestCurrentPhotoAsync(bool activate)
    {
        try
        {
            var photo = await HostApi.GetPhotoMetadataAsync().ConfigureAwait(false);
            RaiseFileRequested(photo?.FilePath, activate);
        }
        catch
        {
            // Best-effort: a PHOTO_CHANGED event will follow with the current file.
        }
    }

    private void RaiseFileRequested(string? filePath, bool activate) => Dispatcher.UIThread.Post(() =>
            FileRequested?.Invoke(this, new FileRequestedEventArgs(filePath, activate)));

    // Dispose is inherited from ToolBase (disconnects the pipe client) and satisfies
    // IImageSourceHost.Dispose.
}
