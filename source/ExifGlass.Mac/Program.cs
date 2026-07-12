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
using Avalonia;

namespace ExifGlass;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Restore the bundled ExifTool script's Unix executable bit (lost on a non-Unix publish host).
        EnsureBundledExifToolExecutable();

        // Standalone + ImageGlass 10 (--pipe) only; no SourceHostFactory override.
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Gives the bundled ExifTool script the Unix executable bit. Best-effort: falls back to a
    /// PATH-installed copy if it can't.
    /// </summary>
    private static void EnsureBundledExifToolExecutable()
    {
        // IsMacOS() (not a wrapper) so the analyzer accepts the Windows-unsupported UnixFileMode calls.
        if (!OperatingSystem.IsMacOS()) return;

        try
        {
            var exiftool = Path.Combine(AppContext.BaseDirectory, "exiftool");
            if (!File.Exists(exiftool)) return;

            const UnixFileMode executable = UnixFileMode.UserExecute
                | UnixFileMode.GroupExecute
                | UnixFileMode.OtherExecute;

            var mode = File.GetUnixFileMode(exiftool);
            if ((mode & executable) != executable)
            {
                File.SetUnixFileMode(exiftool, mode | executable);
            }
        }
        catch
        {
            // Best-effort: leave resolution to fall back to a PATH-installed ExifTool.
        }
    }
}
