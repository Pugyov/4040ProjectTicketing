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
    private readonly ITicketImportService _ticketImportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketController(ITicketService ticketService, ITicketImportService ticketImportService, UserManager<ApplicationUser> userManager)
    {
        _ticketService = ticketService;
        _ticketImportService = ticketImportService;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View(new CreateTicketViewModel());
    }

    [HttpGet]
    public IActionResult Import()
    {
        return View(new ImportTicketsViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var ticket = await _ticketService.GetEditableTicketAsync(id, user.Team);
        if (ticket is null)
        {
            return NotFound();
        }

        return View(new EditTicketViewModel
        {
            Subject = ticket.Subject,
            Description = ticket.Description,
            Status = ticket.Status.ToString()
        });
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

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EditTicketViewModel model)
    {
        model.Subject = model.Subject.Trim();
        model.Description = model.Description?.Trim();
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

        Enum.TryParse<TicketStatus>(model.Status, out var status);
        var result = await _ticketService.UpdateAsync(new TicketUpdateRequest
        {
            TicketId = id,
            RequestingTeam = user.Team,
            Subject = model.Subject,
            Description = model.Description,
            Status = status
        });

        if (result.NotFound)
        {
            return NotFound();
        }

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

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _ticketService.DeleteAsync(id, user.Team);
        if (result.NotFound)
        {
            return NotFound();
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Import(ImportTicketsViewModel model)
    {
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

        await using var fileStream = model.File!.OpenReadStream();
        var result = await _ticketImportService.ImportAsync(new TicketImportRequest
        {
            FileStream = fileStream,
            CreatedByUserId = user.Id,
            Team = user.Team
        });

        if (!result.Succeeded)
        {
            ViewData["ImportErrors"] = result.Errors;
            return View(model);
        }

        var homeModel = await BuildHomeIndexViewModelAsync(user);
        homeModel.StatusMessage = $"Successfully imported {result.ImportedCount} tickets.";
        return View("~/Views/Home/Index.cshtml", homeModel);
    }

    private async Task<HomeIndexViewModel> BuildHomeIndexViewModelAsync(ApplicationUser user)
    {
        return new HomeIndexViewModel
        {
            IsAuthenticated = true,
            CurrentTeam = user.Team,
            UnresolvedSummary = (await _ticketService.GetUnresolvedSummaryAsync()).Items,
            RecentTickets = await _ticketService.GetRecentTicketsForTeamAsync(user.Team, 10)
        };
    }
}
