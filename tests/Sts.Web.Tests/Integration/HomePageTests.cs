using System.Net;
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
    }
}
