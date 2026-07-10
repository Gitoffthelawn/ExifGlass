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
using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;
using System.ComponentModel;

namespace ExifGlass.Core.Services;

/// <summary>
/// ExifTool reader. Reads go through a persistent <c>-stay_open</c> daemon (see
/// <see cref="ExifToolDaemon"/>) for fast live navigation; the rarer binary-extraction and
/// validation calls stay one-shot through <see cref="IProcessRunner"/>. Both paths share the
/// single <see cref="ExifToolCommand"/> argument builder, so a read and its footer preview can't drift.
/// </summary>
public sealed class ExifToolService(
    IExifToolPathResolver resolver,
    IProcessRunner runner,
    ISettingsService settings) : IExifToolService
{
    private readonly List<string> _tempFiles = [];
    private readonly Lock _tempLock = new();

    // Persistent daemon, recreated if the resolved executable changes or it crashes.
    private readonly Lock _daemonLock = new();
    private ExifToolDaemon? _daemon;

    public string BuildCommandPreview(string filePath)
    {
        var cfg = settings.Config;
        var exe = resolver.Resolve(cfg.ExifToolPath);
        return ExifToolCommand.BuildPreview(exe, filePath, cfg.ExifToolArguments);
    }

    public async Task<ExifReadResult> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return ExifReadResult.Empty();
        }

        var cfg = settings.Config;
        var exe = resolver.Resolve(cfg.ExifToolPath);
        var preview = ExifToolCommand.BuildPreview(exe, filePath, cfg.ExifToolArguments);
        var originalName = Path.GetFileName(filePath);

        // Windows-only: ExifTool cannot open paths with codepoints above the ANSI range,
        // so copy to an ASCII temp path and read that instead.
        var readPath = filePath;
        string? tempCopy = null;
        if (PlatformInfo.NeedsAnsiPathWorkaround(filePath))
        {
            tempCopy = TryCreateAnsiCopy(filePath);
            if (tempCopy is not null) readPath = tempCopy;
        }

        string stdout, stderr;
        try
        {
            var daemon = GetDaemon(exe);
            var args = ExifToolCommand.BuildArgs(readPath, cfg.ExifToolArguments);
            (stdout, stderr) = await daemon.ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsExecutableMissing(ex))
        {
            return ExifReadResult.Failure(MissingToolMessage(exe), preview);
        }
        catch (Exception ex)
        {
            return ExifReadResult.Failure(ex.Message, preview);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tags = ExifToolOutputParser.Parse(stdout);

        // Restore the real file name when we read from a temp copy.
        if (tempCopy is not null && tags.Count > 0)
        {
            tags = RemapFileName(tags, originalName);
        }

        if (tags.Count == 0)
        {
            // The daemon has no per-command exit code; empty output means either an error
            // (surfaced on stderr) or a file with no readable metadata.
            var message = string.IsNullOrWhiteSpace(stderr)
                ? "No metadata was found for this file."
                : stderr.Trim();
            return ExifReadResult.Failure(message, preview);
        }

        return new ExifReadResult(tags, preview, true, null);
    }

    public async Task<string?> ExtractBinaryTagAsync(
        string sourceFilePath,
        string tagName,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || string.IsNullOrWhiteSpace(tagName))
        {
            return "There is no binary data to extract.";
        }

        var cfg = settings.Config;
        var exe = resolver.Resolve(cfg.ExifToolPath);

        // Same Windows-only ANSI-path workaround as reads: ExifTool cannot open a source
        // path with codepoints above the ANSI range.
        var readPath = sourceFilePath;
        if (PlatformInfo.NeedsAnsiPathWorkaround(sourceFilePath)
            && TryCreateAnsiCopy(sourceFilePath) is { } ansiCopy)
        {
            readPath = ansiCopy;
        }

        // ExifTool command names have no spaces (e.g. "Thumbnail Image" -> ThumbnailImage).
        var tagArg = "-" + tagName.Replace(" ", "");

        string extractDir;
        try
        {
            AppPaths.EnsureTempDir();
            extractDir = Path.Combine(AppPaths.TempDir, "extract-" + Path.GetRandomFileName());
            Directory.CreateDirectory(extractDir);
        }
        catch (Exception ex)
        {
            return $"Could not create a temporary folder for extraction: {ex.Message}";
        }

        try
        {
            // -w! writes to "<extractDir>/<sourceBaseName><suffix>"; %f is the input base name.
            var suffix = "_extracted";
            var outFormat = Path.Combine(extractDir, "%f" + suffix);
            string[] args = [tagArg, "-b", "-w!", outFormat, readPath];

            ProcessResult result;
            try
            {
                result = await runner.RunAsync(exe, args, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsExecutableMissing(ex))
            {
                return MissingToolMessage(exe);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            var produced = Path.Combine(extractDir, Path.GetFileNameWithoutExtension(readPath) + suffix);
            if (!File.Exists(produced))
            {
                var message = result.StandardError?.Trim();
                return string.IsNullOrEmpty(message)
                    ? "ExifTool produced no data for this tag."
                    : message;
            }

            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
            File.Move(produced, destinationPath, overwrite: true);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            try { Directory.Delete(extractDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    public async Task<ExifToolStatus> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var exe = resolver.Resolve(settings.Config.ExifToolPath);
        try
        {
            var result = await runner.RunAsync(exe, ["-ver"], cancellationToken).ConfigureAwait(false);
            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return new ExifToolStatus(true, exe, result.StandardOutput.Trim(), null);
            }
            return new ExifToolStatus(false, exe, null, "ExifTool did not respond as expected.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (IsExecutableMissing(ex))
        {
            return new ExifToolStatus(false, exe, null, MissingToolMessage(exe));
        }
        catch (Exception ex)
        {
            return new ExifToolStatus(false, exe, null, ex.Message);
        }
    }

    public void CleanupTempFiles()
    {
        lock (_tempLock)
        {
            foreach (var path in _tempFiles)
            {
                try { File.Delete(path); }
                catch { /* best-effort */ }
            }
            _tempFiles.Clear();
        }
    }

    public void Dispose()
    {
        lock (_daemonLock)
        {
            _daemon?.Dispose();
            _daemon = null;
        }
        CleanupTempFiles();
    }

    /// <summary>
    /// Returns the persistent daemon, (re)creating it if none exists yet or the resolved
    /// executable has changed (e.g. a new path set in Settings). Extra arguments are sent per
    /// command, so an argument change needs no restart.
    /// </summary>
    private ExifToolDaemon GetDaemon(string exe)
    {
        lock (_daemonLock)
        {
            if (_daemon is not null
                && !string.Equals(_daemon.ExePath, exe, StringComparison.OrdinalIgnoreCase))
            {
                _daemon.Dispose();
                _daemon = null;
            }
            return _daemon ??= new ExifToolDaemon(exe);
        }
    }

    private string? TryCreateAnsiCopy(string filePath)
    {
        try
        {
            AppPaths.EnsureTempDir();
            var ext = Path.GetExtension(filePath);
            var dest = Path.Combine(AppPaths.TempDir, Path.GetRandomFileName() + ext);
            File.Copy(filePath, dest, overwrite: true);
            lock (_tempLock) _tempFiles.Add(dest);
            return dest;
        }
        catch
        {
            // Fall back to reading the original path directly.
            return null;
        }
    }

    private static List<ExifTagItem> RemapFileName(IReadOnlyList<ExifTagItem> tags, string originalName)
    {
        var list = new List<ExifTagItem>(tags.Count);
        foreach (var t in tags)
        {
            list.Add(t.TagName.Equals("File Name", StringComparison.Ordinal)
                ? t with { TagValue = originalName }
                : t);
        }
        return list;
    }

    private static bool IsExecutableMissing(Exception ex)
        => ex is Win32Exception or FileNotFoundException
           || (ex.InnerException is not null && IsExecutableMissing(ex.InnerException));

    private static string MissingToolMessage(string exe)
        => $"ExifTool could not be started ('{exe}'). Install ExifTool or set its path in Settings.";
}
