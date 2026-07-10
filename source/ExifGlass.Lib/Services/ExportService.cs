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
using System.Text;
using System.Text.Json;

namespace ExifGlass.Core.Services;

/// <summary>
/// Builds Text / CSV / JSON exports from metadata rows. Reflection-free: JSON goes
/// through the source-generated <see cref="AppJsonContext"/>.
/// </summary>
public sealed class ExportService : IExportService
{
    // RFC-4180 mandates CRLF line endings regardless of host OS.
    private const string Crlf = "\r\n";

    public async Task ExportAsync(
        ExportFileType type,
        Stream destination,
        IReadOnlyList<ExifTagItem> rows,
        CancellationToken cancellationToken = default)
    {
        var content = type switch
        {
            ExportFileType.Text => BuildText(rows),
            ExportFileType.Csv => BuildCsv(rows),
            ExportFileType.Json => BuildJson(rows),
            _ => string.Empty,
        };

        var bytes = Encoding.UTF8.GetBytes(content);
        await destination.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public string BuildText(IReadOnlyList<ExifTagItem> rows)
    {
        var sb = new StringBuilder();
        var currentGroup = string.Empty;

        foreach (var item in rows)
        {
            if (!string.Equals(item.TagGroup, currentGroup, StringComparison.Ordinal))
            {
                // Blank line before every group heading except the first.
                if (currentGroup.Length > 0) sb.Append('\n');
                sb.Append('[').Append(item.TagGroup).Append(']').Append('\n');
                currentGroup = item.TagGroup;
            }

            sb.Append(item.TagId).Append('\t')
              .Append(item.TagName).Append('\t')
              .Append(item.TagValue).Append('\n');
        }

        return sb.ToString();
    }

    public string BuildCsv(IReadOnlyList<ExifTagItem> rows)
    {
        var sb = new StringBuilder();
        sb.Append("Index,TagGroup,TagId,TagName,TagValue").Append(Crlf);

        foreach (var item in rows)
        {
            sb.Append(Field(item.Index.ToString())).Append(',')
              .Append(Field(item.TagGroup)).Append(',')
              .Append(Field(item.TagId)).Append(',')
              .Append(Field(item.TagName)).Append(',')
              .Append(Field(item.TagValue)).Append(Crlf);
        }

        return sb.ToString();
    }

    public string BuildJson(IReadOnlyList<ExifTagItem> rows)
    {
        var list = rows as List<ExifTagItem> ?? [.. rows];
        return JsonSerializer.Serialize(list, AppJsonContext.Default.ListExifTagItem);
    }

    /// <summary>
    /// Quotes an RFC-4180 field, doubling any embedded quotes.
    /// </summary>
    private static string Field(string value)
        => "\"" + value.Replace("\"", "\"\"") + "\"";
}
