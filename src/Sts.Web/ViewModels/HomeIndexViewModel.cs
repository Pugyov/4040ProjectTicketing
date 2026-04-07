using Sts.Web.Models;
using Sts.Web.Services;

namespace Sts.Web.ViewModels;

public class HomeIndexViewModel
{
    public bool IsAuthenticated { get; set; }

    public Team? CurrentTeam { get; set; }

    public string? StatusMessage { get; set; }

    public IReadOnlyList<TeamTicketSummaryItem> UnresolvedSummary { get; set; } = Array.Empty<TeamTicketSummaryItem>();

    public IReadOnlyList<RecentTicketListItem> RecentTickets { get; set; } = Array.Empty<RecentTicketListItem>();
}
