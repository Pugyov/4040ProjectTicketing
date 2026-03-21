using Sts.Web.Models;

namespace Sts.Web.Services;

public class RecentTicketListItem
{
    public string Subject { get; init; } = string.Empty;

    public Team Team { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
