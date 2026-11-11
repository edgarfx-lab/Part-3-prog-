using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class ParticipantController(RaceDayApiClient api) : Controller
{
    private bool Allowed() => HttpContext.Session.GetString("UserRole") == "Participant"
        && HttpContext.Session.GetString("ApiSessionCookie") is not null;

    public async Task<IActionResult> Index(string? eventType, DateTime? from)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        from ??= DateTime.Today;
        var path = "api/events?from=" + Uri.EscapeDataString(from.Value.ToString("O"));
        if (!string.IsNullOrWhiteSpace(eventType)) path += "&eventType=" + Uri.EscapeDataString(eventType);
        ViewBag.EventType = eventType;
        ViewBag.From = from.Value.ToString("yyyy-MM-dd");
        var r = await api.SendAsync(HttpMethod.Get, path);
        var vm = new JsonListViewModel { Title = "Find your next race", Message = r.Error };
        if (r.IsSuccess && r.Data.ValueKind == JsonValueKind.Array)
            vm.Items = r.Data.EnumerateArray().Where(e => e.GetProperty("eventDate").GetDateTime() >= DateTime.Today).ToList();
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        if (!r.IsSuccess) return NotFound();
        ViewBag.Categories = r.Data.TryGetProperty("categories", out var c) && c.ValueKind == JsonValueKind.Array
            ? c.EnumerateArray().ToList() : new List<JsonElement>();
        return View(r.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Post, $"api/events/{eventId}/enrolments", new { CategoryId = categoryId });
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Your enrolment has been submitted." : r.Error;
        return RedirectToAction(nameof(Details), new { id = eventId });
    }

    public async Task<IActionResult> MyEnrolments()
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, "api/users/me/enrolments");
        var vm = new JsonListViewModel { Title = "My enrolments", Message = r.Error };
        if (r.IsSuccess && r.Data.ValueKind == JsonValueKind.Array) vm.Items = r.Data.EnumerateArray().ToList();
        return View(vm);
    }

    public async Task<IActionResult> MyResults()
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, "api/users/me/results");
        var vm = new JsonListViewModel { Title = "My race results", Message = r.Error };
        if (r.IsSuccess && r.Data.ValueKind == JsonValueKind.Array) vm.Items = r.Data.EnumerateArray().ToList();
        return View(vm);
    }
}
