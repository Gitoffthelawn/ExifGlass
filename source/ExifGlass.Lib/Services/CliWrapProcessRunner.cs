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
using CliWrap;
using CliWrap.Buffered;
using System.Text;

namespace ExifGlass.Core.Services;

/// <summary>
/// <see cref="IProcessRunner"/> backed by CliWrap. Passes an argv array (no shell),
/// captures UTF-8 stdout/stderr, and never throws on a non-zero exit code — the caller
/// interprets the result.
/// </summary>
public sealed class CliWrapProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var result = await Cli.Wrap(executablePath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(Encoding.UTF8, cancellationToken)
            .ConfigureAwait(false);

        return new ProcessResult(result.ExitCode, result.StandardOutput, result.StandardError);
    }
}
