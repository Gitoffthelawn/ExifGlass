using ExifGlass.Core.Models;

namespace ExifGlass.Core.Helpers;

/// <summary>
/// Reflection-free command-line parsing. Produces a <see cref="StartupOptions"/>
/// from the raw argument vector.
/// </summary>
/// <remarks>
/// Recognized forms:
/// <list type="bullet">
/// <item><c>--pipe</c> or <c>--pipe=&lt;name&gt;</c> — run in ImageGlass integrated mode.</item>
/// <item><c>/Key=Value</c> — a config override.</item>
/// <item>the first bare token — the file to open (an <c>exifglass:</c> scheme is stripped and URL-decoded).</item>
/// </list>
/// </remarks>
public static class CommandLine
{
    private const string PipeFlag = "--pipe";
    private const string SchemePrefix = "exifglass:";

    /// <summary>Parses the process argument vector into structured startup options.</summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var mode = AppMode.Standalone;
        string? initialFile = null;

        foreach (var raw in args)
        {
            if (string.IsNullOrEmpty(raw)) continue;

            // ImageGlass pipe flag: "--pipe" or "--pipe=<name>".
            if (raw.Equals(PipeFlag, StringComparison.OrdinalIgnoreCase)
                || raw.StartsWith(PipeFlag + "=", StringComparison.OrdinalIgnoreCase))
            {
                mode = AppMode.ImageGlass;
                continue;
            }

            // Config override: "/Key=Value".
            if (raw.StartsWith('/'))
            {
                var body = raw[1..];
                var eq = body.IndexOf('=');
                if (eq > 0)
                {
                    var key = body[..eq].Trim();
                    var value = body[(eq + 1)..];
                    if (key.Length > 0) overrides[key] = value;
                }
                continue;
            }

            // Any other flag is ignored here.
            if (raw.StartsWith('-')) continue;

            // First bare token is the file to open.
            initialFile ??= NormalizeFileArgument(raw);
        }

        return new StartupOptions(mode, initialFile, overrides);
    }

    /// <summary>
    /// Strips an optional <c>exifglass:</c> scheme and URL-decodes a file argument.
    /// </summary>
    public static string NormalizeFileArgument(string arg)
    {
        var value = arg.Trim();

        if (value.StartsWith(SchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[SchemePrefix.Length..];
            // Tolerate authority-style "exifglass://".
            value = value.TrimStart('/');
            value = Uri.UnescapeDataString(value);
        }

        return value;
    }
}
