using System.ComponentModel;
using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;

namespace ExifGlass.Core.Services;

/// <summary>
/// One-shot ExifTool reader: each read spawns a fresh process via <see cref="IProcessRunner"/>.
/// Correct and simple; the internal invocation can later become a persistent
/// <c>-stay_open</c> daemon behind the same <see cref="IExifToolService"/> contract.
/// </summary>
public sealed class ExifToolService(
    IExifToolPathResolver resolver,
    IProcessRunner runner,
    ISettingsService settings) : IExifToolService
{
    private readonly List<string> _tempFiles = [];
    private readonly Lock _tempLock = new();

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

        ProcessResult result;
        try
        {
            var args = ExifToolCommand.BuildArgs(readPath, cfg.ExifToolArguments);
            result = await runner.RunAsync(exe, args, cancellationToken).ConfigureAwait(false);
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

        // Non-zero exit with no output means a real failure.
        if (result.ExitCode != 0 && string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            var message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "ExifTool exited with an error while reading this file."
                : result.StandardError.Trim();
            return ExifReadResult.Failure(message, preview);
        }

        var tags = ExifToolOutputParser.Parse(result.StandardOutput);

        // Restore the real file name when we read from a temp copy.
        if (tempCopy is not null && tags.Count > 0)
        {
            tags = RemapFileName(tags, originalName);
        }

        if (tags.Count == 0)
        {
            var message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "No metadata was found for this file."
                : result.StandardError.Trim();
            return ExifReadResult.Failure(message, preview);
        }

        return new ExifReadResult(tags, preview, true, null);
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

    public void Dispose() => CleanupTempFiles();

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

    private static IReadOnlyList<ExifTagItem> RemapFileName(IReadOnlyList<ExifTagItem> tags, string originalName)
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
