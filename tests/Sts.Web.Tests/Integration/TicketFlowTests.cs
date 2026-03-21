using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Sts.Web.Tests.Support;

namespace Sts.Web.Tests.Integration;

public class TicketFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public TicketFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousUser_ShouldBeRedirectedFromAddTicketPage()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Ticket/Add");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task LoggedInUser_ShouldSeeAddNewLink()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await Register(client, "nav-user@example.com", "A1234", "Nav User", "Development");

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();

        Assert.Contains("Add New", html);
    }

    [Fact]
    public async Task AddTicket_WithInvalidStatus_ShouldShowValidationErrors()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "ticket-errors@example.com", "A1234", "Ticket Errors", "Development");

        var response = await client.PostAsync("/Ticket/Add", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Subject"] = "Printer offline",
            ["Description"] = "",
            ["Team"] = "Development",
            ["Status"] = "InProgress"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Status must be one of: New, Open, Closed.", html);
    }

    [Fact]
    public async Task AddTicket_WithValidInput_ShouldRedirectHome()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "ticket-success@example.com", "A1234", "Ticket Success", "Development");

        var response = await client.PostAsync("/Ticket/Add", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Subject"] = "Printer offline",
            ["Description"] = "The office printer is not responding.",
            ["Team"] = "Development",
            ["Status"] = "New"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();
        Assert.Contains("Printer offline", html);
        Assert.Contains("Development", html);
    }

    [Fact]
    public async Task HomePage_ShouldShowOnlyCurrentUsersTeamTickets_CappedAtTen_OrderedNewestFirst()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "team-view@example.com", "A1234", "Team View", "Development");

        for (var index = 1; index <= 12; index++)
        {
            var create = await client.PostAsync("/Ticket/Add", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Subject"] = $"Dev Ticket {index:00}",
                ["Description"] = $"Description {index:00}",
                ["Team"] = "Development",
                ["Status"] = "New"
            }));

            Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        }

        var otherTeam = await client.PostAsync("/Ticket/Add", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Subject"] = "Sales Ticket",
            ["Description"] = "Should not appear.",
            ["Team"] = "Sales",
            ["Status"] = "Open"
        }));
        Assert.Equal(HttpStatusCode.Redirect, otherTeam.StatusCode);

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();

        Assert.Contains("Dev Ticket 12", html);
        Assert.Contains("Dev Ticket 03", html);
        Assert.DoesNotContain("Dev Ticket 02", html);
        Assert.DoesNotContain("Dev Ticket 01", html);
        Assert.DoesNotContain("Sales Ticket", html);
        Assert.True(html.IndexOf("Dev Ticket 12", StringComparison.Ordinal) < html.IndexOf("Dev Ticket 11", StringComparison.Ordinal));
        Assert.True(html.IndexOf("Dev Ticket 11", StringComparison.Ordinal) < html.IndexOf("Dev Ticket 10", StringComparison.Ordinal));
    }

    private static Task<HttpResponseMessage> Register(HttpClient client, string email, string password, string name, string team)
    {
        return client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["ConfirmPassword"] = password,
            ["Name"] = name,
            ["Team"] = team
        }));
    }
}
