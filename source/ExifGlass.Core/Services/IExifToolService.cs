using ExifGlass.Core.Models;

namespace ExifGlass.Core.Services;

/// <summary>
/// Reads image metadata via ExifTool. The one-shot implementation may later be swapped
/// for a persistent daemon without changing this contract.
/// </summary>
public interface IExifToolService : IDisposable
{
    /// <summary>
    /// Reads metadata for <paramref name="filePath"/>. Never throws to the caller for
    /// expected failures (missing tool, unreadable file) — those surface as an
    /// unsuccessful <see cref="ExifReadResult"/>. Only <see cref="OperationCanceledException"/>
    /// propagates so a superseded read can be discarded.
    /// </summary>
    Task<ExifReadResult> ReadAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Builds the display command line for reading <paramref name="filePath"/>.</summary>
    string BuildCommandPreview(string filePath);

    /// <summary>Probes the resolved ExifTool executable and reports its availability/version.</summary>
    Task<ExifToolStatus> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes any temporary files created during reads.</summary>
    void CleanupTempFiles();
}
