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
namespace ExifGlass.Core.Helpers;

/// <summary>
/// Single source of truth for the ExifTool argument vector, shared by the real
/// invocation and the footer command preview so the two can never drift.
/// </summary>
public static class ExifToolCommand
{
    /// <summary>
    /// Base flags. <c>-t -G -H</c> yield the tab-delimited, grouped, hex-id output the
    /// parser expects; <c>-fast -m -q</c> speed up reads and quiet warnings.
    /// </summary>
    public static readonly string[] BaseFlags = ["-fast", "-G", "-t", "-m", "-q", "-H"];

    /// <summary>Builds the full argv array for reading <paramref name="filePath"/>.</summary>
    public static IReadOnlyList<string> BuildArgs(string filePath, string? extraArguments)
    {
        var args = new List<string>(BaseFlags.Length + 6);
        args.AddRange(BaseFlags);

        if (!string.IsNullOrWhiteSpace(extraArguments))
        {
            args.AddRange(Tokenize(extraArguments));
        }

        // Default charset so non-ASCII values decode predictably.
        args.Add("-charset");
        args.Add("UTF8");

        args.Add(filePath);
        return args;
    }

    /// <summary>
    /// Builds a human-readable command line for display. Quoting is cosmetic only —
    /// the real run uses the argv array above and is not shell-parsed.
    /// </summary>
    public static string BuildPreview(string executable, string filePath, string? extraArguments)
    {
        var args = BuildArgs(filePath, extraArguments);
        var parts = new List<string>(args.Count + 1) { Quote(executable) };
        foreach (var a in args) parts.Add(Quote(a));
        return string.Join(' ', parts);
    }

    /// <summary>
    /// Splits a user-supplied argument string into tokens, honoring double quotes.
    /// </summary>
    public static IReadOnlyList<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens;
    }

    private static string Quote(string value) => value.Contains(' ') || value.Length == 0 ? $"\"{value}\"" : value;
}
