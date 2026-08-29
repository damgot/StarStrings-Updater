using System.IO.Compression;
using StarStringsUpdater.Models;

namespace StarStringsUpdater.Services;

/// <summary>
/// Downloads/extracts the StarStrings zip and applies it to (or removes it from) a given
/// StarCitizen channel.
/// </summary>
public sealed class UpdateService
{
    private readonly GitHubReleaseService _releaseService;

    private string? _extractedPackagePath;
    private long _extractedReleaseId;

    public UpdateService(GitHubReleaseService releaseService)
    {
        _releaseService = releaseService;
    }

    /// <summary>
    /// Downloads and extracts the zip for the given release, reusing it if already done for
    /// this same release (useful when updating several channels at once).
    /// </summary>
    public async Task<string> EnsurePackageExtractedAsync(GitHubRelease release, GitHubReleaseAsset asset, CancellationToken ct = default)
    {
        if (_extractedPackagePath is not null && _extractedReleaseId == release.Id && Directory.Exists(_extractedPackagePath))
        {
            return _extractedPackagePath;
        }

        var workDir = Path.Combine(Path.GetTempPath(), "StarStringsUpdater", release.Id.ToString());
        if (Directory.Exists(workDir))
        {
            Directory.Delete(workDir, recursive: true);
        }
        Directory.CreateDirectory(workDir);

        var zipPath = Path.Combine(workDir, asset.Name);
        await _releaseService.DownloadReleaseAssetAsync(asset, zipPath, ct);

        var extractPath = Path.Combine(workDir, "extracted");
        ZipFile.ExtractToDirectory(zipPath, extractPath);

        _extractedPackagePath = extractPath;
        _extractedReleaseId = release.Id;
        return extractPath;
    }

    /// <summary>
    /// Copies the package's Data folder and merges USER.cfg into the channel.
    /// Returns the list of file paths (relative to the channel's Data folder) that were copied,
    /// so they can be precisely removed again on uninstall.
    /// </summary>
    public IReadOnlyList<string> ApplyToChannel(string extractedPackagePath, string channelPath)
    {
        var copiedRelativePaths = Array.Empty<string>();

        var sourceDataPath = Path.Combine(extractedPackagePath, "Data");
        if (Directory.Exists(sourceDataPath))
        {
            copiedRelativePaths = CopyDirectory(sourceDataPath, Path.Combine(channelPath, "Data"));
        }

        var sourceUserCfgPath = Path.Combine(extractedPackagePath, "USER.cfg");
        if (File.Exists(sourceUserCfgPath))
        {
            var targetUserCfgPath = Path.Combine(channelPath, "USER.cfg");
            UserCfgMerger.Apply(sourceUserCfgPath, targetUserCfgPath);
        }

        return copiedRelativePaths;
    }

    /// <summary>
    /// Removes the previously installed Data files and strips the "g_language" line from
    /// USER.cfg. Never touches directories — only the tracked files themselves are deleted;
    /// USER.cfg is kept, and no folder (not even one left empty by this) is ever removed.
    /// </summary>
    public void RemoveFromChannel(string channelPath, IReadOnlyList<string> installedDataFiles)
    {
        var dataPath = Path.Combine(channelPath, "Data");
        foreach (var relativePath in installedDataFiles)
        {
            var filePath = Path.Combine(dataPath, relativePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        var userCfgPath = Path.Combine(channelPath, "USER.cfg");
        if (File.Exists(userCfgPath))
        {
            UserCfgMerger.RemoveLanguageLine(userCfgPath);
        }
    }

    private static string[] CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        var relativePaths = new List<string>();
        foreach (var filePath in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, filePath);
            var destPath = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(filePath, destPath, overwrite: true);
            relativePaths.Add(relativePath);
        }
        return relativePaths.ToArray();
    }
}
