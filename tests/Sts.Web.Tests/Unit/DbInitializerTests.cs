using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sts.Web.Data;
using Sts.Web.Models;
using Sts.Web.Validation;

namespace Sts.Web.Tests.Unit;

public class DbInitializerTests
{
    [Fact]
    public async Task SeedDevUserAsync_ShouldCreateDevUserAndSampleTickets_WithoutDuplicates()
    {
        using var scope = BuildScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedDevUserAsync(services);

        var devUser = await userManager.FindByEmailAsync("dev@sts.com");
        Assert.NotNull(devUser);
        Assert.True(await userManager.CheckPasswordAsync(devUser!, "Dev123"));

        var tickets = await dbContext.Tickets
            .Where(ticket => ticket.CreatedByUserId == devUser!.Id)
            .OrderBy(ticket => ticket.CreatedAtUtc)
            .ToListAsync();

        Assert.NotEmpty(tickets);
        Assert.All(tickets, ticket => Assert.Equal(Team.Development, ticket.Team));

        var firstCount = tickets.Count;

        await DbInitializer.SeedDevUserAsync(services);

        var secondCount = await dbContext.Tickets.CountAsync(ticket => ticket.CreatedByUserId == devUser.Id);
        Assert.Equal(firstCount, secondCount);
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"db-initializer-{Guid.NewGuid()}"));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 5;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddTransient<IPasswordValidator<ApplicationUser>, StartsWithLetterPasswordValidator>();

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }
}
