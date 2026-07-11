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
using System.Globalization;

namespace ExifGlass.Core.Helpers;

/// <summary>
/// Decides whether an automatic update check is due, given the last-checked timestamp.
/// Pure and side-effect-free so it can be unit-tested without a clock or network.
/// </summary>
public static class UpdateThrottle
{
    /// <summary>
    /// <c>true</c> if a check should run: never checked, an unparseable timestamp (fail open),
    /// or at least <paramref name="interval"/> has elapsed since <paramref name="lastCheckIso"/>.
    /// </summary>
    public static bool ShouldCheck(string? lastCheckIso, DateTimeOffset now, TimeSpan interval)
    {
        if (string.IsNullOrWhiteSpace(lastCheckIso)) return true;

        if (!DateTimeOffset.TryParse(lastCheckIso, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var last))
        {
            return true;
        }

        return now - last >= interval;
    }
}
