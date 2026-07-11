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
/// Color theme mode for the application window.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Follow the operating-system theme.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Force the dark variant.
    /// </summary>
    Dark = 1,

    /// <summary>
    /// Force the light variant.
    /// </summary>
    Light = 2,
}

/// <summary>
/// How the application was launched.
/// </summary>
public enum AppMode
{
    /// <summary>
    /// Launched directly by the user (CLI arg, drag-drop, or file picker).
    /// </summary>
    Standalone = 0,

    /// <summary>
    /// Launched by ImageGlass 10 as an integrated tool (SDK pipe connection).
    /// </summary>
    ImageGlass = 1,

    /// <summary>
    /// Launched by ImageGlass 9 as an external tool (ImageGlass.Tools pipe connection).
    /// </summary>
    ImageGlass9 = 2,
}

/// <summary>
/// Supported metadata export formats.
/// </summary>
public enum ExportFileType
{
    Text = 0,
    Csv = 1,
    Json = 2,
}
