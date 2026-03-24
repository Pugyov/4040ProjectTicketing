using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sts.Web.Models;

namespace Sts.Web.Data;

public static class DbInitializer
{
    public const string DemoUserPassword = "Dev123";

    private static readonly DemoUserSeed[] DemoUsers =
    {
        new("dev@sts.com", "Dev User", Team.Development),
        new("support@sts.com", "Support User", Team.Support),
        new("sales@sts.com", "Sales User", Team.Sales)
    };

    public static async Task SeedDemoDataAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = services.GetRequiredService<AppDbContext>();

        foreach (var demoUserSeed in DemoUsers)
        {
            var user = await userManager.FindByEmailAsync(demoUserSeed.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = demoUserSeed.Email,
                    Email = demoUserSeed.Email,
                    Name = demoUserSeed.Name,
                    Team = demoUserSeed.Team,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, DemoUserPassword);
            }

            var hasSeededTickets = await dbContext.Tickets
                .AnyAsync(ticket => ticket.CreatedByUserId == user.Id);

            if (hasSeededTickets)
            {
                continue;
            }

            dbContext.Tickets.AddRange(BuildTicketsForUser(user));
        }

        await dbContext.SaveChangesAsync();
    }

    private static IEnumerable<Ticket> BuildTicketsForUser(ApplicationUser user)
    {
        var baseTimeUtc = new DateTime(2026, 3, 24, 8, 0, 0, DateTimeKind.Utc)
            .AddHours((int)user.Team - 1);

        var ticketSeeds = user.Team switch
        {
            Team.Development => new[]
            {
                new TicketSeed("Printer offline in room 204", "The network printer is not responding to print jobs.", TicketStatus.New),
                new TicketSeed("Customer portal login timeout", "Users report a timeout after submitting valid credentials.", TicketStatus.Open),
                new TicketSeed("Sales dashboard export issue", "CSV export returns an empty file for filtered results.", TicketStatus.Open),
                new TicketSeed("Broken password reset email template", "Reset emails are missing the action link for some users.", TicketStatus.New),
                new TicketSeed("Profile page validation message overlap", "Validation text overlaps the save button on narrow screens.", TicketStatus.Closed),
                new TicketSeed("Reporting API returns 500 on large payloads", "The endpoint fails when generating monthly summary reports.", TicketStatus.Open)
            },
            Team.Support => new[]
            {
                new TicketSeed("VPN access request backlog", "Several new employees still cannot connect to the company VPN.", TicketStatus.New),
                new TicketSeed("Shared mailbox sync issue", "The team inbox stops syncing after Outlook is reopened.", TicketStatus.Open),
                new TicketSeed("Laptop battery replacement follow-up", "Two devices are still waiting for hardware service.", TicketStatus.Open),
                new TicketSeed("Helpdesk phone queue misrouting", "Calls for billing are being routed to general support.", TicketStatus.New),
                new TicketSeed("Office Wi-Fi guest password update", "Reception needs the new guest credentials posted.", TicketStatus.Closed),
                new TicketSeed("Adobe license activation problem", "A designer cannot activate the assigned Creative Cloud seat.", TicketStatus.Open)
            },
            Team.Sales => new[]
            {
                new TicketSeed("Lead import duplicate records", "The latest CSV import created duplicated contacts in the CRM.", TicketStatus.New),
                new TicketSeed("Quote approval email delay", "Approval notifications arrive several minutes late.", TicketStatus.Open),
                new TicketSeed("Pipeline dashboard filter mismatch", "Quarterly filters show totals that do not match the raw report.", TicketStatus.Open),
                new TicketSeed("Discount form missing regional options", "The discount request form does not list all sales regions.", TicketStatus.New),
                new TicketSeed("Expired campaign list cleanup", "Old campaign entries were removed from the weekly dashboard.", TicketStatus.Closed),
                new TicketSeed("Opportunity stage audit request", "Management requested a review of stale opportunities in negotiation.", TicketStatus.Open)
            },
            _ => Array.Empty<TicketSeed>()
        };

        return ticketSeeds.Select((ticket, index) => new Ticket
        {
            Subject = ticket.Subject,
            Description = ticket.Description,
            Team = user.Team,
            Status = ticket.Status,
            CreatedByUserId = user.Id,
            CreatedAtUtc = baseTimeUtc.AddMinutes((index + 1) * 10)
        });
    }

    private sealed record DemoUserSeed(string Email, string Name, Team Team);

    private sealed record TicketSeed(string Subject, string Description, TicketStatus Status);
}
