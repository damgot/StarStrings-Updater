namespace StarStringsUpdater.Models;

/// <summary>
/// StarStrings version currently installed for a given StarCitizen channel (LIVE, PTU, ...).
/// Persisted in the application's state file.
/// </summary>
public sealed class ChannelState
{
    public long InstalledReleaseId { get; set; }

    public string? InstalledReleaseName { get; set; }

    public DateTimeOffset InstalledAssetUpdatedAtUtc { get; set; }

    public DateTimeOffset LastUpdatedUtc { get; set; }

    /// <summary>
    /// Paths (relative to the channel's "Data" folder) copied during install, so uninstall
    /// can remove exactly those files and nothing else.
    /// </summary>
    public List<string> InstalledDataFiles { get; set; } = new();
}
