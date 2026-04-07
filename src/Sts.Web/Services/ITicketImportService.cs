namespace Sts.Web.Services;

public interface ITicketImportService
{
    Task<TicketImportResult> ImportAsync(TicketImportRequest request, CancellationToken cancellationToken = default);
}
