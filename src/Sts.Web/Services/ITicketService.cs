using Sts.Web.Models;

namespace Sts.Web.Services;

public interface ITicketService
{
    Task<TicketCreationResult> CreateAsync(CreateTicketRequest request);

    Task<IReadOnlyList<RecentTicketListItem>> GetRecentTicketsForTeamAsync(Team team, int maxCount);

    Task<TicketSummaryQueryResult> GetUnresolvedSummaryAsync();

    Task<EditableTicketDetails?> GetEditableTicketAsync(int ticketId, Team requestingTeam);

    Task<TicketUpdateResult> UpdateAsync(TicketUpdateRequest request);

    Task<TicketDeletionResult> DeleteAsync(int ticketId, Team requestingTeam);
}
