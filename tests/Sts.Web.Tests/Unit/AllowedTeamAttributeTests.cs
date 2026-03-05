using System.ComponentModel.DataAnnotations;
using Sts.Web.Validation;
using Xunit;

namespace Sts.Web.Tests.Unit;

public class AllowedTeamAttributeTests
{
    private readonly AllowedTeamAttribute _attribute = new();

    [Theory]
    [InlineData("Development")]
    [InlineData("Support")]
    [InlineData("Sales")]
    public void AllowedTeam_ShouldAcceptValidValues(string value)
    {
        var context = new ValidationContext(new object());
        var result = _attribute.GetValidationResult(value, context);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Marketing")]
    [InlineData("dev")]
    public void AllowedTeam_ShouldRejectInvalidValues(string value)
    {
        var context = new ValidationContext(new object());
        var result = _attribute.GetValidationResult(value, context);

        Assert.NotEqual(ValidationResult.Success, result);
    }
}
