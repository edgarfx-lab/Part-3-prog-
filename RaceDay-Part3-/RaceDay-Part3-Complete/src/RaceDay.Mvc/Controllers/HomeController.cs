using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class HomeController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? eventType, DateTime? from)
    {
        from ??= DateTime.Today;
        var path = "api/events";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(eventType)) query.Add("eventType=" + Uri.EscapeDataString(eventType));
        if (from.HasValue) query.Add("from=" + Uri.EscapeDataString(from.Value.ToString("O")));
        if (query.Count > 0) path += "?" + string.Join("&", query);

        var result = await api.SendAsync(HttpMethod.Get, path);
        var vm = new JsonListViewModel { Title = "Upcoming events", Message = result.Error };
        if (result.IsSuccess && result.Data.ValueKind == System.Text.Json.JsonValueKind.Array)
            vm.Items = result.Data.EnumerateArray().ToList();
        ViewBag.EventType = eventType;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        return View(vm);
    }

    public IActionResult Error() => View();
}
