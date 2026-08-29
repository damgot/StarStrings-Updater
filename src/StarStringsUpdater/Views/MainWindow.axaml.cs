using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using StarStringsUpdater.ViewModels;

namespace StarStringsUpdater.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select your StarCitizen folder",
            AllowMultiple = false,
        });

        var folder = folders.FirstOrDefault();
        if (folder?.TryGetLocalPath() is { } path && DataContext is MainWindowViewModel vm)
        {
            await vm.SetRootPathAsync(path);
        }
    }
}
