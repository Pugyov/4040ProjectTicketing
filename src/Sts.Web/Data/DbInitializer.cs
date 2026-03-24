using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sts.Web.Models;

namespace Sts.Web.Data;

public static class DbInitializer
{
    private const string DevUserEmail = "dev@sts.com";
    private const string DevUserPassword = "Dev123";

    public static async Task SeedDevUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<AppDbContext>();

        var devUser = await userManager.FindByEmailAsync(DevUserEmail);
        if (devUser is null)
        {
            devUser = new ApplicationUser
            {
                UserName = DevUserEmail,
                Email = DevUserEmail,
                Name = "Dev User",
                Team = Team.Development,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(devUser, DevUserPassword);
        }

        var hasSeededTickets = await dbContext.Tickets
            .AnyAsync(ticket => ticket.CreatedByUserId == devUser.Id);

        if (hasSeededTickets)
        {
            return;
        }

        var baseTimeUtc = new DateTime(2026, 3, 24, 8, 0, 0, DateTimeKind.Utc);

        dbContext.Tickets.AddRange(
            new Ticket
            {
                Subject = "Printer offline in room 204",
                Description = "The network printer is not responding to print jobs.",
                Team = Team.Development,
                Status = TicketStatus.New,
                CreatedByUserId = devUser.Id,
                CreatedAtUtc = baseTimeUtc.AddMinutes(5)
            },
            new Ticket
            {
                Subject = "Customer portal login timeout",
                Description = "Users report a timeout after submitting valid credentials.",
                Team = Team.Development,
                Status = TicketStatus.Open,
                CreatedByUserId = devUser.Id,
                CreatedAtUtc = baseTimeUtc.AddMinutes(15)
            },
            new Ticket
            {
                Subject = "Sales dashboard export issue",
                Description = "CSV export returns an empty file for filtered results.",
                Team = Team.Development,
                Status = TicketStatus.Open,
                CreatedByUserId = devUser.Id,
                CreatedAtUtc = baseTimeUtc.AddMinutes(25)
            },
            new Ticket
            {
                Subject = "Broken password reset email template",
                Description = "Reset emails are missing the action link for some users.",
                Team = Team.Development,
                Status = TicketStatus.New,
                CreatedByUserId = devUser.Id,
                CreatedAtUtc = baseTimeUtc.AddMinutes(35)
            },
            new Ticket
            {
                Subject = "Profile page validation message overlap",
                Description = "Validation text overlaps the save button on narrow screens.",
                Team = Team.Development,
                Status = TicketStatus.Closed,
                CreatedByUserId = devUser.Id,
                CreatedAtUtc = baseTimeUtc.AddMinutes(45)
            },
            new Ticket
            {
                Subject = "Reporting API returns 500 on large payloads",
                Description = "The endpoint fails when generating monthly summary reports.",
                Team = Team.Development,
                Status = TicketStatus.Open,
                CreatedByUserId = devUser.Id,
                CreatedAtUtc = baseTimeUtc.AddMinutes(55)
            });

        await dbContext.SaveChangesAsync();
    }
}
