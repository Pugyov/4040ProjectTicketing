using Microsoft.EntityFrameworkCore;
using Sts.Web.Data;
using Sts.Web.Models;
using Sts.Web.Services;

namespace Sts.Web.Tests.Unit;

public class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldPersistTicket()
    {
        await using var dbContext = BuildDbContext();
        var service = new TicketService(dbContext);

        var result = await service.CreateAsync(new CreateTicketRequest
        {
            Subject = "Broken build",
            Description = "Build fails in CI.",
            Team = Team.Development,
            Status = TicketStatus.Open,
            CreatedByUserId = "user-1"
        });

        Assert.True(result.Succeeded);
        var ticket = await dbContext.Tickets.SingleAsync();
        Assert.Equal("Broken build", ticket.Subject);
        Assert.Equal(Team.Development, ticket.Team);
        Assert.Equal(TicketStatus.Open, ticket.Status);
    }

    [Fact]
    public async Task GetRecentTicketsForTeamAsync_ShouldFilterOrderAndLimit()
    {
        await using var dbContext = BuildDbContext();

        for (var index = 1; index <= 12; index++)
        {
            dbContext.Tickets.Add(new Ticket
            {
                Subject = $"Dev Ticket {index:00}",
                Team = Team.Development,
                Status = TicketStatus.New,
                CreatedByUserId = "user-1",
                CreatedAtUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc).AddMinutes(index)
            });
        }

        dbContext.Tickets.Add(new Ticket
        {
            Subject = "Support Ticket",
            Team = Team.Support,
            Status = TicketStatus.New,
            CreatedByUserId = "user-2",
            CreatedAtUtc = new DateTime(2026, 3, 2, 12, 0, 0, DateTimeKind.Utc)
        });

        await dbContext.SaveChangesAsync();
        var service = new TicketService(dbContext);

        var tickets = await service.GetRecentTicketsForTeamAsync(Team.Development, 10);

        Assert.Equal(10, tickets.Count);
        Assert.Equal("Dev Ticket 12", tickets[0].Subject);
        Assert.Equal("Dev Ticket 03", tickets[^1].Subject);
        Assert.DoesNotContain(tickets, ticket => ticket.Subject == "Support Ticket");
    }

    private static AppDbContext BuildDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ticket-service-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
