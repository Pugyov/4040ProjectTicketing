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

            Assert.Equal(12, tickets.Count);
            Assert.All(tickets, ticket => Assert.Equal(expectedUser.Team, ticket.Team));
        }

        var firstCount = await dbContext.Tickets.CountAsync();
        Assert.Equal(36, firstCount);

        await DbInitializer.SeedDemoDataAsync(services);

        var secondCount = await dbContext.Tickets.CountAsync();
        Assert.Equal(firstCount, secondCount);
    }

    [Fact]
    public async Task SeedDemoDataAsync_ShouldTopUpExistingDemoUsersToFullTicketSet()
    {
        using var scope = BuildScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<AppDbContext>();

        var devUser = new ApplicationUser
        {
            UserName = "dev@sts.com",
            Email = "dev@sts.com",
            Name = "Dev User",
            Team = Team.Development,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(devUser, DbInitializer.DemoUserPassword);
        Assert.True(result.Succeeded);

        for (var index = 1; index <= 6; index++)
        {
            dbContext.Tickets.Add(new Ticket
            {
                Subject = $"Legacy Dev Ticket {index:00}",
                Description = $"Legacy seeded ticket {index:00}",
                Team = Team.Development,
                Status = TicketStatus.Open,
                CreatedByUserId = devUser.Id,
                CreatedAtUtc = new DateTime(2026, 3, 20, 8, 0, 0, DateTimeKind.Utc).AddMinutes(index)
            });
        }

        await dbContext.SaveChangesAsync();

        await DbInitializer.SeedDemoDataAsync(services);

        Assert.Equal(12, await dbContext.Tickets.CountAsync(ticket => ticket.CreatedByUserId == devUser.Id));
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
