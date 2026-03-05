using Microsoft.AspNetCore.Identity;
using Sts.Web.Models;

namespace Sts.Web.Data;

public static class DbInitializer
{
    public static async Task SeedDevUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string email = "dev.user@sts.local";

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return;
        }

        var devUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Dev User",
            Team = Team.Development,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(devUser, "A1234");
    }
}
