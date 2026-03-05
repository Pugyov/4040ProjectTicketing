using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sts.Web.Models;
using Sts.Web.Services;
using Sts.Web.ViewModels;

namespace Sts.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.Email = model.Email.Trim();
        model.Name = model.Name.Trim();
        model.Team = model.Team.Trim();

        ModelState.Clear();
        if (!TryValidateModel(model))
        {
            return View(model);
        }

        Enum.TryParse<Team>(model.Team, out var team);
        var result = await _accountService.RegisterAsync(new RegisterRequest
        {
            Email = model.Email,
            Password = model.Password,
            Name = model.Name,
            Team = team
        });

        if (!result.Succeeded)
        {
            foreach (var (key, errors) in result.Errors)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(key, error);
                }
            }

            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.Email = model.Email.Trim();

        ModelState.Clear();
        if (!TryValidateModel(model))
        {
            return View(model);
        }

        var result = await _accountService.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(result.ErrorKey ?? string.Empty, result.ErrorMessage ?? "Login failed.");
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }
}
