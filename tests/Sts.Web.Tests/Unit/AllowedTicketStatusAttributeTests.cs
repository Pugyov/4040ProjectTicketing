using System.ComponentModel.DataAnnotations;
using Sts.Web.Validation;

namespace Sts.Web.Tests.Unit;

public class AllowedTicketStatusAttributeTests
{
    private readonly AllowedTicketStatusAttribute _attribute = new();

    [Theory]
    [InlineData("New")]
    [InlineData("Open")]
    [InlineData("Closed")]
    public void AllowedTicketStatus_ShouldAcceptValidValues(string value)
    {
        var context = new ValidationContext(new object());
        var result = _attribute.GetValidationResult(value, context);

        Assert.Equal(ValidationResult.Success, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("InProgress")]
    [InlineData("Pending")]
    public void AllowedTicketStatus_ShouldRejectInvalidValues(string value)
    {
        var context = new ValidationContext(new object());
        var result = _attribute.GetValidationResult(value, context);

        Assert.NotEqual(ValidationResult.Success, result);
    }
}
