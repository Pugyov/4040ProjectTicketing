using Sts.Web.ViewModels;

namespace Sts.Web.Services;

public interface IAccountService
{
    Task<RegistrationResult> RegisterAsync(RegisterRequest request);

    Task<LoginResult> LoginAsync(LoginRequest request);

    Task LogoutAsync();
}
