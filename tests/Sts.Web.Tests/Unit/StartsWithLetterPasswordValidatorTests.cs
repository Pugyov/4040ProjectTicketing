using Microsoft.AspNetCore.Identity;
using Sts.Web.Models;
using Sts.Web.Validation;
using Xunit;

namespace Sts.Web.Tests.Unit;

public class StartsWithLetterPasswordValidatorTests
{
    private readonly StartsWithLetterPasswordValidator _validator = new();

    [Theory]
    [InlineData("A1234")]
    [InlineData("b9999")]
    public async Task ValidateAsync_WithLetterFirstCharacter_ShouldSucceed(string password)
    {
        var result = await _validator.ValidateAsync(null!, new ApplicationUser(), password);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("1abcd")]
    [InlineData("_Abcd")]
    [InlineData("")]
    public async Task ValidateAsync_WithoutLetterFirstCharacter_ShouldFail(string password)
    {
        var result = await _validator.ValidateAsync(null!, new ApplicationUser(), password);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordStartsWithLetter");
    }
}
