using System.Runtime.InteropServices;

namespace ExifGlass.Core.Helpers;

/// <summary>
/// Centralized platform capability checks so OS-specific branches read intentionally
/// and stay in one place (no scattered <c>#if</c> directives).
/// </summary>
public static class PlatformInfo
{
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsMacOS => OperatingSystem.IsMacOS();
    public static bool IsLinux => OperatingSystem.IsLinux();

    /// <summary>
    /// File name of the bundled ExifTool executable for the current OS.
    /// </summary>
    public static string ExifToolExecutableName => IsWindows ? "exiftool.exe" : "exiftool";

    /// <summary>
    /// Whether the Unicode/non-ANSI path workaround applies. ExifTool on Windows cannot
    /// open paths containing codepoints above the ANSI range, so the file must be copied
    /// to an ASCII temp path first.
    /// </summary>
    public static bool NeedsAnsiPathWorkaround(string path)
        => IsWindows && ContainsNonAnsi(path);

    private static bool ContainsNonAnsi(string value)
    {
        const int maxAnsi = 255;
        foreach (var c in value)
        {
            if (c > maxAnsi) return true;
        }
        return false;
    }

    /// <summary>
    /// Human-readable architecture string (e.g. <c>x64</c>, <c>arm64</c>).
    /// </summary>
    public static string Architecture => RuntimeInformation.ProcessArchitecture switch
    {
        System.Runtime.InteropServices.Architecture.X64 => "x64",
        System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
        System.Runtime.InteropServices.Architecture.X86 => "x86",
        var other => other.ToString().ToLowerInvariant(),
    };
}
