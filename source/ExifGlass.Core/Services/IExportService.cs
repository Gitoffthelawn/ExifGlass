using ExifGlass.Core.Models;

namespace ExifGlass.Core.Services;

/// <summary>
/// Serializes metadata rows to the supported export formats. Pure and UI-free: the
/// caller supplies the destination <see cref="Stream"/> (the exe's dialog layer opens it
/// from an <c>IStorageFile</c>).
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Writes <paramref name="rows"/> to <paramref name="destination"/> as UTF-8 in the given format.
    /// </summary>
    Task ExportAsync(
        ExportFileType type,
        Stream destination,
        IReadOnlyList<ExifTagItem> rows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grouped, tab-separated plain text with <c>[Group]</c> headings.
    /// </summary>
    string BuildText(IReadOnlyList<ExifTagItem> rows);

    /// <summary>
    /// RFC-4180 CSV: header + one quoted row per tag.
    /// </summary>
    string BuildCsv(IReadOnlyList<ExifTagItem> rows);

    /// <summary>
    /// Indented JSON array via the source-generated serializer.
    /// </summary>
    string BuildJson(IReadOnlyList<ExifTagItem> rows);
}
