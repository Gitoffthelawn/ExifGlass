namespace ExifGlass.Core.Models;

/// <summary>
/// Outcome of a single metadata read.
/// </summary>
/// <param name="Tags">The parsed rows (empty when the read failed).</param>
/// <param name="CommandPreview">The command line that produced this result, for display.</param>
/// <param name="Success"><c>true</c> when metadata was read successfully.</param>
/// <param name="ErrorMessage">A friendly, actionable message when <paramref name="Success"/> is <c>false</c>.</param>
public sealed record ExifReadResult(
    IReadOnlyList<ExifTagItem> Tags,
    string CommandPreview,
    bool Success,
    string? ErrorMessage)
{
    /// <summary>An empty, successful result (e.g. after a "clear" request).</summary>
    public static ExifReadResult Empty(string commandPreview = "")
        => new([], commandPreview, true, null);

    /// <summary>A failed result carrying an error message.</summary>
    public static ExifReadResult Failure(string errorMessage, string commandPreview = "")
        => new([], commandPreview, false, errorMessage);
}
