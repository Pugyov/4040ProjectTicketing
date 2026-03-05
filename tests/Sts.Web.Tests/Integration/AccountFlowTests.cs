using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Sts.Web.Tests.Support;

namespace Sts.Web.Tests.Integration;

public class AccountFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AccountFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithInvalidTeam_ShouldShowValidationError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "bad-team@example.com",
            ["Password"] = "A1234",
            ["ConfirmPassword"] = "A1234",
            ["Name"] = "Bad Team",
            ["Team"] = "Marketing"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Team must be one of: Development, Support, Sales.", html);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldShowValidationError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "asd@m",
            ["Password"] = "A1234",
            ["ConfirmPassword"] = "A1234",
            ["Name"] = "Invalid Email",
            ["Team"] = "Development"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email must include a valid domain (for example: name@example.com).", html);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldShowError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var first = await Register(client, "duplicate@example.com", "A1234", "First User", "Development");
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);

        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));

        var second = await Register(client, "duplicate@example.com", "A1234", "Second User", "Support");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var html = await second.Content.ReadAsStringAsync();
        Assert.Contains("Email is already registered.", html);
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ShouldShowError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await Register(client, "bad-password@example.com", "11234", "Bad Password", "Sales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Password must start with a letter.", html);
    }

    [Fact]
    public async Task Register_WithValidInput_ShouldAuthenticateUserAndShowLogout()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await Register(client, "valid@example.com", "A1234", "Valid User", "Development");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();
        Assert.Contains("Logout", html);
        Assert.DoesNotContain(">Register<", html);
        Assert.DoesNotContain(">Login<", html);
    }

    [Fact]
    public async Task Register_WithPasswordMismatch_ShouldShowValidationError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "mismatch@example.com",
            ["Password"] = "A1234",
            ["ConfirmPassword"] = "A9999",
            ["Name"] = "Mismatch User",
            ["Team"] = "Support"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Password and Confirm password must match.", html);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldShowSpecificError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "missing@example.com",
            ["Password"] = "A1234"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("No user exists with this email address.", html);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ShouldShowValidationError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "asd@m",
            ["Password"] = "A1234"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Email must include a valid domain (for example: name@example.com).", html);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldShowSpecificError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var registered = await Register(client, "wrong-pass@example.com", "A1234", "Wrong Pass", "Support");
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);

        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));

        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "wrong-pass@example.com",
            ["Password"] = "A9999"
        }));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var html = await login.Content.ReadAsStringAsync();
        Assert.Contains("Incorrect password.", html);
    }

    [Fact]
    public async Task Login_Get_ShouldEmitClientValidationAttributesForRequiredFields()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var login = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var html = await login.Content.ReadAsStringAsync();
        Assert.Contains("data-val-required=\"Email is required.\"", html);
        Assert.Contains("data-val-required=\"Password is required.\"", html);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldRedirectHome()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var registered = await Register(client, "good-login@example.com", "A1234", "Good Login", "Sales");
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);

        await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));

        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "good-login@example.com",
            ["Password"] = "A1234"
        }));

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Logout_ShouldSignOutAndRedirectHome()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var registered = await Register(client, "logout@example.com", "A1234", "Logout User", "Development");
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);

        var logout = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/", logout.Headers.Location?.ToString());

        var home = await client.GetAsync("/");
        var html = await home.Content.ReadAsStringAsync();
        Assert.Contains("Register", html);
        Assert.Contains("Login", html);
        Assert.DoesNotContain("Logout", html);
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
