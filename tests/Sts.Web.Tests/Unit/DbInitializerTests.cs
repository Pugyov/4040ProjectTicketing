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
    public async Task SeedDemoDataAsync_ShouldCreateDemoUsersAndSampleTickets_ForAllTeams_WithoutDuplicates()
    {
        using var scope = BuildScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<AppDbContext>();

        await DbInitializer.SeedDemoDataAsync(services);

        var expectedUsers = new (string Email, Team Team)[]
        {
            ("dev@sts.com", Team.Development),
            ("support@sts.com", Team.Support),
            ("sales@sts.com", Team.Sales)
        };

        foreach (var expectedUser in expectedUsers)
        {
            var user = await userManager.FindByEmailAsync(expectedUser.Email);
            Assert.NotNull(user);
            Assert.Equal(expectedUser.Team, user!.Team);
            Assert.True(await userManager.CheckPasswordAsync(user, DbInitializer.DemoUserPassword));

            var tickets = await dbContext.Tickets
                .Where(ticket => ticket.CreatedByUserId == user.Id)
                .OrderBy(ticket => ticket.CreatedAtUtc)
                .ToListAsync();

            Assert.Equal(6, tickets.Count);
            Assert.All(tickets, ticket => Assert.Equal(expectedUser.Team, ticket.Team));
        }

        var firstCount = await dbContext.Tickets.CountAsync();
        Assert.Equal(18, firstCount);

        await DbInitializer.SeedDemoDataAsync(services);

        var secondCount = await dbContext.Tickets.CountAsync();
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
