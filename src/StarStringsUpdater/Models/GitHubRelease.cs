using System.Text.Json.Serialization;

namespace StarStringsUpdater.Models;

public sealed class GitHubRelease
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("published_at")]
    public DateTimeOffset PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubReleaseAsset> Assets { get; set; } = new();

    /// <summary>
    /// This repo's GitHub tag is a rolling "latest" for every release, so it can't be used
    /// to detect a new version — hence picking the first .zip asset (name may vary).
    /// </summary>
    public GitHubReleaseAsset? FindZipAsset() =>
        Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
}

public sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
