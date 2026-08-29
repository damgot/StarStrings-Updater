namespace StarStringsUpdater.Models;

/// <summary>
/// The StarStrings repo publishes two independent rolling releases: a "LIVE" one (tag
/// "latest") and a "PTU" one (tag "latest-ptu"). Each supported StarCitizen channel is
/// permanently tied to one of these two tracks; there is no per-channel release.
/// </summary>
public enum ReleaseTrack
{
    Live,
    Ptu,
}
