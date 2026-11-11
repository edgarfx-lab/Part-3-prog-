using System.Text.Json;

namespace RaceDay.Mvc.Services;

public sealed class ApiResponse
{
    public int StatusCode { get; init; }
    public JsonElement Data { get; init; }
    public string? Error { get; init; }
    public string? SetCookie { get; init; }
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}
