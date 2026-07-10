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
/// Outcome of an update check.
/// </summary>
/// <param name="Checked">Whether a network check actually ran (false when throttled/disabled).</param>
/// <param name="UpdateAvailable">Whether a strictly newer version is available.</param>
/// <param name="Info">The feed document when a check ran and parsed; otherwise <c>null</c>.</param>
/// <param name="ErrorMessage">A friendly message when the check failed; otherwise <c>null</c>.</param>
public sealed record UpdateCheckResult(
    bool Checked,
    bool UpdateAvailable,
    UpdateInfo? Info,
    string? ErrorMessage)
{
    /// <summary>
    /// No check ran (disabled by config or within the throttle window).
    /// </summary>
    public static UpdateCheckResult Skipped() => new(false, false, null, null);

    /// <summary>
    /// A check ran and the running build is current.
    /// </summary>
    public static UpdateCheckResult UpToDate() => new(true, false, null, null);

    /// <summary>
    /// A check ran and a newer release is available.
    /// </summary>
    public static UpdateCheckResult Available(UpdateInfo info) => new(true, true, info, null);

    /// <summary>
    /// A check ran but failed (network/parse); message is user-facing.
    /// </summary>
    public static UpdateCheckResult Failure(string message) => new(true, false, null, message);
}
