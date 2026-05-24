using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace LiteQMS.Pages;

public class UpdatesModel : PageModel
{
    private static readonly HttpClient _http = new();
    private static readonly string _currentVersion =
        Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString(3) ?? "1.0.0";

    private readonly IMemoryCache _cache;

    public UpdatesModel(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string CurrentVersion { get; set; } = _currentVersion;
    public string? LatestVersion { get; set; }
    public string? DownloadUrl { get; set; }
    public bool IsUpdateAvailable { get; set; }
    public bool CheckFailed { get; set; }
    public bool IsDevelopment { get; set; }

    public async Task OnGet()
    {
        IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        var cacheKey = "GitHubLatestRelease";

        if (_cache.TryGetValue(cacheKey, out GitHubRelease? release) && release is not null)
        {
            ApplyRelease(release);
            return;
        }

        try
        {
            _http.DefaultRequestHeaders.UserAgent.TryParseAdd("LiteQMS/1.0");

            release = await _http.GetFromJsonAsync<GitHubRelease>(
                "https://api.github.com/repos/spd3ictpro/liteqms/releases/latest");

            if (release is not null)
            {
                _cache.Set(cacheKey, release, TimeSpan.FromHours(1));
                ApplyRelease(release);
            }
            else
            {
                CheckFailed = true;
            }
        }
        catch
        {
            CheckFailed = true;
        }
    }

    void ApplyRelease(GitHubRelease release)
    {
        LatestVersion = release.TagName?.TrimStart('v', 'V') ?? release.TagName;
        DownloadUrl = release.HtmlUrl;

        if (LatestVersion is not null)
        {
            IsUpdateAvailable = string.Compare(LatestVersion, _currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }
}

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}
