using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Sts.Web.Tests.Support;

namespace Sts.Web.Tests.Integration;

public class HomePageTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HomePageTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomePage_ShouldShowWelcomeMessageAndAnonymousLinks()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Welcome to STS", html);
        Assert.Contains("Register", html);
        Assert.Contains("Login", html);
        Assert.Contains("sts-site-nav__brand", html);
        Assert.Contains("sts-site-nav__list", html);
        Assert.Contains("sts-site-nav__panel", html);
        Assert.Contains("sts-home", html);
        Assert.Contains("sts-home__hero", html);
        Assert.Contains("sts-home__actions", html);
        Assert.Contains("data-sts-nav-toggle", html);
        Assert.Contains("/js/site-nav.js", html);
        Assert.DoesNotContain("data-bs-toggle=\"collapse\"", html);
    }

    [Fact]
    public async Task HomePage_AuthenticatedUser_ShouldRenderBemHooksForNavAndTickets()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var registerResponse = await Register(client, "home-hooks@example.com", "A1234", "Home Hooks", "Development");
        Assert.Equal(HttpStatusCode.Redirect, registerResponse.StatusCode);

        var createTicketResponse = await client.PostAsync("/Ticket/Add", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Subject"] = "Homepage Hook Ticket",
            ["Description"] = "Verifies the authenticated home markup contract.",
            ["Team"] = "Development",
            ["Status"] = "New"
        }));
        Assert.Equal(HttpStatusCode.Redirect, createTicketResponse.StatusCode);

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("sts-site-nav__action", html);
        Assert.Contains("sts-home__status-pill", html);
        Assert.Contains("sts-home__tickets", html);
        Assert.Contains("sts-home__tickets-heading", html);
        Assert.Contains("sts-home__timestamp", html);
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
