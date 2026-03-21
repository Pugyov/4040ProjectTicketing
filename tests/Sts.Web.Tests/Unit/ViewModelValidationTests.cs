using System.ComponentModel.DataAnnotations;
using Sts.Web.ViewModels;

namespace Sts.Web.Tests.Unit;

public class ViewModelValidationTests
{
    [Fact]
    public void RegisterViewModel_WithInvalidEmail_ShouldFailValidation()
    {
        var model = ValidRegisterModel();
        model.Email = "asd@m";

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Email must include a valid domain (for example: name@example.com).");
    }

    [Fact]
    public void RegisterViewModel_WithEmailLongerThan254_ShouldFailValidation()
    {
        var model = ValidRegisterModel();
        model.Email = $"{new string('a', 245)}@example.com";

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Email must be at most 254 characters long.");
    }

    [Fact]
    public void RegisterViewModel_WithMismatchedConfirmPassword_ShouldFailValidation()
    {
        var model = ValidRegisterModel();
        model.ConfirmPassword = "A9999";

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Password and Confirm password must match.");
    }

    [Fact]
    public void RegisterViewModel_WithShortName_ShouldFailValidation()
    {
        var model = ValidRegisterModel();
        model.Name = "A";

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Name must be between 2 and 100 characters long.");
    }

    [Fact]
    public void LoginViewModel_WithMissingFields_ShouldFailValidation()
    {
        var model = new LoginViewModel();

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Email is required.");
        Assert.Contains(results, r => r.ErrorMessage == "Password is required.");
    }

    [Fact]
    public void LoginViewModel_WithInvalidEmail_ShouldFailValidation()
    {
        var model = new LoginViewModel
        {
            Email = "asd@m",
            Password = "A1234"
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Email must include a valid domain (for example: name@example.com).");
    }

    [Fact]
    public void CreateTicketViewModel_WithMissingRequiredFields_ShouldFailValidation()
    {
        var model = new CreateTicketViewModel();

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Subject is required.");
        Assert.Contains(results, r => r.ErrorMessage == "Team is required.");
        Assert.Contains(results, r => r.ErrorMessage == "Status is required.");
    }

    [Fact]
    public void CreateTicketViewModel_WithInvalidStatus_ShouldFailValidation()
    {
        var model = new CreateTicketViewModel
        {
            Subject = "Broken import",
            Team = "Development",
            Status = "InProgress"
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.ErrorMessage == "Status must be one of: New, Open, Closed.");
    }

    private static RegisterViewModel ValidRegisterModel() => new()
    {
        Email = "valid@example.com",
        Password = "A1234",
        ConfirmPassword = "A1234",
        Name = "Valid User",
        Team = "Development"
    };

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
