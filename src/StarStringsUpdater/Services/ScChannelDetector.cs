using StarStringsUpdater.Models;

namespace StarStringsUpdater.Services;

/// <summary>
/// Detects the StarCitizen channels the app supports (LIVE, HOTFIX, PTU — nothing else, e.g.
/// no TECH-PREVIEW) under a root folder, by matching the subfolder name against the supported
/// list and checking it contains a "Data" folder.
/// </summary>
public static class ScChannelDetector
{
    private static readonly Dictionary<string, ReleaseTrack> SupportedChannels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LIVE"] = ReleaseTrack.Live,
        ["HOTFIX"] = ReleaseTrack.Live,
        ["PTU"] = ReleaseTrack.Ptu,
    };

    public static IReadOnlyList<ScChannel> DetectChannels(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return Array.Empty<ScChannel>();
        }

        var channels = new List<ScChannel>();
        foreach (var dir in Directory.EnumerateDirectories(rootPath))
        {
            var name = Path.GetFileName(dir);
            if (!SupportedChannels.TryGetValue(name, out var track))
            {
                continue;
            }

            var dataPath = Path.Combine(dir, "Data");
            if (Directory.Exists(dataPath))
            {
                channels.Add(new ScChannel(name, dir, track));
            }
        }

        return channels
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
