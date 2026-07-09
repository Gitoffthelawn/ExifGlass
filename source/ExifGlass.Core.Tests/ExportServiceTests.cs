using System.Text;
using ExifGlass.Core.Models;
using ExifGlass.Core.Services;
using Xunit;

namespace ExifGlass.Core.Tests;

public class ExportServiceTests
{
    private static readonly ExifTagItem[] Sample =
    [
        new() { Index = 1, TagGroup = "File", TagId = "-", TagName = "File Name", TagValue = "a.jpg" },
        new() { Index = 2, TagGroup = "File", TagId = "-", TagName = "File Size", TagValue = "1.2 MB" },
        new() { Index = 3, TagGroup = "EXIF", TagId = "0x0110", TagName = "Camera Model Name", TagValue = "NIKON D850" },
    ];

    private readonly ExportService _svc = new();

    [Fact]
    public void BuildText_GroupsWithHeadingsAndBlankLineBetweenGroups()
    {
        var text = _svc.BuildText(Sample);

        Assert.Equal(
            "[File]\n" +
            "-\tFile Name\ta.jpg\n" +
            "-\tFile Size\t1.2 MB\n" +
            "\n" +
            "[EXIF]\n" +
            "0x0110\tCamera Model Name\tNIKON D850\n",
            text);
    }

    [Fact]
    public void BuildCsv_HasHeaderAndCrlfRows()
    {
        var csv = _svc.BuildCsv(Sample);
        var lines = csv.Split("\r\n");

        Assert.Equal("Index,TagGroup,TagId,TagName,TagValue", lines[0]);
        Assert.Equal("\"1\",\"File\",\"-\",\"File Name\",\"a.jpg\"", lines[1]);
        Assert.Equal("\"3\",\"EXIF\",\"0x0110\",\"Camera Model Name\",\"NIKON D850\"", lines[3]);
        // Trailing CRLF after the last row -> final split element is empty.
        Assert.Equal(string.Empty, lines[^1]);
    }

    [Fact]
    public void BuildCsv_EscapesQuotesCommasAndNewlinesPerRfc4180()
    {
        ExifTagItem[] tricky =
        [
            new() { Index = 1, TagGroup = "XMP", TagId = "0x0", TagName = "Comment", TagValue = "say \"hi\", now" },
            new() { Index = 2, TagGroup = "XMP", TagId = "0x1", TagName = "Note", TagValue = "line1\nline2" },
        ];

        var csv = _svc.BuildCsv(tricky);

        // Embedded quotes are doubled; commas/newlines stay inside the quoted field.
        Assert.Contains("\"say \"\"hi\"\", now\"", csv);
        Assert.Contains("\"line1\nline2\"", csv);
    }

    [Fact]
    public void BuildJson_IsArrayWithMetadataFieldsAndNoDerivedFlag()
    {
        var json = _svc.BuildJson(Sample);

        Assert.StartsWith("[", json.TrimStart());
        Assert.Contains("\"TagName\": \"Camera Model Name\"", json);
        Assert.Contains("\"TagValue\": \"NIKON D850\"", json);
        // The derived, UI-only flag must not leak into the export.
        Assert.DoesNotContain("CanExtractBinary", json);
    }

    [Fact]
    public async Task ExportAsync_WritesUtf8BytesMatchingTheBuilder()
    {
        using var stream = new MemoryStream();
        await _svc.ExportAsync(ExportFileType.Csv, stream, Sample);

        var written = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal(_svc.BuildCsv(Sample), written);
    }
}
