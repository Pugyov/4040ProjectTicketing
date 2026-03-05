using Microsoft.AspNetCore.Identity;
using Sts.Web.Models;

namespace Sts.Web.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return RegistrationResult.Failed(("Email", "Email is already registered."));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            Team = request.Team,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors
                .Select(error =>
                {
                    var key = error.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                        ? "Password"
                        : error.Code.Contains("Email", StringComparison.OrdinalIgnoreCase)
                            ? "Email"
                            : string.Empty;

                    return (key, error.Description);
                })
                .ToArray();

            return RegistrationResult.Failed(errors);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RegistrationResult.Success();
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return LoginResult.Failed("Email", "No user exists with this email address.");
        }

        var signInResult = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return LoginResult.Failed("Password", "Incorrect password.");
        }

        return LoginResult.Success();
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}
