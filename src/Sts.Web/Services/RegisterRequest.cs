using Sts.Web.Models;

namespace Sts.Web.Services;

public class RegisterRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public Team Team { get; init; }
}
