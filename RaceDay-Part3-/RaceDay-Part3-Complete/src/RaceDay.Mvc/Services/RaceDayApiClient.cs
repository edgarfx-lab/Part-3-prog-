using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    public async Task<ApiResponse> SendAsync(HttpMethod method, string path, object? body = null,
        IFormFile? file = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, path.TrimStart('/'));
        var cookie = _accessor.HttpContext?.Session.GetString("ApiSessionCookie");
        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        if (file is not null)
        {
            var multipart = new MultipartFormDataContent();
            var stream = file.OpenReadStream();
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            multipart.Add(content, "image", Path.GetFileName(file.FileName));
            request.Content = multipart;
        }
        else if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            JsonElement data;
            try
            {
                using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
                data = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                data = JsonDocument.Parse(JsonSerializer.Serialize(new { message = raw })).RootElement.Clone();
            }

            var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values.FirstOrDefault()?.Split(';', 2)[0]
                : null;

            string? error = null;
            if (!response.IsSuccessStatusCode)
            {
                error = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("message", out var message)
                    ? message.GetString()
                    : $"The API returned HTTP {(int)response.StatusCode}.";
            }

            return new ApiResponse { StatusCode = (int)response.StatusCode, Data = data, Error = error, SetCookie = setCookie };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse
            {
                StatusCode = 503,
                Data = JsonDocument.Parse("{}").RootElement.Clone(),
                Error = "The RaceDay API is unavailable. Start the API and check ApiSettings:BaseUrl."
            };
        }
        catch (TaskCanceledException)
        {
            return new ApiResponse
            {
                StatusCode = 504,
                Data = JsonDocument.Parse("{}").RootElement.Clone(),
                Error = "The API request timed out. Please try again."
            };
        }
    }
}
