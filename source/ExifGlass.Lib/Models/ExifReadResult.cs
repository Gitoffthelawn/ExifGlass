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
namespace ExifGlass.Core.Models;

/// <summary>
/// Outcome of a single metadata read.
/// </summary>
/// <param name="Tags">The parsed rows (empty when the read failed).</param>
/// <param name="CommandPreview">The command line that produced this result, for display.</param>
/// <param name="Success"><c>true</c> when metadata was read successfully.</param>
/// <param name="ErrorMessage">A friendly, actionable message when <paramref name="Success"/> is <c>false</c>.</param>
public sealed record ExifReadResult(
    IReadOnlyList<ExifTagItem> Tags,
    string CommandPreview,
    bool Success,
    string? ErrorMessage)
{
    /// <summary>
    /// An empty, successful result (e.g. after a "clear" request).
    /// </summary>
    public static ExifReadResult Empty(string commandPreview = "")
        => new([], commandPreview, true, null);

    /// <summary>
    /// A failed result carrying an error message.
    /// </summary>
    public static ExifReadResult Failure(string errorMessage, string commandPreview = "")
        => new([], commandPreview, false, errorMessage);
}
