using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace MedalSync;

public sealed record UpdateInfo(Version Version, string AssetName, string DownloadUrl);

public static class UpdateService
{
    private const string RepositoryOwner = "Reskeyo";
    private const string RepositoryName = "MedalSync";
    private static readonly Uri LatestReleaseUri = new(
        $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest");

    private static readonly HttpClient Client = CreateHttpClient();

    public static async Task<UpdateInfo?> CheckForUpdateAsync(Settings settings, CancellationToken cancellationToken = default)
    {
        if (!settings.AutoUpdateEnabled)
            return null;

        var now = DateTime.UtcNow;
        if (now - settings.LastUpdateCheckUtc < TimeSpan.FromHours(12))
            return null;

        settings.LastUpdateCheckUtc = now;
        settings.Save();

        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUri);
        using var response = await Client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!doc.RootElement.TryGetProperty("tag_name", out var tagProp))
            return null;

        var latestVersion = ParseVersion(tagProp.GetString());
        if (latestVersion == null)
            return null;

        var currentVersion = GetCurrentVersion();
        if (latestVersion <= currentVersion)
            return null;

        if (!doc.RootElement.TryGetProperty("assets", out var assets))
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameProp))
                continue;

            var name = nameProp.GetString();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!asset.TryGetProperty("browser_download_url", out var urlProp))
                continue;

            var url = urlProp.GetString();
            if (string.IsNullOrWhiteSpace(url))
                continue;

            return new UpdateInfo(latestVersion, name, url);
        }

        return null;
    }

    public static async Task<string> DownloadUpdateAsync(UpdateInfo update, CancellationToken cancellationToken = default)
    {
        using var response = await Client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var targetPath = Path.Combine(Path.GetTempPath(), update.AssetName);
        await using var file = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        await content.CopyToAsync(file, cancellationToken);

        return targetPath;
    }

    public static void RunInstaller(string installerPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART",
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private static Version GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version ?? new Version(0, 0, 0, 0);
    }

    private static Version? ParseVersion(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        tag = tag.Trim();
        if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            tag = tag[1..];

        return Version.TryParse(tag, out var version) ? version : null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MedalSync-Updater/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }
}
