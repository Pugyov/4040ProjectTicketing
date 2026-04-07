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
        Assert.Contains("Unresolved Tickets by Team", html);
        Assert.True(html.IndexOf("Welcome to STS", StringComparison.Ordinal) < html.IndexOf("Unresolved Tickets by Team", StringComparison.Ordinal));
        Assert.Contains("Development", html);
        Assert.Contains("Support", html);
        Assert.Contains("Sales", html);
        Assert.Contains(">New<", html);
        Assert.Contains(">Open<", html);
        Assert.DoesNotContain(">Import<", html);
        Assert.DoesNotContain(">Edit<", html);
        Assert.DoesNotContain(">Delete<", html);
    }

    [Fact]
    public async Task HomePage_ShouldShowUnresolvedSummaryCounts_AndExcludeClosedTickets()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Unresolved Tickets by Team", html);
        Assert.Contains("<td>Development</td>", html);
        Assert.Contains("<td>4</td>", html);
        Assert.Contains("<td>6</td>", html);
        Assert.Contains("<td>Support</td>", html);
        Assert.Contains("<td>Sales</td>", html);
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
