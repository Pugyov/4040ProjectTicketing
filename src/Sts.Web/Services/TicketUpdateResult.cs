namespace Sts.Web.Services;

public class TicketUpdateResult
{
    public bool Succeeded { get; init; }

    public bool NotFound { get; init; }

    public Dictionary<string, List<string>> Errors { get; } = new();

    public static TicketUpdateResult Success() => new() { Succeeded = true };

    public static TicketUpdateResult Missing() => new() { NotFound = true };

    public static TicketUpdateResult Failed(params (string Key, string Error)[] errors)
    {
        var result = new TicketUpdateResult();

        foreach (var (key, error) in errors)
        {
            if (!result.Errors.TryGetValue(key, out var current))
            {
                current = new List<string>();
                result.Errors[key] = current;
            }

            current.Add(error);
        }

        return result;
    }
}
