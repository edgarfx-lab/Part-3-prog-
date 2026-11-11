using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class AccountController(RaceDayApiClient api) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.SendAsync(HttpMethod.Post, "api/auth/login", new { model.Email, model.Password });
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
            return View(model);
        }
        if (string.IsNullOrWhiteSpace(result.SetCookie))
        {
            ModelState.AddModelError(string.Empty, "The API did not return its session cookie. Check the API session configuration.");
            return View(model);
        }
        HttpContext.Session.SetString("ApiSessionCookie", result.SetCookie);
        var me = await api.SendAsync(HttpMethod.Get, "api/users/me");
        if (!me.IsSuccess)
        {
            HttpContext.Session.Clear();
            ModelState.AddModelError(string.Empty, "Login succeeded but the profile could not be loaded.");
            return View(model);
        }
        HttpContext.Session.SetString("UserRole", me.Data.GetProperty("role").GetString() ?? "");
        HttpContext.Session.SetInt32("UserId", me.Data.GetProperty("userId").GetInt32());
        HttpContext.Session.SetString("UserName", (me.Data.GetProperty("firstName").GetString() ?? "") + " " + (me.Data.GetProperty("lastName").GetString() ?? ""));
        return RedirectToAction("Index", "Home");
    }

    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.SendAsync(HttpMethod.Post, "api/auth/register", new
        {
            model.FirstName, model.LastName, model.Email, model.Password, model.Role, model.PhoneNumber
        });
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return View(model);
        }
        TempData["Success"] = "Your account was created. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Session.GetString("ApiSessionCookie") is not null)
            await api.SendAsync(HttpMethod.Post, "api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
