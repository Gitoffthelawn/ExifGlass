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
