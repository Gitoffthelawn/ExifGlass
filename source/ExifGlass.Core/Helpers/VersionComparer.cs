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
namespace ExifGlass.Core.Helpers;

/// <summary>
/// Compares dotted version strings tolerantly (ignores build metadata / pre-release
/// suffixes, pads missing components with zero).
/// </summary>
public static class VersionComparer
{
    /// <summary>
    /// Returns a negative number if <paramref name="left"/> is older than
    /// <paramref name="right"/>, zero if equal, positive if newer.
    /// </summary>
    public static int Compare(string? left, string? right)
    {
        var a = ParseComponents(left);
        var b = ParseComponents(right);

        var len = Math.Max(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            var av = i < a.Length ? a[i] : 0;
            var bv = i < b.Length ? b[i] : 0;
            if (av != bv) return av.CompareTo(bv);
        }
        return 0;
    }

    /// <summary>
    /// <c>true</c> when <paramref name="candidate"/> is strictly newer than <paramref name="current"/>.
    /// </summary>
    public static bool IsNewer(string? candidate, string? current)
        => Compare(candidate, current) > 0;

    private static int[] ParseComponents(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return [];

        // Trim any build/pre-release metadata: "1.2.3-beta+abc" => "1.2.3".
        var span = version.AsSpan().Trim();
        var cut = span.IndexOfAny('-', '+');
        if (cut >= 0) span = span[..cut];

        var parts = span.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries);
        var result = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            // Keep only leading digits of each component.
            var p = parts[i];
            var end = 0;
            while (end < p.Length && char.IsDigit(p[end])) end++;
            result[i] = end > 0 && int.TryParse(p.AsSpan(0, end), out var n) ? n : 0;
        }
        return result;
    }
}
