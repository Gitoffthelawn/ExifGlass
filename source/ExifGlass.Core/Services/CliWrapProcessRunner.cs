using System.Text;
using CliWrap;
using CliWrap.Buffered;

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
