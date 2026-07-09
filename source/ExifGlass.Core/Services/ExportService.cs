using System.Text;
using System.Text.Json;
using ExifGlass.Core.Models;

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

    /// <summary>Quotes an RFC-4180 field, doubling any embedded quotes.</summary>
    private static string Field(string value)
        => "\"" + value.Replace("\"", "\"\"") + "\"";
}
