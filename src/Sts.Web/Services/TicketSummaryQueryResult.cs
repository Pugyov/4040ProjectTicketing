namespace Sts.Web.Services;

public class TicketSummaryQueryResult
{
    public IReadOnlyList<TeamTicketSummaryItem> Items { get; init; } = Array.Empty<TeamTicketSummaryItem>();
}
