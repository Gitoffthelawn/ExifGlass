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
/// Persisted position, size, and state of the main window.
/// </summary>
public sealed class WindowBounds
{
    public int X { get; set; } = 200;
    public int Y { get; set; } = 200;
    public int Width { get; set; } = 600;
    public int Height { get; set; } = 800;

    /// <summary>
    /// <c>true</c> when the window was maximized at last save.
    /// </summary>
    public bool Maximized { get; set; }
}
