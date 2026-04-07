namespace Sts.Web.Services;

public class TicketImportResult
{
    public bool Succeeded { get; init; }

    public int ImportedCount { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static TicketImportResult Success(int importedCount) => new()
    {
        Succeeded = true,
        ImportedCount = importedCount
    };

    public static TicketImportResult Failed(params string[] errors) => new()
    {
        Errors = errors
    };
}
