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
using System.Diagnostics;
using System.Text;

namespace ExifGlass.Core.Services;

/// <summary>
/// A long-lived <c>exiftool -stay_open True -@ -</c> process. Feeding argument sets over stdin
/// and reading stdout up to a per-command <c>{ready}</c> sentinel removes the dominant per-read
/// cost — process/interpreter startup — so live navigation stays fast.
/// </summary>
/// <remarks>
/// Access is serialized (one command at a time). Cancellation is observed only while waiting for
/// the turn: once a command has been written, it is read to completion so the process never
/// desyncs — a superseded read finishes quickly (that is the whole point of the daemon) and its
/// result is discarded by the caller. The process is killed and restarted transparently if the
/// pipe breaks or a command stalls. Raw <see cref="Process"/> piping keeps this AOT-safe.
/// </remarks>
internal sealed class ExifToolDaemon(string exePath) : IDisposable
{
    // A stalled command should not hang the UI forever; kill and restart past this.
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private Process? _process;
    private int _seq;
    private bool _disposed;

    /// <summary>
    /// The resolved ExifTool executable this daemon was started for.
    /// </summary>
    public string ExePath { get; } = exePath;

    /// <summary>
    /// Runs one ExifTool command and returns its captured stdout/stderr. Throws
    /// <see cref="OperationCanceledException"/> only if cancelled before the command is written.
    /// </summary>
    public async Task<(string StdOut, string StdErr)> ExecuteAsync(
        IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            try
            {
                return await RunCommandAsync(args).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException)
            {
                // The pipe broke or the command stalled — restart once and retry.
                KillProcess();
                return await RunCommandAsync(args).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<(string, string)> RunCommandAsync(IReadOnlyList<string> args)
    {
        var process = EnsureProcess();
        var seq = ++_seq;
        var readyOut = $"{{ready{seq}}}";
        var readyErr = $"{{igerr{seq}}}";

        var input = process.StandardInput;
        foreach (var arg in args)
        {
            await input.WriteLineAsync(arg).ConfigureAwait(false);
        }
        // -echo4 prints its argument to stderr after the command completes, framing that stream
        // the same way the numbered -execute frames stdout with {ready<seq>}.
        await input.WriteLineAsync("-echo4").ConfigureAwait(false);
        await input.WriteLineAsync(readyErr).ConfigureAwait(false);
        await input.WriteLineAsync($"-execute{seq}").ConfigureAwait(false);
        await input.FlushAsync().ConfigureAwait(false);

        using var timeout = new CancellationTokenSource(CommandTimeout);
        try
        {
            var stdout = ReadUntilAsync(process.StandardOutput, readyOut, timeout.Token);
            var stderr = ReadUntilAsync(process.StandardError, readyErr, timeout.Token);
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            return (stdout.Result, stderr.Result);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException("ExifTool did not respond in time.");
        }
    }

    private static async Task<string> ReadUntilAsync(
        StreamReader reader, string sentinel, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (line == sentinel) break;
            builder.Append(line).Append('\n');
        }
        return builder.ToString();
    }

    private Process EnsureProcess()
    {
        if (_process is { HasExited: false }) return _process;

        KillProcess();

        var psi = new ProcessStartInfo
        {
            FileName = ExePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
        };
        psi.ArgumentList.Add("-stay_open");
        psi.ArgumentList.Add("True");
        psi.ArgumentList.Add("-@");
        psi.ArgumentList.Add("-");

        _process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ExifTool.");
        _seq = 0;
        return _process;
    }

    private void KillProcess()
    {
        var process = _process;
        _process = null;
        if (process is null) return;

        try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
        try { process.Dispose(); } catch { /* best-effort */ }
    }

    public void Dispose()
    {
        _disposed = true;

        var process = _process;
        _process = null;
        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    // Graceful stop: ExifTool exits when it sees "-stay_open False".
                    process.StandardInput.WriteLine("-stay_open");
                    process.StandardInput.WriteLine("False");
                    process.StandardInput.Flush();
                    if (!process.WaitForExit(1000)) process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            }
            try { process.Dispose(); } catch { /* best-effort */ }
        }

        _gate.Dispose();
    }
}
