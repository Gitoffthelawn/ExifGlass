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
using ExifGlass.Core.Models;

namespace ExifGlass.Core.Services;

/// <summary>
/// Notify-only update checker: fetches the release feed and compares it to the running build.
/// Never downloads or replaces the binary — the UI opens the release page instead.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks for a newer release. When <paramref name="force"/> is <c>false</c> the check is
    /// gated by <see cref="AppConfig.CheckForUpdates"/> and the throttle window and may report
    /// <see cref="UpdateCheckResult.Skipped"/>; when <c>true</c> it always runs (e.g. the
    /// About dialog's button). Expected failures surface in the result, not as exceptions.
    /// </summary>
    Task<UpdateCheckResult> CheckAsync(string currentVersion, bool force, CancellationToken cancellationToken = default);
}
