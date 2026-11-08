using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;
    public AccountController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("Role") != null) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var login = await _api.PostAsync<object>("api/auth/login", new { model.Email, model.Password }, captureCookie: true);
        if (!login.Success)
        {
            ModelState.AddModelError("", login.Error ?? "Login failed.");
            return View(model);
        }

        var me = await _api.GetAsync<UserDto>("api/users/me");
        if (!me.Success || me.Data == null)
        {
            HttpContext.Session.Clear();
            ModelState.AddModelError("", "We could not load your account. Please try again.");
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", me.Data.UserID);
        HttpContext.Session.SetString("Role", me.Data.Role);
        HttpContext.Session.SetString("FullName", me.Data.FullName);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var res = await _api.PostAsync<object>("api/auth/register", new
        {
            model.FullName,
            model.Email,
            model.Password,
            model.Role,
            model.ContactNumber,
            model.OrganiserKey
        });

        if (!res.Success)
        {
            ModelState.AddModelError("", res.Error ?? "Registration failed.");
            return View(model);
        }

        TempData["Success"] = "Account created! Please log in.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<object>("api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}