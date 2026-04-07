namespace Sts.Web.Services;

public class TicketDeletionResult
{
    public bool Succeeded { get; init; }

    public bool NotFound { get; init; }

    public static TicketDeletionResult Success() => new() { Succeeded = true };

    public static TicketDeletionResult Missing() => new() { NotFound = true };
}
