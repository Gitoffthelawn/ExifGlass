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
/// Holds the live <see cref="AppConfig"/> and persists it via source-generated JSON.
/// Registered as a singleton in the composition root.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// The current, mutable configuration.
    /// </summary>
    AppConfig Config { get; }

    /// <summary>
    /// Loads configuration from disk, tolerating a missing or corrupt file
    /// (falls back to defaults). Synchronous: the config is tiny and both callers
    /// (startup and close) need it done before proceeding.
    /// </summary>
    void Load();

    /// <summary>
    /// Writes the current configuration to disk (atomic temp-file swap). Synchronous
    /// so it is safe to call while blocking the UI thread on window close.
    /// </summary>
    void Save();

    /// <summary>
    /// Applies CLI <c>/Key=Value</c> overrides on top of the loaded configuration
    /// using an explicit, reflection-free mapping.
    /// </summary>
    void ApplyOverrides(IReadOnlyDictionary<string, string> overrides);
}
