namespace Sts.Web.Models;

public class Ticket
{
    public int Id { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Team Team { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.New;
}
