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
using System.Net;
using ExifGlass.Core.Models;
using ExifGlass.Core.Services;
using Xunit;

namespace ExifGlass.Core.Tests;

public class UpdateServiceTests
{
    private const string Feed = """
        {
            "ApiVersion": 1,
            "Version": "1.10.0.0",
            "Title": "ExifGlass 1.10",
            "Description": "Notes here.",
            "ChangelogUrl": "https://example/changelog",
            "PublishedDate": "2025-10-31T14:24:26",
            "DownloadUrl": "https://example/download"
        }
        """;

    [Fact]
    public async Task CheckAsync_ReportsAvailable_WhenFeedIsNewer()
    {
        var (service, _) = Build(Feed);

        var result = await service.CheckAsync(currentVersion: "1.9.0.0", force: true);

        Assert.True(result.Checked);
        Assert.True(result.UpdateAvailable);
        Assert.NotNull(result.Info);
        Assert.Equal("1.10.0.0", result.Info!.Version);
        Assert.Equal("https://example/download", result.Info.DownloadUrl);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAsync_ReportsUpToDate_WhenRunningNewerOrEqual()
    {
        var (service, _) = Build(Feed);

        Assert.False((await service.CheckAsync("1.10.0.0", force: true)).UpdateAvailable);
        Assert.False((await service.CheckAsync("2.0.0", force: true)).UpdateAvailable);
    }

    [Fact]
    public async Task CheckAsync_StampsLastCheck_OnSuccessfulCheck()
    {
        var (service, settings) = Build(Feed);
        Assert.Equal("", settings.Config.LastUpdateCheck);

        await service.CheckAsync("1.0.0", force: true);

        Assert.NotEqual("", settings.Config.LastUpdateCheck);
    }

    [Fact]
    public async Task CheckAsync_Skips_WhenDisabledAndNotForced()
    {
        var (service, settings) = Build(Feed);
        settings.Config.CheckForUpdates = false;

        var result = await service.CheckAsync("1.0.0", force: false);

        Assert.False(result.Checked);
        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckAsync_Skips_WhenWithinThrottleWindow()
    {
        var (service, settings) = Build(Feed);
        settings.Config.CheckForUpdates = true;
        settings.Config.LastUpdateCheck = DateTimeOffset.UtcNow.ToString("O"); // just now

        var result = await service.CheckAsync("1.0.0", force: false);

        Assert.False(result.Checked);
    }

    [Fact]
    public async Task CheckAsync_Runs_WhenThrottleWindowElapsed()
    {
        var (service, settings) = Build(Feed);
        settings.Config.CheckForUpdates = true;
        settings.Config.LastUpdateCheck = DateTimeOffset.UtcNow.AddDays(-10).ToString("O");

        var result = await service.CheckAsync("1.0.0", force: false);

        Assert.True(result.Checked);
        Assert.True(result.UpdateAvailable);
    }

    [Fact]
    public async Task CheckAsync_ReturnsFailure_OnHttpError()
    {
        var (service, _) = Build("not found", HttpStatusCode.NotFound);

        var result = await service.CheckAsync("1.0.0", force: true);

        Assert.True(result.Checked);
        Assert.False(result.UpdateAvailable);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAsync_ReturnsFailure_OnMalformedJson()
    {
        var (service, _) = Build("{ this is not json ");

        var result = await service.CheckAsync("1.0.0", force: true);

        Assert.NotNull(result.ErrorMessage);
        Assert.False(result.UpdateAvailable);
    }

    private static (UpdateService Service, ISettingsService Settings) Build(
        string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var http = new HttpClient(new StubHandler(status, body));
        var settings = new InMemorySettings();
        return (new UpdateService(http, settings), settings);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private sealed class InMemorySettings : ISettingsService
    {
        public AppConfig Config { get; } = new();
        public void Load() { }
        public void Save() { }
        public void ApplyOverrides(IReadOnlyDictionary<string, string> overrides) { }
    }
}
