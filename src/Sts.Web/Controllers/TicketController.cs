using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sts.Web.Models;
using Sts.Web.Services;
using Sts.Web.ViewModels;

namespace Sts.Web.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketController(ITicketService ticketService, UserManager<ApplicationUser> userManager)
    {
        _ticketService = ticketService;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View(new CreateTicketViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateTicketViewModel model)
    {
        model.Subject = model.Subject.Trim();
        model.Description = model.Description?.Trim();
        model.Team = model.Team.Trim();
        model.Status = model.Status.Trim();

        ModelState.Clear();
        if (!TryValidateModel(model))
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        Enum.TryParse<Team>(model.Team, out var team);
        Enum.TryParse<TicketStatus>(model.Status, out var status);

        var result = await _ticketService.CreateAsync(new CreateTicketRequest
        {
            Subject = model.Subject,
            Description = model.Description,
            Team = team,
            Status = status,
            CreatedByUserId = user.Id
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
}
