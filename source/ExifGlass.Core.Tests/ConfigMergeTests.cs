using System.Text.Json;
using ExifGlass.Core.Models;
using ExifGlass.Core.Services;
using Xunit;

namespace ExifGlass.Core.Tests;

public class ConfigMergeTests
{
    [Fact]
    public void ApplyOverrides_MapsKnownKeysWithoutReflection()
    {
        var svc = new SettingsService();
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Theme"] = "Dark",
            ["AlwaysOnTop"] = "true",
            ["ExifToolPath"] = @"C:\tools\exiftool.exe",
            ["ExifToolArguments"] = "-a -u",
            ["ShowCommandPreview"] = "false",
            ["ShowIndex"] = "0",
            ["CheckForUpdates"] = "no",
            ["WindowWidth"] = "1024",
            ["WindowHeight"] = "768",
        };

        svc.ApplyOverrides(overrides);

        Assert.Equal(ThemeMode.Dark, svc.Config.Theme);
        Assert.True(svc.Config.AlwaysOnTop);
        Assert.Equal(@"C:\tools\exiftool.exe", svc.Config.ExifToolPath);
        Assert.Equal("-a -u", svc.Config.ExifToolArguments);
        Assert.False(svc.Config.ShowCommandPreview);
        Assert.False(svc.Config.ShowIndex);
        Assert.False(svc.Config.CheckForUpdates);
        Assert.Equal(1024, svc.Config.Window.Width);
        Assert.Equal(768, svc.Config.Window.Height);
    }

    [Fact]
    public void ApplyOverrides_IgnoresUnknownAndMalformedValues()
    {
        var svc = new SettingsService();
        var original = svc.Config.Theme;

        svc.ApplyOverrides(new Dictionary<string, string>
        {
            ["Unknown"] = "whatever",
            ["Theme"] = "NotATheme",   // invalid -> unchanged
            ["WindowWidth"] = "abc",   // invalid -> unchanged
        });

        Assert.Equal(original, svc.Config.Theme);
        Assert.Equal(600, svc.Config.Window.Width); // default preserved
    }

    [Fact]
    public void ApplyOverrides_LayersOnTopOfLoadedValues()
    {
        // Simulate defaults -> file -> CLI: only the overridden key changes.
        var svc = new SettingsService();
        svc.Config.Theme = ThemeMode.Light;      // as if loaded from file
        svc.Config.AlwaysOnTop = true;           // as if loaded from file

        svc.ApplyOverrides(new Dictionary<string, string> { ["Theme"] = "Dark" });

        Assert.Equal(ThemeMode.Dark, svc.Config.Theme);   // CLI wins
        Assert.True(svc.Config.AlwaysOnTop);              // untouched value survives
    }

    [Fact]
    public void AppConfig_RoundTripsThroughSourceGenJson_WithStringEnums()
    {
        var config = new AppConfig
        {
            Theme = ThemeMode.Dark,
            AlwaysOnTop = true,
            ExifToolArguments = "-a",
            Window = new WindowBounds { X = 10, Y = 20, Width = 800, Height = 900, Maximized = true },
        };

        var json = JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig);

        // Enum serialized as string, not the integer.
        Assert.Contains("\"Dark\"", json);
        Assert.DoesNotContain("\"Theme\": 1", json);

        var restored = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig)!;
        Assert.Equal(ThemeMode.Dark, restored.Theme);
        Assert.True(restored.AlwaysOnTop);
        Assert.Equal(800, restored.Window.Width);
        Assert.True(restored.Window.Maximized);
    }

    [Fact]
    public void AppConfig_DeserializePartialJson_KeepsDefaultsForMissingKeys()
    {
        // A sparse file should only override present keys; the rest fall back to defaults.
        const string sparse = "{ \"Theme\": \"Light\" }";

        var config = JsonSerializer.Deserialize(sparse, AppJsonContext.Default.AppConfig)!;

        Assert.Equal(ThemeMode.Light, config.Theme);
        Assert.True(config.ShowValue);            // default
        Assert.True(config.CheckForUpdates);      // default
        Assert.Equal(600, config.Window.Width);   // default
    }
}
