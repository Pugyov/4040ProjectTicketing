using Sts.Web.Models;

namespace Sts.Web.Services;

public class EditableTicketDetails
{
    public int Id { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Team Team { get; init; }

    public TicketStatus Status { get; init; }
}
