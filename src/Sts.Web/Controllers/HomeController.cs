using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Sts.Web.Models;
using Sts.Web.Services;
using Sts.Web.ViewModels;

namespace Sts.Web.Controllers;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITicketService _ticketService;

    public HomeController(UserManager<ApplicationUser> userManager, ITicketService ticketService)
    {
        _userManager = userManager;
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new HomeIndexViewModel
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        };

        if (!model.IsAuthenticated)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            model.IsAuthenticated = false;
            return View(model);
        }

        model.CurrentTeam = user.Team;
        model.RecentTickets = await _ticketService.GetRecentTicketsForTeamAsync(user.Team, 10);
        return View(model);
    }

    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }
}
