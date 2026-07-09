using ExifGlass.Core.Models;

namespace ExifGlass.Core.Helpers;

/// <summary>
/// Parses ExifTool's tab-delimited output produced by the <c>-t -G -H</c> flags.
/// </summary>
/// <remarks>
/// Each metadata row is one physical line of four tab-separated fields:
/// <c>Group\tTagId\tTagName\tValue</c>. A line that does not split into four fields is
/// treated as a continuation of the previous value (values may contain embedded newlines);
/// embedded tabs inside a value are folded back in because the split is capped at four parts.
/// </remarks>
public static class ExifToolOutputParser
{
    /// <summary>
    /// Parses raw stdout into ordered, 1-indexed rows.
    /// </summary>
    public static IReadOnlyList<ExifTagItem> Parse(string? output)
    {
        var items = new List<ExifTagItem>();
        if (string.IsNullOrEmpty(output)) return items;

        // Normalize newlines so \r\n and lone \r/\n all split cleanly.
        var normalized = output.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');

        var index = 0;
        foreach (var line in lines)
        {
            if (line.Length == 0) continue;

            // Cap at 4 so tabs embedded in the value fold into the last field.
            var fields = line.Split('\t', 4);
            if (fields.Length == 4)
            {
                items.Add(new ExifTagItem
                {
                    Index = ++index,
                    TagGroup = fields[0],
                    TagId = fields[1],
                    TagName = fields[2],
                    TagValue = fields[3],
                });
            }
            else if (items.Count > 0)
            {
                // Continuation of a multi-line value.
                var prev = items[^1];
                items[^1] = prev with { TagValue = prev.TagValue + "\n" + line };
            }
            // Otherwise it is preamble before the first row — ignore.
        }

        return items;
    }

    /// <summary>
    /// Heuristic guard for the startup self-check: confirms the parsed rows carry a
    /// hex tag id in the expected column, catching a silent ExifTool format change.
    /// </summary>
    public static bool LooksValid(IReadOnlyList<ExifTagItem> items)
    {
        if (items.Count == 0) return false;
        foreach (var item in items)
        {
            if (LooksLikeTagId(item.TagId)) return true;
        }
        return false;
    }

    /// <summary>
    /// <c>true</c> when the string looks like an ExifTool hex tag id (e.g. <c>0x0110</c> or <c>010f</c>).
    /// </summary>
    public static bool LooksLikeTagId(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        var span = value.AsSpan();
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) span = span[2..];
        if (span.Length == 0) return false;

        foreach (var c in span)
        {
            if (!Uri.IsHexDigit(c)) return false;
        }
        return true;
    }
}
