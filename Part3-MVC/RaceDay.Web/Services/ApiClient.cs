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

    /// <summary>Sends an image to the API as multipart/form-data (field name "file").</summary>
    public async Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            var cookie = Session.GetString("ApiCookie");
            if (!string.IsNullOrEmpty(cookie)) req.Headers.Add("Cookie", cookie);

            var res = await _http.SendAsync(req);
            return await ReadAsync<T>(res);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T> { Success = false, Error = "The RaceDay service is not reachable right now. Please try again shortly." };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T> { Success = false, Error = "The upload took too long. Please try again." };
        }
    }

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

            return await ReadAsync<T>(res);
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

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage res)
    {
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

    private static string FriendlyError(int status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var name in new[] { "message", "detail", "title" })
            {
                if (doc.RootElement.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(p.GetString()))
                    return p.GetString()!;
            }
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