using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace ExifGlass.Views;

/// <summary>
/// Base window with system-integrated translucency. It requests Mica, then Acrylic, then a
/// solid fallback. When the platform can actually honor a translucent level (Windows 11) the
/// background is transparent so the effect shows through; otherwise (Linux, older Windows) a
/// solid, theme-aware background is painted — so no per-OS <c>#if</c> is needed.
/// </summary>
public class StyledWindow : Window
{
    public StyledWindow()
    {
        TransparencyLevelHint =
        [
            WindowTransparencyLevel.Mica,
            WindowTransparencyLevel.AcrylicBlur,
            WindowTransparencyLevel.None,
        ];

        ActualThemeVariantChanged += (_, _) => UpdateBackground();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UpdateBackground();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualTransparencyLevelProperty)
        {
            UpdateBackground();
        }
    }

    private void UpdateBackground()
    {
        if (ActualTransparencyLevel != WindowTransparencyLevel.None)
        {
            // A translucent level is active — let it show through.
            Background = Brushes.Transparent;
            return;
        }

        // Solid, theme-aware fallback matching the Fluent light/dark surface colors.
        var dark = ActualThemeVariant == ThemeVariant.Dark;
        Background = new SolidColorBrush(dark ? Color.FromRgb(0x20, 0x20, 0x20) : Color.FromRgb(0xF3, 0xF3, 0xF3));
    }
}
