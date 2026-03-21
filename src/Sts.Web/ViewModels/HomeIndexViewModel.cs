using Sts.Web.Models;
using Sts.Web.Services;

namespace Sts.Web.ViewModels;

public class HomeIndexViewModel
{
    public bool IsAuthenticated { get; set; }

    public Team? CurrentTeam { get; set; }

    public IReadOnlyList<RecentTicketListItem> RecentTickets { get; set; } = Array.Empty<RecentTicketListItem>();
}
