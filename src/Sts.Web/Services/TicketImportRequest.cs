using Sts.Web.Models;

namespace Sts.Web.Services;

public class TicketImportRequest
{
    public Stream FileStream { get; init; } = Stream.Null;

    public string CreatedByUserId { get; init; } = string.Empty;

    public Team Team { get; init; }
}
