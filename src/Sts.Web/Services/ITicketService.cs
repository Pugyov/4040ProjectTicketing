using Sts.Web.Models;

namespace Sts.Web.Services;

public interface ITicketService
{
    Task<TicketCreationResult> CreateAsync(CreateTicketRequest request);

    Task<IReadOnlyList<RecentTicketListItem>> GetRecentTicketsForTeamAsync(Team team, int maxCount);
}
