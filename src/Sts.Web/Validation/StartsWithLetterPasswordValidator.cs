using Microsoft.AspNetCore.Identity;
using Sts.Web.Models;

namespace Sts.Web.Validation;

public class StartsWithLetterPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || !char.IsLetter(password[0]))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordStartsWithLetter",
                Description = "Password must start with a letter."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
