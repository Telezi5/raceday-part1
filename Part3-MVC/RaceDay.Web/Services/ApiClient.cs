using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int Status { get; set; }
}

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx = ctx;
    }

    private ISession Session => _ctx.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null, bool captureCookie = false) => SendAsync<T>(HttpMethod.Post, url, body, captureCookie);
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Put, url, body);
    public Task<ApiResult<T>> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, bool captureCookie = false)
    {
        try
        {
            var req = new HttpRequestMessage(method, url);

            var cookie = Session.GetString("ApiCookie");
            if (!string.IsNullOrEmpty(cookie)) req.Headers.Add("Cookie", cookie);

            if (body != null)
                req.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req);

            if (captureCookie && res.IsSuccessStatusCode && res.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                var pairs = cookies.Select(c => c.Split(';')[0]);
                Session.SetString("ApiCookie", string.Join("; ", pairs));
            }

            var text = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                T? data = default;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    try { data = JsonSerializer.Deserialize<T>(text, Json); } catch { }
                }
                return new ApiResult<T> { Success = true, Data = data, Status = (int)res.StatusCode };
            }

            return new ApiResult<T> { Success = false, Status = (int)res.StatusCode, Error = FriendlyError((int)res.StatusCode, text) };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T> { Success = false, Error = "The RaceDay service is not reachable right now. Please try again shortly." };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T> { Success = false, Error = "The request took too long. Please try again." };
        }
    }

    private static string FriendlyError(int status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(d.GetString()))
                return d.GetString()!;
            if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString()!;
        }
        catch { }

        return status switch
        {
            401 => "Please log in to continue.",
            403 => "You do not have permission to do that.",
            404 => "We could not find what you were looking for.",
            _ => "Something went wrong. Please try again."
        };
    }
}