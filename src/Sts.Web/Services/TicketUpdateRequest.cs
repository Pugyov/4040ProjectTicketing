using Sts.Web.Models;

namespace Sts.Web.Services;

public class TicketUpdateRequest
{
    public int TicketId { get; init; }

    public Team RequestingTeam { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public TicketStatus Status { get; init; }
}
