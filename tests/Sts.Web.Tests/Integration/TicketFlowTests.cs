using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Sts.Web.Data;
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

    [Fact]
    public async Task EditTicket_WithValidInput_ShouldRedirectHome_AndShowUpdatedTicket()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "edit-success@example.com", "A1234", "Edit Success", "Development");

        await CreateTicket(client, "Original Subject", "Original Description", "Development", "New");
        var ticketId = await GetTicketIdBySubjectAsync("Original Subject");

        var response = await client.PostAsync($"/Ticket/Edit/{ticketId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Subject"] = "Updated Subject",
            ["Description"] = "Updated Description",
            ["Status"] = "Open"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();
        Assert.Contains("Updated Subject", html);
        Assert.Contains("Open", html);
        Assert.DoesNotContain("Original Subject", html);
    }

    [Fact]
    public async Task EditTicket_WithInvalidStatus_ShouldShowValidationErrors()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "edit-errors@example.com", "A1234", "Edit Errors", "Development");

        await CreateTicket(client, "Needs Edit", "Original Description", "Development", "New");
        var ticketId = await GetTicketIdBySubjectAsync("Needs Edit");

        var response = await client.PostAsync($"/Ticket/Edit/{ticketId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Subject"] = "Needs Edit",
            ["Description"] = "Updated Description",
            ["Status"] = "InProgress"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Status must be one of: New, Open, Closed.", html);
    }

    [Fact]
    public async Task EditTicket_ForDifferentTeam_ShouldReturnNotFound()
    {
        var ownerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(ownerClient, "sales-owner@example.com", "A1234", "Sales Owner", "Sales");
        await CreateTicket(ownerClient, "Sales Only", "Hidden from Development", "Sales", "New");
        var ticketId = await GetTicketIdBySubjectAsync("Sales Only");

        var editorClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(editorClient, "dev-editor@example.com", "A1234", "Dev Editor", "Development");

        var response = await editorClient.GetAsync($"/Ticket/Edit/{ticketId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTicket_ShouldRemoveTicketAndRedirectHome()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "delete-success@example.com", "A1234", "Delete Success", "Development");

        await CreateTicket(client, "Delete Me", "Remove this ticket", "Development", "New");
        var ticketId = await GetTicketIdBySubjectAsync("Delete Me");

        var response = await client.PostAsync($"/Ticket/Delete/{ticketId}", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Delete Me", html);
    }

    [Fact]
    public async Task DeleteTicket_ForDifferentTeam_ShouldReturnNotFound()
    {
        var ownerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(ownerClient, "support-owner@example.com", "A1234", "Support Owner", "Support");
        await CreateTicket(ownerClient, "Support Private", "Not for Sales", "Support", "Open");
        var ticketId = await GetTicketIdBySubjectAsync("Support Private");

        var deleterClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(deleterClient, "sales-deleter@example.com", "A1234", "Sales Deleter", "Sales");

        var response = await deleterClient.PostAsync($"/Ticket/Delete/{ticketId}", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<int> GetTicketIdBySubjectAsync(string subject)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ticket = await dbContext.Tickets
            .OrderByDescending(item => item.Id)
            .FirstAsync(item => item.Subject == subject);

        return ticket.Id;
    }

    private static Task<HttpResponseMessage> CreateTicket(HttpClient client, string subject, string description, string team, string status)
    {
        return client.PostAsync("/Ticket/Add", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Subject"] = subject,
            ["Description"] = description,
            ["Team"] = team,
            ["Status"] = status
        }));
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
