using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sts.Web.Data;
using Sts.Web.Models;
using Sts.Web.Services;
using Sts.Web.Validation;
using Xunit;

namespace Sts.Web.Tests.Unit;

public class AccountServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldFail()
    {
        using var scope = BuildScope();
        var service = scope.ServiceProvider.GetRequiredService<IAccountService>();

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "duplicate-service@example.com",
            Password = "A1234",
            Name = "First",
            Team = Team.Development
        });

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "duplicate-service@example.com",
            Password = "A1234",
            Name = "Second",
            Team = Team.Support
        });

        Assert.False(result.Succeeded);
        Assert.True(result.Errors.ContainsKey("Email"));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ShouldFail()
    {
        using var scope = BuildScope();
        var service = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "missing-service@example.com",
            Password = "A1234"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Email", result.ErrorKey);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldFail()
    {
        using var scope = BuildScope();
        var service = scope.ServiceProvider.GetRequiredService<IAccountService>();

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "wrong-password-service@example.com",
            Password = "A1234",
            Name = "Service User",
            Team = Team.Sales
        });

        await service.LogoutAsync();

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "wrong-password-service@example.com",
            Password = "A9999"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Password", result.ErrorKey);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldSucceed()
    {
        using var scope = BuildScope();
        var service = scope.ServiceProvider.GetRequiredService<IAccountService>();

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "valid-service@example.com",
            Password = "A1234",
            Name = "Service User",
            Team = Team.Sales
        });

        await service.LogoutAsync();

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "valid-service@example.com",
            Password = "A1234"
        });

        Assert.True(result.Succeeded);
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase($"account-service-{Guid.NewGuid()}"));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 5;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddTransient<IPasswordValidator<ApplicationUser>, StartsWithLetterPasswordValidator>();
        services.AddScoped<IAccountService, AccountService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        return scope;
    }
}
