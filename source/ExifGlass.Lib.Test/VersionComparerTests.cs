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

namespace ExifGlass.Lib.Test;

public class VersionComparerTests
{
    [Theory]
    [InlineData("1.10.0.0", "1.9.0.0", true)]   // 10 > 9, not lexical
    [InlineData("2.0.0.0", "1.99.0.0", true)]
    [InlineData("1.0.1", "1.0.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]        // equal is not newer
    [InlineData("1.0.0", "1.0.1", false)]
    [InlineData("1.2", "1.2.0.0", false)]        // padded equal
    public void IsNewer_ComparesNumerically(string candidate, string current, bool expected)
    {
        Assert.Equal(expected, VersionComparer.IsNewer(candidate, current));
    }

    [Fact]
    public void Compare_IgnoresPreReleaseAndBuildMetadata()
    {
        Assert.Equal(0, VersionComparer.Compare("1.2.3-beta+abc", "1.2.3"));
        Assert.True(VersionComparer.IsNewer("1.2.4-rc1", "1.2.3"));
    }

    [Fact]
    public void Compare_HandlesNullAndEmpty()
    {
        Assert.Equal(0, VersionComparer.Compare(null, ""));
        Assert.True(VersionComparer.IsNewer("1.0.0", null));
        Assert.False(VersionComparer.IsNewer(null, "1.0.0"));
    }
}
