using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Andy.Tui.Examples.HackerNews;

public sealed class HackerNewsApiClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://hacker-news.firebaseio.com/v0";

    public HackerNewsApiClient()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<int>> GetTopStoriesAsync(int limit = 30, CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<int>>($"{BaseUrl}/topstories.json", ct);
        return ids?.Take(limit).ToList() ?? new List<int>();
    }

    public async Task<List<int>> GetNewStoriesAsync(int limit = 30, CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<int>>($"{BaseUrl}/newstories.json", ct);
        return ids?.Take(limit).ToList() ?? new List<int>();
    }

    public async Task<List<int>> GetBestStoriesAsync(int limit = 30, CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<int>>($"{BaseUrl}/beststories.json", ct);
        return ids?.Take(limit).ToList() ?? new List<int>();
    }

    public async Task<List<int>> GetAskStoriesAsync(int limit = 30, CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<int>>($"{BaseUrl}/askstories.json", ct);
        return ids?.Take(limit).ToList() ?? new List<int>();
    }

    public async Task<List<int>> GetShowStoriesAsync(int limit = 30, CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<int>>($"{BaseUrl}/showstories.json", ct);
        return ids?.Take(limit).ToList() ?? new List<int>();
    }

    public async Task<List<int>> GetJobStoriesAsync(int limit = 30, CancellationToken ct = default)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<int>>($"{BaseUrl}/jobstories.json", ct);
        return ids?.Take(limit).ToList() ?? new List<int>();
    }

    public async Task<HNItem?> GetItemAsync(int id, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<HNItem>($"{BaseUrl}/item/{id}.json", ct);
    }

    public async Task<HNUser?> GetUserAsync(string username, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<HNUser>($"{BaseUrl}/user/{username}.json", ct);
    }

    public async Task<List<HNItem>> GetItemsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var tasks = ids.Select(id => GetItemAsync(id, ct));
        var items = await Task.WhenAll(tasks);
        return items.Where(i => i != null).Cast<HNItem>().ToList();
    }
}

public sealed record HNItem
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("by")]
    public string? By { get; init; }

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("dead")]
    public bool? Dead { get; init; }

    [JsonPropertyName("deleted")]
    public bool? Deleted { get; init; }

    [JsonPropertyName("parent")]
    public int? Parent { get; init; }

    [JsonPropertyName("poll")]
    public int? Poll { get; init; }

    [JsonPropertyName("kids")]
    public List<int>? Kids { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("score")]
    public int? Score { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("parts")]
    public List<int>? Parts { get; init; }

    [JsonPropertyName("descendants")]
    public int? Descendants { get; init; }

    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(Time).DateTime;
    public string Domain => Url != null && Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        ? uri.Host.Replace("www.", "")
        : string.Empty;
}

public sealed record HNUser
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; init; }

    [JsonPropertyName("karma")]
    public int Karma { get; init; }

    [JsonPropertyName("about")]
    public string? About { get; init; }

    [JsonPropertyName("submitted")]
    public List<int>? Submitted { get; init; }

    public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(Created).DateTime;
}
