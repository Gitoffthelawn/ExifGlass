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
using Xunit;

namespace ExifGlass.Core.Tests;

public class ExifToolOutputParserTests
{
    [Fact]
    public void Parse_MapsFourTabColumnsToFields()
    {
        var output = "EXIF\t0x0110\tCamera Model Name\tNIKON D850\r\n";

        var tags = ExifToolOutputParser.Parse(output);

        var tag = Assert.Single(tags);
        Assert.Equal(1, tag.Index);
        Assert.Equal("EXIF", tag.TagGroup);
        Assert.Equal("0x0110", tag.TagId);
        Assert.Equal("Camera Model Name", tag.TagName);
        Assert.Equal("NIKON D850", tag.TagValue);
    }

    [Fact]
    public void Parse_AssignsSequentialOneBasedIndexes()
    {
        var output =
            "File\t-\tFile Name\ta.jpg\n" +
            "EXIF\t0x0112\tOrientation\tHorizontal\n" +
            "XMP\t0x0000\tCreator\tPhap\n";

        var tags = ExifToolOutputParser.Parse(output);

        Assert.Equal(3, tags.Count);
        Assert.Equal(1, tags[0].Index);
        Assert.Equal(2, tags[1].Index);
        Assert.Equal(3, tags[2].Index);
        Assert.Equal("XMP", tags[2].TagGroup);
    }

    [Fact]
    public void Parse_FoldsEmbeddedTabsIntoValue()
    {
        // A value containing tabs must not spill into extra columns.
        var output = "EXIF\t0x9286\tUser Comment\ttab\there\tthere\n";

        var tag = Assert.Single(ExifToolOutputParser.Parse(output));

        Assert.Equal("User Comment", tag.TagName);
        Assert.Equal("tab\there\tthere", tag.TagValue);
    }

    [Fact]
    public void Parse_TreatsNonFourFieldLineAsContinuation()
    {
        // A physical line without 4 fields continues the previous value.
        var output =
            "EXIF\t0x9c9c\tXP Comment\tline one\n" +
            "line two continues\n" +
            "line three continues\n";

        var tag = Assert.Single(ExifToolOutputParser.Parse(output));

        Assert.Equal("line one\nline two continues\nline three continues", tag.TagValue);
    }

    [Fact]
    public void Parse_KeepsBinaryMarkerSoExtractionCanBeOffered()
    {
        var output = "EXIF\t0x0201\tThumbnail Image\t(Binary data 8192 bytes, use -b option to extract)\n";

        var tag = Assert.Single(ExifToolOutputParser.Parse(output));

        Assert.True(tag.CanExtractBinary);
    }

    [Fact]
    public void Parse_EmptyOrNullReturnsEmpty()
    {
        Assert.Empty(ExifToolOutputParser.Parse(null));
        Assert.Empty(ExifToolOutputParser.Parse(""));
    }

    [Fact]
    public void LooksValid_TrueWhenHexTagIdPresent()
    {
        var tags = ExifToolOutputParser.Parse("EXIF\t0x0110\tModel\tX\n");
        Assert.True(ExifToolOutputParser.LooksValid(tags));
    }

    [Theory]
    [InlineData("0x0110", true)]
    [InlineData("010f", true)]
    [InlineData("ABCD", true)]
    [InlineData("-", false)]
    [InlineData("Composite", false)]
    [InlineData("", false)]
    public void LooksLikeTagId_RecognizesHex(string value, bool expected)
    {
        Assert.Equal(expected, ExifToolOutputParser.LooksLikeTagId(value));
    }
}
