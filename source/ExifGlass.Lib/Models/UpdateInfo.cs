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
/// The release feed document (<c>update.json</c>), deserialized via source-generated JSON.
/// Mirrors the published schema exactly so the reflection-free deserializer can bind it.
/// </summary>
public sealed record UpdateInfo
{
    /// <summary>
    /// Feed schema version; lets the client reject an incompatible future format.
    /// </summary>
    public int ApiVersion { get; init; }

    /// <summary>
    /// Latest released version (dotted), compared against the running build.
    /// </summary>
    public string Version { get; init; } = "";

    /// <summary>
    /// Short release title shown in the update window.
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// Human-readable release notes (may contain newlines).
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Optional link to the full changelog.
    /// </summary>
    public string ChangelogUrl { get; init; } = "";

    /// <summary>
    /// ISO-8601 publication timestamp.
    /// </summary>
    public string PublishedDate { get; init; } = "";

    /// <summary>
    /// Page opened by the Download button (no self-replacing binary).
    /// </summary>
    public string DownloadUrl { get; init; } = "";
}
