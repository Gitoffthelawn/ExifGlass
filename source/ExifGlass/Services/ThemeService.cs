using Avalonia;
using Avalonia.Styling;
using ExifGlass.Core.Models;

namespace ExifGlass.Services;

/// <summary>Applies the configured <see cref="ThemeMode"/> to the running application.</summary>
public interface IThemeService
{
    void Apply(ThemeMode mode);
}

/// <summary>
/// Maps <see cref="ThemeMode"/> onto Avalonia's <see cref="ThemeVariant"/>. <c>Default</c>
/// follows the operating-system theme.
/// </summary>
public sealed class ThemeService : IThemeService
{
    public void Apply(ThemeMode mode)
    {
        if (Application.Current is not { } app) return;

        app.RequestedThemeVariant = mode switch
        {
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }
}
