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
using ExifGlass.Core.Helpers;
using ExifGlass.Core.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ExifGlass.Core.Services;

/// <summary>
/// Notify-only update checker. GETs the release feed (no-cache), deserializes it with the
/// source-generated JSON context (AOT-safe), compares versions with <see cref="VersionComparer"/>,
/// and — for automatic checks — throttles to once every <see cref="ThrottleInterval"/> and stamps
/// <see cref="AppConfig.LastUpdateCheck"/>.
/// </summary>
public sealed class UpdateService(HttpClient http, ISettingsService settings) : IUpdateService
{
    /// <summary>
    /// The published release feed (raw GitHub content).
    /// </summary>
    public const string FeedUrl = "https://raw.githubusercontent.com/d2phap/ExifGlass/main/update.json";

    /// <summary>
    /// Automatic checks run at most once per this interval.
    /// </summary>
    public static readonly TimeSpan ThrottleInterval = TimeSpan.FromDays(5);

    public async Task<UpdateCheckResult> CheckAsync(
        string currentVersion, bool force, CancellationToken cancellationToken = default)
    {
        var config = settings.Config;

        if (!force)
        {
            if (!config.CheckForUpdates) return UpdateCheckResult.Skipped();
            if (!UpdateThrottle.ShouldCheck(config.LastUpdateCheck, DateTimeOffset.UtcNow, ThrottleInterval))
            {
                return UpdateCheckResult.Skipped();
            }
        }

        UpdateInfo? info;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, FeedUrl);
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failure($"The update server returned {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            info = JsonSerializer.Deserialize(json, AppJsonContext.Default.UpdateInfo);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failure(FriendlyError(ex));
        }

        // A check ran; record the time so the throttle is honoured next launch.
        StampNow();

        if (info is null || string.IsNullOrWhiteSpace(info.Version))
        {
            return UpdateCheckResult.Failure("The update information could not be read.");
        }

        return VersionComparer.IsNewer(info.Version, currentVersion)
            ? UpdateCheckResult.Available(info)
            : UpdateCheckResult.UpToDate();
    }

    private void StampNow()
    {
        settings.Config.LastUpdateCheck = DateTimeOffset.UtcNow.ToString("O");
        try
        {
            settings.Save();
        }
        catch
        {
            // A failed stamp must not turn a successful check into an error.
        }
    }

    private static string FriendlyError(Exception ex) => ex switch
    {
        HttpRequestException => "Could not reach the update server. Check your internet connection.",
        JsonException => "The update information could not be read.",
        _ => ex.Message,
    };
}
