using CommunityToolkit.Mvvm.Input;

namespace ExifGlass.ViewModels;

/// <summary>
/// Backs a simple modal message dialog (heading + message + OK).
/// </summary>
public sealed partial class MessageBoxViewModel : ViewModelBase
{
    public event Action? CloseRequested;

    public string Heading { get; }
    public string Message { get; }

    public MessageBoxViewModel(string heading, string message)
    {
        Heading = heading;
        Message = message;
    }

    [RelayCommand]
    private void Ok() => CloseRequested?.Invoke();
}
