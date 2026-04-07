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
                Id = ticket.Id,
                Subject = ticket.Subject,
                Team = ticket.Team,
                Status = ticket.Status,
                CreatedAtUtc = ticket.CreatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<TicketSummaryQueryResult> GetUnresolvedSummaryAsync()
    {
        var unresolvedCounts = await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Status == TicketStatus.New || ticket.Status == TicketStatus.Open)
            .GroupBy(ticket => ticket.Team)
            .Select(group => new
            {
                Team = group.Key,
                NewCount = group.Count(ticket => ticket.Status == TicketStatus.New),
                OpenCount = group.Count(ticket => ticket.Status == TicketStatus.Open)
            })
            .ToDictionaryAsync(item => item.Team);

        var items = Enum.GetValues<Team>()
            .Select(team => new TeamTicketSummaryItem
            {
                Team = team,
                NewCount = unresolvedCounts.GetValueOrDefault(team)?.NewCount ?? 0,
                OpenCount = unresolvedCounts.GetValueOrDefault(team)?.OpenCount ?? 0
            })
            .ToArray();

        return new TicketSummaryQueryResult
        {
            Items = items
        };
    }

    public async Task<EditableTicketDetails?> GetEditableTicketAsync(int ticketId, Team requestingTeam)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId && ticket.Team == requestingTeam)
            .Select(ticket => new EditableTicketDetails
            {
                Id = ticket.Id,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Team = ticket.Team,
                Status = ticket.Status
            })
            .SingleOrDefaultAsync();
    }

    public async Task<TicketUpdateResult> UpdateAsync(TicketUpdateRequest request)
    {
        var ticket = await _dbContext.Tickets
            .SingleOrDefaultAsync(item => item.Id == request.TicketId && item.Team == request.RequestingTeam);

        if (ticket is null)
        {
            return TicketUpdateResult.Missing();
        }

        ticket.Subject = request.Subject;
        ticket.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        ticket.Status = request.Status;

        try
        {
            await _dbContext.SaveChangesAsync();
            return TicketUpdateResult.Success();
        }
        catch (DbUpdateException)
        {
            return TicketUpdateResult.Failed((string.Empty, "The ticket could not be updated. Please try again."));
        }
    }

    public async Task<TicketDeletionResult> DeleteAsync(int ticketId, Team requestingTeam)
    {
        var ticket = await _dbContext.Tickets
            .SingleOrDefaultAsync(item => item.Id == ticketId && item.Team == requestingTeam);

        if (ticket is null)
        {
            return TicketDeletionResult.Missing();
        }

        _dbContext.Tickets.Remove(ticket);
        await _dbContext.SaveChangesAsync();
        return TicketDeletionResult.Success();
    }
}
