namespace Sts.Web.Services;

public class LoginResult
{
    public bool Succeeded { get; init; }

    public string? ErrorKey { get; init; }

    public string? ErrorMessage { get; init; }

    public static LoginResult Success() => new() { Succeeded = true };

    public static LoginResult Failed(string key, string message) => new()
    {
        Succeeded = false,
        ErrorKey = key,
        ErrorMessage = message
    };
}
