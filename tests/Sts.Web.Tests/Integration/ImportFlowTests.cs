using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Sts.Web.Tests.Support;

namespace Sts.Web.Tests.Integration;

public class ImportFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ImportFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnonymousUser_ShouldBeRedirectedFromImportPage()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Ticket/Import");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task LoggedInUser_ShouldSeeImportPageAndFormatInstructions()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "import-view@example.com", "A1234", "Import View", "Development");

        var response = await client.GetAsync("/Ticket/Import");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Import Tickets", html);
        Assert.Contains("\"tickets\"", html);
        Assert.Contains("\"subject\"", html);
        Assert.Contains("\"status\"", html);
    }

    [Fact]
    public async Task Import_WithValidJson_ShouldPersistTickets_ForCurrentUsersTeam()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "import-success@example.com", "A1234", "Import Success", "Development");

        var payload = """
        {
          "tickets": [
            {
              "subject": "Imported Ticket Alpha",
              "description": "Imported from JSON",
              "status": "New"
            },
            {
              "subject": "Imported Ticket Beta",
              "description": "Imported from JSON",
              "status": "Open"
            }
          ]
        }
        """;

        var response = await ImportJson(client, payload, "tickets.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Successfully imported 2 tickets.", html);
        Assert.Contains("Imported Ticket Alpha", html);
        Assert.Contains("Imported Ticket Beta", html);
        Assert.Contains("<td>5</td>", html);
        Assert.Contains("<td>7</td>", html);
        Assert.DoesNotContain("Sales", html[(html.IndexOf("Imported Ticket Alpha", StringComparison.Ordinal))..Math.Min(html.Length, html.IndexOf("Imported Ticket Alpha", StringComparison.Ordinal) + 120)]);
    }

    [Fact]
    public async Task Import_WithInvalidJsonSchema_ShouldShowDetailedError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Register(client, "import-errors@example.com", "A1234", "Import Errors", "Support");

        var payload = """
        {
          "tickets": [
            {
              "subject": "Bad Import",
              "status": "InProgress"
            }
          ]
        }
        """;

        var response = await ImportJson(client, payload, "invalid-tickets.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("The uploaded file does not match the required JSON schema.", html);
        Assert.Contains("$.tickets[0].status must be one of: New, Open, Closed.", html);
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

    private static async Task<HttpResponseMessage> ImportJson(HttpClient client, string payload, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(payload));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "File", fileName);
        return await client.PostAsync("/Ticket/Import", content);
    }
}
