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
        // The bundled ExifTool is the upstream Perl distribution (the `exiftool` script + lib/),
        // launched on the system Perl. A publish produced on a non-Unix host can't carry the Unix
        // executable bit, so set it here before anything tries to run the script.
        EnsureBundledExifToolExecutable();

        // No SourceHostFactory override: this head serves only standalone launches and the
        // ImageGlass 10 SDK tool (--pipe), both built into the library. ImageGlass 9 is Windows-only
        // and stays in the Windows head.
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
    /// Gives the bundled ExifTool script the Unix executable bit so it can be launched directly.
    /// Best-effort: if the script is absent, already executable, or on a read-only mount this
    /// quietly does nothing, and ExifTool resolution falls back to a PATH-installed copy.
    /// </summary>
    private static void EnsureBundledExifToolExecutable()
    {
        // Guarded with OperatingSystem.IsLinux() (not a wrapper) so the platform analyzer accepts
        // the File.*UnixFileMode calls, which are unsupported on Windows.
        if (!OperatingSystem.IsLinux()) return;

        try
        {
            var script = Path.Combine(AppContext.BaseDirectory, "exiftool");
            if (!File.Exists(script)) return;

            const UnixFileMode executable = UnixFileMode.UserExecute
                | UnixFileMode.GroupExecute
                | UnixFileMode.OtherExecute;

            var mode = File.GetUnixFileMode(script);
            if ((mode & executable) != executable)
            {
                File.SetUnixFileMode(script, mode | executable);
            }
        }
        catch
        {
            // Best-effort: leave resolution to fall back to a PATH-installed ExifTool.
        }
    }
}
