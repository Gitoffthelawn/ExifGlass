using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ExifGlass.Core.Models;
using ExifGlass.ViewModels;

namespace ExifGlass.Views;

// The generated XAML populate method instantiates DataGrid and binds ItemsSource, both
// [RequiresUnreferencedCode] (IL2026): DataGrid inspects items via reflection. ExifTagItem's
// members are preserved with [DynamicallyAccessedMembers] and grouping is reflection-free
// (TagGroupDescription), so this is safe — confirmed by the AOT smoke test rendering grouped
// rows. Scoped to this type only (not a project-wide NoWarn). IL3050 is never suppressed.
[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "ExifTagItem members preserved; DataGrid usage verified under AOT publish.")]
public partial class MainWindow : StyledWindow
{
    private MainWindowViewModel? _boundVm;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        MetadataGrid.CellPointerPressed += OnCellPointerPressed;
        MetadataGrid.CurrentCellChanged += OnCurrentCellChanged;
        MetadataGrid.SelectionChanged += OnGridSelectionChanged;

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        SizeChanged += (_, _) => CaptureNormalBounds();
        PositionChanged += (_, _) => CaptureNormalBounds();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_boundVm is not null)
        {
            _boundVm.PropertyChanged -= OnViewModelPropertyChanged;
            _boundVm.ExitRequested -= Close;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _boundVm = vm;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.ExitRequested += Close;
            SyncColumnVisibility(vm);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _boundVm?.SaveOnClose(WindowState == WindowState.Maximized);
    }

    // Column visibility: DataGridColumn is not in the visual tree and does not inherit
    // DataContext, so it is mirrored here in code-behind (accepted exception).
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
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

    // Track the right/left-clicked cell so Copy targets the correct column and the context
    // menu reflects the right-clicked row. Selecting the row raises SelectionChanged, which
    // flows the selection to the view model. Selection is driven here in code-behind (rather
    // than an x:Bind to DataGrid.SelectedItem) so no compiled dynamic setter references the
    // trim-unfriendly SelectedItemProperty — keeping our code IL2026-clean.
    private void OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (e.Column?.Tag is string key) vm.CurrentColumnKey = key;
        if (e.Row?.DataContext is ExifTagItem item) MetadataGrid.SelectedItem = item;
    }

    private void OnCurrentCellChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && MetadataGrid.CurrentColumn?.Tag is string key)
        {
            vm.CurrentColumnKey = key;
        }
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SelectedTag = MetadataGrid.SelectedItem as ExifTagItem;
        }
    }

    // Drag-drop: accept exactly one non-directory file.
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = TryGetSingleFile(e) is not null
            ? DragDropEffects.Copy | DragDropEffects.Link
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (TryGetSingleFile(e) is { } path && DataContext is MainWindowViewModel vm)
        {
            _ = vm.LoadFileAsync(path);
        }
    }

    private static string? TryGetSingleFile(DragEventArgs e)
    {
        // Avalonia 12 exposes dragged content through IDataTransfer.
        if (e.DataTransfer?.TryGetFiles() is not { } files) return null;

        string? single = null;
        foreach (var item in files)
        {
            if (item.TryGetLocalPath() is not { } path) return null;
            if (Directory.Exists(path)) return null;   // folders are not accepted
            if (single is not null) return null;        // more than one file -> ignore
            single = path;
        }
        return single;
    }

    private void CaptureNormalBounds()
    {
        if (WindowState != WindowState.Normal || _boundVm is null) return;
        if (double.IsNaN(Width) || double.IsNaN(Height)) return;

        _boundVm.UpdateNormalBounds(Position.X, Position.Y, (int)Width, (int)Height);
    }
}
