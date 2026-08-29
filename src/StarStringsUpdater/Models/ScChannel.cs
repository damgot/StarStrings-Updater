namespace StarStringsUpdater.Models;

/// <summary>
/// A supported StarCitizen channel detected on disk (LIVE, HOTFIX, or PTU), tied to the
/// StarStrings release track that must be used to update it.
/// </summary>
public sealed record ScChannel(string Name, string Path, ReleaseTrack Track);
