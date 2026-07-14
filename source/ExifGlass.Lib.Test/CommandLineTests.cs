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
using Xunit;

namespace ExifGlass.Lib.Test;

public class CommandLineTests
{
    [Fact]
    public void Parse_DetectsStandaloneModeWithFile()
    {
        var opts = CommandLine.Parse(["C:\\photos\\a.jpg"]);

        Assert.Equal(AppMode.Standalone, opts.Mode);
        Assert.Equal("C:\\photos\\a.jpg", opts.InitialFilePath);
        Assert.Empty(opts.ConfigOverrides);
    }

    [Fact]
    public void Parse_DetectsImageGlassModeFromPipeFlag()
    {
        Assert.Equal(AppMode.ImageGlass, CommandLine.Parse(["--pipe"]).Mode);
        Assert.Equal(AppMode.ImageGlass, CommandLine.Parse(["--pipe=IG_Session_123"]).Mode);
    }

    [Fact]
    public void Parse_DetectsImageGlass9ModeFromToolPipeCode()
    {
        var opts = CommandLine.Parse(["--ig-tool-pipe-code=abc123", "C:\\photos\\a.jpg"]);

        Assert.Equal(AppMode.ImageGlass9, opts.Mode);
        Assert.Equal("C:\\photos\\a.jpg", opts.InitialFilePath);
    }

    [Fact]
    public void Parse_CollectsKeyValueOverrides()
    {
        var opts = CommandLine.Parse(["-p:Theme=Dark", "-p:WindowWidth=900", "photo.jpg"]);

        Assert.Equal("Dark", opts.ConfigOverrides["Theme"]);
        Assert.Equal("900", opts.ConfigOverrides["WindowWidth"]);
        Assert.Equal("photo.jpg", opts.InitialFilePath);
    }

    [Fact]
    public void Parse_OverrideKeysAreCaseInsensitive()
    {
        var opts = CommandLine.Parse(["-p:theme=Light"]);
        Assert.Equal("Light", opts.ConfigOverrides["THEME"]);
    }

    [Fact]
    public void Parse_TreatsUnixAbsolutePathAsFile()
    {
        // A leading '/' is an absolute path, never a config override (those use "-p:").
        var opts = CommandLine.Parse(["/home/phap/Pictures/test_rgb565.ithmb"]);

        Assert.Equal(AppMode.Standalone, opts.Mode);
        Assert.Equal("/home/phap/Pictures/test_rgb565.ithmb", opts.InitialFilePath);
        Assert.Empty(opts.ConfigOverrides);
    }

    [Fact]
    public void Parse_TreatsUnixPathContainingEqualsAsFile()
    {
        // '=' in a path segment must not turn an absolute path into an override.
        var opts = CommandLine.Parse(["/home/phap/a=b/photo.jpg"]);

        Assert.Equal("/home/phap/a=b/photo.jpg", opts.InitialFilePath);
        Assert.Empty(opts.ConfigOverrides);
    }

    [Fact]
    public void Parse_StillCollectsOverrideAlongsideUnixPath()
    {
        var opts = CommandLine.Parse(["-p:Theme=Dark", "/home/phap/photo.jpg"]);

        Assert.Equal("Dark", opts.ConfigOverrides["Theme"]);
        Assert.Equal("/home/phap/photo.jpg", opts.InitialFilePath);
    }

    [Fact]
    public void Parse_TakesFirstBareTokenAsFile()
    {
        var opts = CommandLine.Parse(["--pipe", "-p:Theme=Dark", "first.jpg", "second.jpg"]);
        Assert.Equal("first.jpg", opts.InitialFilePath);
    }

    [Theory]
    [InlineData("exifglass:C:%5Cimages%5Cpic.jpg", "C:\\images\\pic.jpg")]
    [InlineData("exifglass://C:/images/pic.jpg", "C:/images/pic.jpg")]
    [InlineData("C:\\plain\\path.png", "C:\\plain\\path.png")]
    public void NormalizeFileArgument_StripsSchemeAndDecodes(string input, string expected)
    {
        Assert.Equal(expected, CommandLine.NormalizeFileArgument(input));
    }
}
