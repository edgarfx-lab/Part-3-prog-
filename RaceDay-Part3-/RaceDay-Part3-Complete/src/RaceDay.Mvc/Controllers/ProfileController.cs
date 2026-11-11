using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class ProfileController(RaceDayApiClient api) : Controller
{
    private bool Allowed() => HttpContext.Session.GetString("ApiSessionCookie") is not null;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        var r = await api.SendAsync(HttpMethod.Get, "api/users/me");
        if (!r.IsSuccess) { TempData["Error"] = r.Error; return RedirectToAction("Login", "Account"); }
        return View(new ProfileViewModel
        {
            FirstName = r.Data.GetProperty("firstName").GetString() ?? "",
            LastName = r.Data.GetProperty("lastName").GetString() ?? "",
            Email = r.Data.GetProperty("email").GetString(),
            Role = r.Data.GetProperty("role").GetString(),
            ProfilePictureUrl = r.Data.TryGetProperty("profilePictureUrl", out var picture) ? picture.GetString() : null,
            PhoneNumber = r.Data.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() : null
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!Allowed()) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);
        var update = await api.SendAsync(HttpMethod.Put, "api/users/me", new
        {
            model.FirstName, model.LastName, model.PhoneNumber
        });
        if (!update.IsSuccess) { ModelState.AddModelError("", update.Error ?? "Profile update failed."); return View(model); }
        if (model.ProfilePicture is not null)
        {
            var upload = await api.SendAsync(HttpMethod.Put, "api/users/me/profile-picture", file: model.ProfilePicture);
            if (!upload.IsSuccess) TempData["Error"] = "Profile saved, but picture upload failed: " + upload.Error;
        }
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }
}
