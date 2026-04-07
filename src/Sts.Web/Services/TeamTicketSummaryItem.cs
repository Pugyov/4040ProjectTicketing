using Sts.Web.Models;

namespace Sts.Web.Services;

public class TeamTicketSummaryItem
{
    public Team Team { get; init; }

    public int NewCount { get; init; }

    public int OpenCount { get; init; }
}
