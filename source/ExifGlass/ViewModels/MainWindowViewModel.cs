using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExifGlass.Core.Models;
using ExifGlass.Core.Services;
using ExifGlass.Integration;

namespace ExifGlass.ViewModels;

/// <summary>
/// Drives the main window: owns the bound metadata collection and its grouped view,
/// the footer command preview, error state, and the shared file-load pipeline.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IExifToolService _exifTool;
    private readonly ISettingsService _settings;

    // CTS swap: each load cancels the previous one so a newer file always wins.
    private CancellationTokenSource? _cts;

    /// <summary>Live rows; the grouped view observes this collection.</summary>
    public ObservableCollection<ExifTagItem> Items { get; } = [];

    /// <summary>Grouped, reflection-free projection bound by the grid.</summary>
    public DataGridCollectionView GroupedItems { get; }

    [ObservableProperty]
    private string _title = "ExifGlass";

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private string _commandPreview = "";

    [ObservableProperty]
    private bool _showCommandPreview = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    // Column visibility, seeded from config; code-behind mirrors these onto the columns.
    [ObservableProperty] private bool _showIndex = true;
    [ObservableProperty] private bool _showTagId = true;
    [ObservableProperty] private bool _showTagName = true;
    [ObservableProperty] private bool _showValue = true;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public MainWindowViewModel(IExifToolService exifTool, ISettingsService settings)
    {
        _exifTool = exifTool;
        _settings = settings;

        GroupedItems = BuildGroupedView(Items);

        var cfg = settings.Config;
        ShowCommandPreview = cfg.ShowCommandPreview;
        ShowIndex = cfg.ShowIndex;
        ShowTagId = cfg.ShowTagId;
        ShowTagName = cfg.ShowTagName;
        ShowValue = cfg.ShowValue;
    }

    /// <summary>
    /// Shared entry point for every file source (CLI, drag-drop, picker, ImageGlass pipe).
    /// Cancels any in-flight read; a superseded read is silently discarded.
    /// </summary>
    public async Task LoadFileAsync(string? path)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;

        CurrentFilePath = path;

        if (string.IsNullOrEmpty(path))
        {
            Items.Clear();
            CommandPreview = "";
            ErrorMessage = null;
            Title = "ExifGlass";
            return;
        }

        // Show the command preview immediately, before the (possibly slow) read.
        CommandPreview = _exifTool.BuildCommandPreview(path);
        IsLoading = true;

        try
        {
            var result = await _exifTool.ReadAsync(path, token);
            if (token.IsCancellationRequested) return;

            CommandPreview = result.CommandPreview;

            if (result.Success)
            {
                ErrorMessage = null;
                Items.Clear();
                foreach (var tag in result.Tags)
                {
                    Items.Add(tag);
                }
                Title = $"{Path.GetFileName(path)} — ExifGlass";
            }
            else
            {
                // Keep the last good grid; surface the problem in a dismissible banner.
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer load — ignore.
        }
        finally
        {
            // Only clear the flag if this load is still the current one.
            if (ReferenceEquals(_cts, cts))
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private void DismissError() => ErrorMessage = null;

    // DataGridCollectionView is [RequiresUnreferencedCode] (IL2026) because it inspects
    // items via reflection/TypeDescriptor. ExifTagItem's public members are preserved with
    // [DynamicallyAccessedMembers], so this is safe; the reflection-free TagGroupDescription
    // avoids the path-based grouping that would otherwise be trimmed. IL3050 is never
    // suppressed — none of these APIs require dynamic code generation.
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "ExifTagItem public members are preserved via DynamicallyAccessedMembers.")]
    private static DataGridCollectionView BuildGroupedView(ObservableCollection<ExifTagItem> items)
    {
        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new TagGroupDescription());
        return view;
    }
}
