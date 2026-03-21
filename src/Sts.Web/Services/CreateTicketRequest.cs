using Sts.Web.Models;

namespace Sts.Web.Services;

public class CreateTicketRequest
{
    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Team Team { get; init; }

    public TicketStatus Status { get; init; }

    public string CreatedByUserId { get; init; } = string.Empty;
}
