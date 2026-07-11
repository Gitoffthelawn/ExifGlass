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
/// Result of validating the resolved ExifTool executable.
/// </summary>
/// <param name="IsAvailable"><c>true</c> when ExifTool was found and responded.</param>
/// <param name="ResolvedPath">The path that was probed.</param>
/// <param name="Version">The reported ExifTool version, when available.</param>
/// <param name="ErrorMessage">A friendly message when <paramref name="IsAvailable"/> is <c>false</c>.</param>
public sealed record ExifToolStatus(
    bool IsAvailable,
    string ResolvedPath,
    string? Version,
    string? ErrorMessage);
