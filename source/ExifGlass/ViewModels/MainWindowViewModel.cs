using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExifGlass.Core.Models;
using ExifGlass.Core.Services;
using ExifGlass.Helpers;
using ExifGlass.Integration;
using ExifGlass.Services;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace ExifGlass.ViewModels;

/// <summary>
/// Drives the main window: owns the bound metadata collection and its grouped view,
/// the footer command preview, error state, selection/column state, and the commands
/// behind the footer buttons, keyboard accelerators, and context menu.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IExifToolService _exifTool;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;

    // CTS swap: each load cancels the previous one so a newer file always wins.
    private CancellationTokenSource? _cts;

    // Re-entrancy guard for the "keep at least one column visible" rule.
    private bool _revertingColumn;

    /// <summary>
    /// Raised when the user asks to quit (Menu → Exit / Esc).
    /// </summary>
    public event Action? ExitRequested;

    /// <summary>
    /// Live rows; the grouped view observes this collection.
    /// </summary>
    public ObservableCollection<ExifTagItem> Items { get; } = [];

    /// <summary>
    /// Grouped, reflection-free projection bound by the grid.
    /// </summary>
    public DataGridCollectionView GroupedItems { get; }

    /// <summary>
    /// Tag of the grid's current column ("Index" / "TagId" / "TagName" / "Value"), mirrored
    /// from the view so <see cref="CopyCellCommand"/> copies the right cell. Defaults to the value column.
    /// </summary>
    public string CurrentColumnKey { get; set; } = "Value";

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyCellCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtractBinaryCommand))]
    private ExifTagItem? _selectedTag;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportTextCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCsvCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    private bool _hasItems;

    // Column visibility, seeded from config; code-behind mirrors these onto the columns.
    [ObservableProperty] private bool _showIndex = true;
    [ObservableProperty] private bool _showTagId = true;
    [ObservableProperty] private bool _showTagName = true;
    [ObservableProperty] private bool _showValue = true;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public MainWindowViewModel(IExifToolService exifTool, ISettingsService settings, IDialogService dialogs)
    {
        _exifTool = exifTool;
        _settings = settings;
        _dialogs = dialogs;

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
        SelectedTag = null;

        if (string.IsNullOrEmpty(path))
        {
            Items.Clear();
            HasItems = false;
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
                HasItems = Items.Count > 0;
                Title = $"{Path.GetFileName(path)} – ExifGlass";
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

    // Commands
    #region Commands

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (await _dialogs.PickImageFileAsync() is { } path && !string.IsNullOrEmpty(path))
        {
            await LoadFileAsync(path);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopy))]
    private Task CopyCellAsync()
    {
        if (SelectedTag is not { } tag) return Task.CompletedTask;

        var value = CurrentColumnKey switch
        {
            "Index" => tag.Index.ToString(),
            "TagId" => tag.TagId,
            "TagName" => tag.TagName,
            _ => tag.TagValue,
        };
        return _dialogs.CopyTextAsync(value);
    }

    private bool CanCopy => SelectedTag is not null;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private Task ExportTextAsync() => ExportAsync(ExportFileType.Text);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private Task ExportCsvAsync() => ExportAsync(ExportFileType.Csv);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private Task ExportJsonAsync() => ExportAsync(ExportFileType.Json);

    private bool CanExport => HasItems;

    [RelayCommand(CanExecute = nameof(CanExtract))]
    private async Task ExtractBinaryAsync()
    {
        if (SelectedTag is not { } tag || string.IsNullOrEmpty(CurrentFilePath)) return;

        var tagNoSpace = tag.TagName.Replace(" ", "");
        var baseName = Path.GetFileNameWithoutExtension(CurrentFilePath);
        var destination = await _dialogs.PickBinaryDestinationAsync($"{baseName}_{tagNoSpace}.bin");
        if (string.IsNullOrEmpty(destination)) return;

        var error = await _exifTool.ExtractBinaryTagAsync(CurrentFilePath, tag.TagName, destination);
        if (error is not null)
        {
            await _dialogs.ShowMessageAsync("Extraction failed", error);
        }
    }

    private bool CanExtract => SelectedTag?.CanExtractBinary == true;

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        if (await _dialogs.ShowSettingsDialogAsync())
        {
            ShowCommandPreview = _settings.Config.ShowCommandPreview;
            if (!string.IsNullOrEmpty(CurrentFilePath))
            {
                await LoadFileAsync(CurrentFilePath);
            }
        }
    }

    [RelayCommand]
    private Task CheckForUpdateAsync() => _dialogs.OpenUrlAsync(AppInfo.ReleasesUrl);

    [RelayCommand]
    private Task OpenAboutAsync() => _dialogs.ShowAboutDialogAsync();

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke();

    [RelayCommand]
    private void DismissError() => ErrorMessage = null;

    #endregion

    private async Task ExportAsync(ExportFileType type)
    {
        if (!HasItems) return;

        var baseName = string.IsNullOrEmpty(CurrentFilePath)
            ? "metadata"
            : Path.GetFileNameWithoutExtension(CurrentFilePath);

        // Snapshot the rows so a concurrent reload can't mutate the export mid-save.
        await _dialogs.ExportAsync(type, [.. Items], baseName);
    }


    // Window-state persistence: the view feeds bounds here; the config is saved on close.
    #region Window state

    /// <summary>
    /// Whether the window should open maximized (restored from config).
    /// </summary>
    public bool RestoreMaximized => _settings.Config.Window.Maximized;

    /// <summary>
    /// Records the window's normal-state bounds (ignored while maximized/minimized).
    /// </summary>
    public void UpdateNormalBounds(int x, int y, int width, int height)
    {
        var w = _settings.Config.Window;
        w.X = x;
        w.Y = y;
        w.Width = width;
        w.Height = height;
    }

    /// <summary>
    /// Persists the final window state to disk on close.
    /// </summary>
    public void SaveOnClose(bool maximized)
    {
        _settings.Config.Window.Maximized = maximized;
        try
        {
            _settings.Save();
        }
        catch
        {
            // A failed save must not block shutdown.
        }
    }

    #endregion


    // Keep at least one column visible; mirror each change into the config for persistence.
    #region Column visibility guards

    partial void OnShowIndexChanged(bool value)
    {
        _settings.Config.ShowIndex = value;
        RevertIfNoColumnsVisible(() => ShowIndex = true);
    }

    partial void OnShowTagIdChanged(bool value)
    {
        _settings.Config.ShowTagId = value;
        RevertIfNoColumnsVisible(() => ShowTagId = true);
    }

    partial void OnShowTagNameChanged(bool value)
    {
        _settings.Config.ShowTagName = value;
        RevertIfNoColumnsVisible(() => ShowTagName = true);
    }

    partial void OnShowValueChanged(bool value)
    {
        _settings.Config.ShowValue = value;
        RevertIfNoColumnsVisible(() => ShowValue = true);
    }

    private void RevertIfNoColumnsVisible(Action restore)
    {
        if (_revertingColumn) return;
        if (ShowIndex || ShowTagId || ShowTagName || ShowValue) return;

        _revertingColumn = true;
        restore();
        _revertingColumn = false;
    }

    #endregion


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
