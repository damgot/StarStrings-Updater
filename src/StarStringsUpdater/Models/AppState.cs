namespace StarStringsUpdater.Models;

/// <summary>
/// Root of the state persisted next to the executable (state.json).
/// </summary>
public sealed class AppState
{
    public string? RootPath { get; set; }

    public Dictionary<string, ChannelState> Channels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
