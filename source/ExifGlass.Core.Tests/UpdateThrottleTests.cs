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

public class UpdateThrottleTests
{
    private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
    private static readonly DateTimeOffset Now = new(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ShouldCheck_WhenNeverChecked()
    {
        Assert.True(UpdateThrottle.ShouldCheck(null, Now, FiveDays));
        Assert.True(UpdateThrottle.ShouldCheck("", Now, FiveDays));
    }

    [Fact]
    public void ShouldCheck_WhenTimestampUnparseable_FailsOpen()
    {
        Assert.True(UpdateThrottle.ShouldCheck("not-a-date", Now, FiveDays));
    }

    [Fact]
    public void ShouldNotCheck_WithinWindow()
    {
        var recent = Now.AddDays(-2).ToString("O");
        Assert.False(UpdateThrottle.ShouldCheck(recent, Now, FiveDays));
    }

    [Fact]
    public void ShouldCheck_AtOrPastWindow()
    {
        Assert.True(UpdateThrottle.ShouldCheck(Now.AddDays(-5).ToString("O"), Now, FiveDays));
        Assert.True(UpdateThrottle.ShouldCheck(Now.AddDays(-30).ToString("O"), Now, FiveDays));
    }
}
