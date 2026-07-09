using CommunityToolkit.Mvvm.ComponentModel;

namespace ExifGlass.ViewModels;

/// <summary>
/// Base for all view models. <see cref="ObservableObject"/> provides
/// source-generated <c>INotifyPropertyChanged</c> support (AOT-safe).
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
