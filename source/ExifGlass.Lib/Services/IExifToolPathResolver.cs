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
namespace ExifGlass.Core.Services;

/// <summary>
/// Resolves the ExifTool executable to run, following the order:
/// explicit Settings override → bundled copy next to the app → bare name on PATH.
/// </summary>
public interface IExifToolPathResolver
{
    /// <summary>
    /// Returns the best executable path/name to invoke.
    /// </summary>
    /// <param name="explicitPath">An optional user-configured path (from Settings).</param>
    string Resolve(string? explicitPath);

    /// <summary>
    /// Full path to the copy bundled next to the application, if one exists.
    /// </summary>
    string? BundledPath { get; }
}
