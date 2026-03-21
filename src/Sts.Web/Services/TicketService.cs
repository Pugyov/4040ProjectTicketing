using Microsoft.EntityFrameworkCore;
using Sts.Web.Data;
using Sts.Web.Models;

namespace Sts.Web.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _dbContext;

    public TicketService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TicketCreationResult> CreateAsync(CreateTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CreatedByUserId))
        {
            return TicketCreationResult.Failed((string.Empty, "Unable to determine the current user."));
        }

        var ticket = new Ticket
        {
            Subject = request.Subject,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            Team = request.Team,
            Status = request.Status,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);

        try
        {
            await _dbContext.SaveChangesAsync();
            return TicketCreationResult.Success();
        }
        catch (DbUpdateException)
        {
            return TicketCreationResult.Failed((string.Empty, "The ticket could not be saved. Please try again."));
        }
    }

    public async Task<IReadOnlyList<RecentTicketListItem>> GetRecentTicketsForTeamAsync(Team team, int maxCount)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Team == team)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Take(maxCount)
            .Select(ticket => new RecentTicketListItem
            {
                Subject = ticket.Subject,
                Team = ticket.Team,
                CreatedAtUtc = ticket.CreatedAtUtc
            })
            .ToListAsync();
    }
}
