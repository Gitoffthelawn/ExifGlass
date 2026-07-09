using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;
using Xunit;

namespace ExifGlass.Core.Tests;

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
    public void Parse_CollectsSlashKeyValueOverrides()
    {
        var opts = CommandLine.Parse(["/Theme=Dark", "/WindowWidth=900", "photo.jpg"]);

        Assert.Equal("Dark", opts.ConfigOverrides["Theme"]);
        Assert.Equal("900", opts.ConfigOverrides["WindowWidth"]);
        Assert.Equal("photo.jpg", opts.InitialFilePath);
    }

    [Fact]
    public void Parse_OverrideKeysAreCaseInsensitive()
    {
        var opts = CommandLine.Parse(["/theme=Light"]);
        Assert.Equal("Light", opts.ConfigOverrides["THEME"]);
    }

    [Fact]
    public void Parse_TakesFirstBareTokenAsFile()
    {
        var opts = CommandLine.Parse(["--pipe", "/Theme=Dark", "first.jpg", "second.jpg"]);
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
