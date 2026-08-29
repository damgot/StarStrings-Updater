using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarStringsUpdater.Models;

namespace StarStringsUpdater.ViewModels;

public sealed partial class ChannelViewModel : ObservableObject
{
    private readonly MainWindowViewModel _parent;

    public ChannelViewModel(MainWindowViewModel parent, string name, string path, ReleaseTrack track)
    {
        _parent = parent;
        Name = name;
        Path = path;
        Track = track;
    }

    public string Name { get; }

    public string Path { get; }

    public ReleaseTrack Track { get; }

    [ObservableProperty]
    private ChannelStatus _status = ChannelStatus.Unknown;

    [ObservableProperty]
    private string? _installedVersionLabel;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isInstalled;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public bool CanUpdate => !IsBusy && Status is ChannelStatus.UpdateAvailable or ChannelStatus.NotInstalled or ChannelStatus.Error;

    public bool CanUninstall => !IsBusy && IsInstalled;

    partial void OnStatusChanged(ChannelStatus value)
    {
        OnPropertyChanged(nameof(CanUpdate));
        UpdateCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanUpdate));
        OnPropertyChanged(nameof(CanUninstall));
        UpdateCommand.NotifyCanExecuteChanged();
        UninstallCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(CanUninstall));
        UninstallCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private async Task UpdateAsync()
    {
        IsBusy = true;
        Status = ChannelStatus.Updating;
        ErrorMessage = null;
        try
        {
            await _parent.ApplyUpdateToChannelAsync(this);
        }
        catch (Exception ex)
        {
            Status = ChannelStatus.Error;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    private async Task UninstallAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _parent.UninstallChannelAsync(this);
        }
        catch (Exception ex)
        {
            Status = ChannelStatus.Error;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
