using System.Reflection;

namespace ExifGlass.Helpers;

/// <summary>Static application identity used by the About window and (later) update checks.</summary>
public static class AppInfo
{
    /// <summary>The running assembly version as <c>Major.Minor.Build</c>.</summary>
    public static string Version { get; } = ResolveVersion();

    /// <summary>Project home / release page.</summary>
    public const string WebsiteUrl = "https://github.com/d2phap/ExifGlass";

    /// <summary>Release list, opened by "Check for update" until the update service lands.</summary>
    public const string ReleasesUrl = "https://github.com/d2phap/ExifGlass/releases";

    /// <summary>Microsoft Store product page (browser-safe form).</summary>
    public const string StoreUrl = "https://www.microsoft.com/store/productId/9MX8S9HZ57W8";

    private static string ResolveVersion()
    {
        var version = typeof(AppInfo).Assembly.GetName().Version;
        return version is null
            ? "1.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
