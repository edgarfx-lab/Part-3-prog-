using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class OrganiserController(RaceDayApiClient api) : Controller
{
    private bool Allowed() => HttpContext.Session.GetString("UserRole") == "Organiser"
        && HttpContext.Session.GetString("ApiSessionCookie") is not null;

    public async Task<IActionResult> Index()
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var response = await api.SendAsync(HttpMethod.Get, "api/events");
        var events = response.IsSuccess && response.Data.ValueKind == JsonValueKind.Array
            ? response.Data.EnumerateArray()
                .Where(e => e.TryGetProperty("organiserId", out var id) && id.GetInt32() == GetUserIdFromSession())
                .ToList()
            : new List<JsonElement>();

        var summaries = new List<OrganiserEventSummary>();
        foreach (var eventItem in events)
        {
            var id = eventItem.GetProperty("eventId").GetInt32();
            var enrolments = await api.SendAsync(HttpMethod.Get, $"api/events/{id}/enrolments");
            var count = enrolments.IsSuccess && enrolments.Data.ValueKind == JsonValueKind.Array
                ? enrolments.Data.GetArrayLength() : 0;
            summaries.Add(new OrganiserEventSummary { Event = eventItem, EnrolmentCount = count });
        }
        ViewBag.ApiError = response.Error;
        ViewBag.UpcomingCount = summaries.Count(x => x.Event.GetProperty("eventDate").GetDateTime() >= DateTime.Today);
        ViewBag.TotalEnrolments = summaries.Sum(x => x.EnrolmentCount);
        var next = summaries.Select(x => x.Event.GetProperty("eventDate").GetDateTime())
            .Where(d => d >= DateTime.Today).OrderBy(d => d).Cast<DateTime?>().FirstOrDefault();
        ViewBag.NextEventDate = next?.ToString("dd MMM yyyy") ?? "None scheduled";
        return View(summaries);
    }

    private int GetUserIdFromSession() => HttpContext.Session.GetInt32("UserId") ?? -1;

    [HttpGet] public IActionResult CreateEvent() { if (!Allowed()) return RedirectToAction("Login", "Account"); return View("EventForm", new EventFormViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View("EventForm", model);
        var result = await api.SendAsync(HttpMethod.Post, "api/events", new
        {
            model.Name, model.Description, model.EventDate, model.Location,
            model.DistanceKm, model.EventTypeId
        });
        if (!result.IsSuccess) { ModelState.AddModelError("", result.Error ?? "Could not create event."); return View("EventForm", model); }
        var id = result.Data.GetProperty("eventId").GetInt32();
        if (model.BannerImage is not null)
        {
            var upload = await api.SendAsync(HttpMethod.Post, $"api/events/{id}/banner", file: model.BannerImage);
            if (!upload.IsSuccess) TempData["Error"] = "Event created, but banner upload failed: " + upload.Error;
        }
        TempData["Success"] = "Event created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet] public async Task<IActionResult> EditEvent(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, $"api/events/{id}");
        if (!r.IsSuccess) return NotFound();
        return View("EventForm", new EventFormViewModel
        {
            EventId = id,
            Name = r.Data.GetProperty("name").GetString() ?? "",
            Description = r.Data.TryGetProperty("description", out var d) ? d.GetString() : null,
            EventDate = r.Data.GetProperty("eventDate").GetDateTime(),
            Location = r.Data.GetProperty("location").GetString() ?? "",
            DistanceKm = r.Data.GetProperty("distanceKm").GetDecimal(),
            EventTypeId = (r.Data.TryGetProperty("eventType", out var eventType) ? eventType.GetString() : "Run") switch { "Walk" => 2, "Cycle" => 3, _ => 1 }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormViewModel model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View("EventForm", model);
        var r = await api.SendAsync(HttpMethod.Put, $"api/events/{model.EventId}", new
        {
            model.Name, model.Description, model.EventDate, model.Location,
            model.DistanceKm, model.EventTypeId
        });
        if (!r.IsSuccess) { ModelState.AddModelError("", r.Error ?? "Could not update event."); return View("EventForm", model); }
        if (model.BannerImage is not null)
        {
            var upload = await api.SendAsync(HttpMethod.Post, $"api/events/{model.EventId}/banner", file: model.BannerImage);
            if (!upload.IsSuccess) TempData["Error"] = "Event updated, but banner upload failed: " + upload.Error;
        }
        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Delete, $"api/events/{id}");
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Event deleted." : r.Error;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Categories(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, $"api/events/{id}/categories");
        var vm = new JsonListViewModel { Title = "Event categories", Message = r.Error };
        if (r.IsSuccess && r.Data.ValueKind == JsonValueKind.Array) vm.Items = r.Data.EnumerateArray().ToList();
        ViewBag.EventId = id;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) { TempData["Error"] = "Enter a valid category name."; return RedirectToAction(nameof(Categories), new { id = model.EventId }); }
        var r = await api.SendAsync(HttpMethod.Post, $"api/events/{model.EventId}/categories", new { model.Name, model.Description });
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Category added." : r.Error;
        return RedirectToAction(nameof(Categories), new { id = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Delete, $"api/categories/{id}");
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Category deleted." : r.Error;
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    public async Task<IActionResult> Enrolments(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, $"api/events/{id}/enrolments");
        var vm = new JsonListViewModel { Title = "Event enrolments", Message = r.Error };
        if (r.IsSuccess && r.Data.ValueKind == JsonValueKind.Array) vm.Items = r.Data.EnumerateArray().ToList();
        ViewBag.EventId = id;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(EnrolmentStatusViewModel model, int eventId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Put, $"api/enrolments/{model.EnrolmentId}/status", new { model.Status });
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Enrolment status updated." : r.Error;
        return RedirectToAction(nameof(Enrolments), new { id = eventId });
    }

    public async Task<IActionResult> Results(int id)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, $"api/events/{id}/enrolments");
        var vm = new JsonListViewModel { Title = "Capture race results", Message = r.Error };
        if (r.IsSuccess && r.Data.ValueKind == JsonValueKind.Array) vm.Items = r.Data.EnumerateArray().ToList();
        ViewBag.EventId = id;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(ResultFormViewModel model, int eventId)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Post, $"api/enrolments/{model.EnrolmentId}/results", new
        {
            model.FinishTime, model.FinishPosition, model.TotalFinishers
        });
        TempData[r.IsSuccess ? "Success" : "Error"] = r.IsSuccess ? "Result recorded." : r.Error;
        return RedirectToAction(nameof(Results), new { id = eventId });
    }
}
