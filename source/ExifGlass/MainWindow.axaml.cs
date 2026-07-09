using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using ExifGlass.ViewModels;

namespace ExifGlass;

// The generated XAML populate method instantiates DataGrid and binds ItemsSource, both
// [RequiresUnreferencedCode] (IL2026): DataGrid inspects items via reflection. ExifTagItem's
// members are preserved with [DynamicallyAccessedMembers] and grouping is reflection-free
// (TagGroupDescription), so this is safe — confirmed by the AOT smoke test rendering grouped
// rows. Scoped to this type only (not a project-wide NoWarn). IL3050 is never suppressed.
[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "ExifTagItem members preserved; DataGrid usage verified under AOT publish.")]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            SyncColumnVisibility(vm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // DataGridColumn is not in the visual tree and does not inherit DataContext,
        // so column visibility is mirrored here in code-behind (accepted exception).
        if (sender is MainWindowViewModel vm && e.PropertyName is
            nameof(MainWindowViewModel.ShowIndex) or
            nameof(MainWindowViewModel.ShowTagId) or
            nameof(MainWindowViewModel.ShowTagName) or
            nameof(MainWindowViewModel.ShowValue))
        {
            SyncColumnVisibility(vm);
        }
    }

    private void SyncColumnVisibility(MainWindowViewModel vm)
    {
        // Columns are addressed by position (DataGridColumn is not a named tree element).
        var columns = MetadataGrid.Columns;
        columns[0].IsVisible = vm.ShowIndex;    // #
        columns[1].IsVisible = vm.ShowTagId;    // Tag ID
        columns[2].IsVisible = vm.ShowTagName;  // Tag Name
        columns[3].IsVisible = vm.ShowValue;    // Value
    }
}
