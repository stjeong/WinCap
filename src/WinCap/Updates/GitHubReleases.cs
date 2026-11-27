using System.Net.Http.Headers;
using System.Text.Json;

namespace WinCap.Updates;

/// <summary>A downloadable file attached to a GitHub release.</summary>
internal sealed record ReleaseAsset(string Name, string DownloadUrl, long Size);

/// <summary>The parts of a GitHub release that the updater uses.</summary>
internal sealed record ReleaseInfo(
    Version Version,
    string Tag,
    string Name,
    string Notes,
    string PageUrl,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<ReleaseAsset> Assets)
{
    public ReleaseAsset? FindAsset(string suffix) =>
        Assets.FirstOrDefault(a => a.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Reads the latest release of the WinCap repository and downloads its files.</summary>
internal static class GitHubReleases
{
    public const string Repository = "stjeong/WinCap";
    public static string ReleasesPageUrl => $"https://github.com/{Repository}/releases";

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan }; // per call via CancellationToken
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WinCap", AppVersion.Current.ToString()));
        return client;
    }

    /// <summary>Returns the latest published (non-draft, non-prerelease) release.</summary>
    public static async Task<ReleaseInfo> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases/latest");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await Http.SendAsync(request, timeout.Token).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("No release has been published yet.");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token).ConfigureAwait(false);
        var root = json.RootElement;

        string tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
        if (!AppVersion.TryParse(tag, out Version version))
            throw new InvalidOperationException($"The latest release tag \"{tag}\" is not a version number.");

        var assets = new List<ReleaseAsset>();
        foreach (var a in root.GetProperty("assets").EnumerateArray())
        {
            assets.Add(new ReleaseAsset(
                a.GetProperty("name").GetString() ?? string.Empty,
                a.GetProperty("browser_download_url").GetString() ?? string.Empty,
                a.GetProperty("size").GetInt64()));
        }

        DateTimeOffset? published = root.TryGetProperty("published_at", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetDateTimeOffset()
            : null;

        return new ReleaseInfo(
            version,
            tag,
            root.TryGetProperty("name", out var n) ? n.GetString() ?? tag : tag,
            root.TryGetProperty("body", out var b) ? b.GetString() ?? string.Empty : string.Empty,
            root.TryGetProperty("html_url", out var u) ? u.GetString() ?? ReleasesPageUrl : ReleasesPageUrl,
            published,
            assets);
    }

    /// <summary>Downloads <paramref name="asset"/> to <paramref name="path"/>, reporting progress 0..1.</summary>
    /// <remarks>A partial file is deleted when the download fails or is canceled.</remarks>
    public static async Task DownloadAsync(ReleaseAsset asset, string path, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(asset.DownloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"Unexpected download address: {asset.DownloadUrl}");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            long total = response.Content.Headers.ContentLength ?? asset.Size;

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var target = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            var buffer = new byte[81920];
            long done = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                done += read;
                if (total > 0) progress?.Report((double)done / total);
            }
            if (total > 0 && done != total)
                throw new IOException($"Download incomplete ({done:N0} of {total:N0} bytes).");
        }
        catch
        {
            try { File.Delete(path); } catch { /* best effort */ }
            throw;
        }
    }
}
