namespace ExifGlass.Core.Services;

/// <summary>Buffered result of running an external process.</summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="StandardOutput">Captured stdout.</param>
/// <param name="StandardError">Captured stderr.</param>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Thin seam over external-process execution so services can be unit-tested with
/// canned output — no ExifTool installation required.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs <paramref name="executablePath"/> with an argv array (no shell, no injection)
    /// and returns its buffered output.
    /// </summary>
    Task<ProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
