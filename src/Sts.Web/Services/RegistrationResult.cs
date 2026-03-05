namespace Sts.Web.Services;

public class RegistrationResult
{
    public bool Succeeded { get; init; }

    public Dictionary<string, List<string>> Errors { get; } = new();

    public static RegistrationResult Success() => new() { Succeeded = true };

    public static RegistrationResult Failed(params (string Key, string Error)[] errors)
    {
        var result = new RegistrationResult();
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
