using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarStringsUpdater.Models;
using StarStringsUpdater.Services;

namespace StarStringsUpdater.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly GitHubReleaseService _releaseService = new();
    private readonly UpdateService _updateService;
    private readonly SettingsStore _settingsStore = new();

    private readonly AppState _state;
    private GitHubRelease? _liveRelease;
    private GitHubRelease? _ptuRelease;

    public MainWindowViewModel()
    {
        _updateService = new UpdateService(_releaseService);
        _state = _settingsStore.Load();
        RootPath = _state.RootPath;

        if (!string.IsNullOrWhiteSpace(RootPath))
        {
            RefreshChannelsFromDisk();
        }

        _ = CheckForUpdatesAsync();
    }

    public ObservableCollection<ChannelViewModel> Channels { get; } = new();

    [ObservableProperty]
    private string? _rootPath;

    [ObservableProperty]
    private string _liveReleaseLabel = "Not checked yet";

    [ObservableProperty]
    private string _ptuReleaseLabel = "Not checked yet";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    public bool HasChannels => Channels.Count > 0;

    /// <summary>
    /// Shown in the OS title bar/taskbar tooltip, e.g. "StarStrings Updater v1.0". The version
    /// is derived from the assembly version (itself driven by &lt;Version&gt; in
    /// StarStringsUpdater.csproj) so there's a single place to bump it.
    /// </summary>
    public string WindowTitle
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionLabel = version is null ? "" : $" v{version.Major}.{version.Minor}";
            return $"StarStrings Updater{versionLabel}";
        }
    }

    public async Task SetRootPathAsync(string path)
    {
        RootPath = path;
        _state.RootPath = path;
        _settingsStore.Save(_state);

        RefreshChannelsFromDisk();
        await CheckForUpdatesAsync();
    }

    private void RefreshChannelsFromDisk()
    {
        Channels.Clear();
        if (string.IsNullOrWhiteSpace(RootPath))
        {
            OnPropertyChanged(nameof(HasChannels));
            return;
        }

        foreach (var channel in ScChannelDetector.DetectChannels(RootPath))
        {
            var vm = new ChannelViewModel(this, channel.Name, channel.Path, channel.Track);
            if (_state.Channels.TryGetValue(channel.Name, out var saved))
            {
                vm.InstalledVersionLabel = saved.InstalledReleaseName;
                vm.IsInstalled = true;
            }
            Channels.Add(vm);
        }

        OnPropertyChanged(nameof(HasChannels));
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        StatusMessage = "";
        var errors = new List<string>();

        // Both tracks are always refreshed, even if no channel for that track (or none at
        // all) was detected yet — the header labels should reflect the latest GitHub releases
        // regardless of whether a StarCitizen folder has been configured.
        await RefreshTrackReleaseAsync(ReleaseTrack.Live, errors);
        await RefreshTrackReleaseAsync(ReleaseTrack.Ptu, errors);

        foreach (var channel in Channels)
        {
            UpdateChannelStatus(channel);
        }

        StatusMessage = errors.Count > 0
            ? $"Error while checking for updates: {string.Join(" / ", errors)}"
            : $"Last checked: {DateTime.Now:HH:mm:ss}";

        IsCheckingForUpdates = false;
    }

    private async Task RefreshTrackReleaseAsync(ReleaseTrack track, List<string> errors)
    {
        try
        {
            var release = await _releaseService.GetReleaseAsync(track);
            SetRelease(track, release);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }
    }

    private GitHubRelease? GetRelease(ReleaseTrack track) => track switch
    {
        ReleaseTrack.Live => _liveRelease,
        ReleaseTrack.Ptu => _ptuRelease,
        _ => null,
    };

    private void SetRelease(ReleaseTrack track, GitHubRelease release)
    {
        var label = release.Name ?? release.TagName;
        switch (track)
        {
            case ReleaseTrack.Live:
                _liveRelease = release;
                LiveReleaseLabel = label;
                break;
            case ReleaseTrack.Ptu:
                _ptuRelease = release;
                PtuReleaseLabel = label;
                break;
        }
    }

    private void UpdateChannelStatus(ChannelViewModel channel)
    {
        var release = GetRelease(channel.Track);
        if (release is null)
        {
            return;
        }

        if (!_state.Channels.TryGetValue(channel.Name, out var saved))
        {
            channel.Status = ChannelStatus.NotInstalled;
            channel.IsInstalled = false;
            return;
        }

        channel.InstalledVersionLabel = saved.InstalledReleaseName;
        channel.IsInstalled = true;
        channel.Status = saved.InstalledReleaseId == release.Id
            ? ChannelStatus.UpToDate
            : ChannelStatus.UpdateAvailable;
    }

    [RelayCommand]
    private async Task UpdateAllAsync()
    {
        foreach (var channel in Channels.Where(c => c.CanUpdate).ToList())
        {
            await channel.UpdateCommand.ExecuteAsync(null);
        }
    }

    public async Task ApplyUpdateToChannelAsync(ChannelViewModel channel)
    {
        var release = GetRelease(channel.Track);
        if (release is null)
        {
            release = await _releaseService.GetReleaseAsync(channel.Track);
            SetRelease(channel.Track, release);
        }

        var asset = release.FindZipAsset()
            ?? throw new InvalidOperationException($"No .zip archive found in the latest {channel.Track} release.");

        var extractedPath = await _updateService.EnsurePackageExtractedAsync(release, asset);
        var installedDataFiles = _updateService.ApplyToChannel(extractedPath, channel.Path);

        var newState = new ChannelState
        {
            InstalledReleaseId = release.Id,
            InstalledReleaseName = release.Name ?? release.TagName,
            InstalledAssetUpdatedAtUtc = asset.UpdatedAt,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            InstalledDataFiles = installedDataFiles.ToList(),
        };
        _state.Channels[channel.Name] = newState;
        _settingsStore.Save(_state);

        channel.InstalledVersionLabel = newState.InstalledReleaseName;
        channel.IsInstalled = true;
        channel.Status = ChannelStatus.UpToDate;
    }

    public Task UninstallChannelAsync(ChannelViewModel channel)
    {
        if (_state.Channels.TryGetValue(channel.Name, out var saved))
        {
            _updateService.RemoveFromChannel(channel.Path, saved.InstalledDataFiles);
            _state.Channels.Remove(channel.Name);
            _settingsStore.Save(_state);
        }

        channel.InstalledVersionLabel = null;
        channel.IsInstalled = false;
        channel.Status = ChannelStatus.NotInstalled;

        return Task.CompletedTask;
    }
}
