using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using StarStringsUpdater.Models;

namespace StarStringsUpdater.Services;

/// <summary>
/// Read-only access to the public GitHub API for the MrKraken/StarStrings repo.
/// </summary>
public sealed class GitHubReleaseService : IDisposable
{
    private const string Owner = "MrKraken";
    private const string Repo = "StarStrings";

    // Each track is its own rolling release tag, reused on every publish for that track.
    private const string LiveTag = "latest";
    private const string PtuTag = "latest-ptu";

    private readonly HttpClient _http;

    public GitHubReleaseService()
    {
        _http = new HttpClient();
        // GitHub rejects requests without a User-Agent header.
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("StarStringsUpdater", "1.0"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public Task<GitHubRelease> GetReleaseAsync(ReleaseTrack track, CancellationToken ct = default)
    {
        var tag = track switch
        {
            ReleaseTrack.Live => LiveTag,
            ReleaseTrack.Ptu => PtuTag,
            _ => throw new ArgumentOutOfRangeException(nameof(track)),
        };
        return GetReleaseByTagAsync(tag, ct);
    }

    private async Task<GitHubRelease> GetReleaseByTagAsync(string tag, CancellationToken ct)
    {
        using var response = await _http.GetAsync(
            $"https://api.github.com/repos/{Owner}/{Repo}/releases/tags/{tag}", ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: ct);
        return release ?? throw new InvalidOperationException($"Invalid GitHub response (release '{tag}' not found).");
    }

    public async Task DownloadReleaseAssetAsync(GitHubReleaseAsset asset, string destinationFilePath, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var httpStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = File.Create(destinationFilePath);
        await httpStream.CopyToAsync(fileStream, ct);
    }

    public void Dispose() => _http.Dispose();
}
