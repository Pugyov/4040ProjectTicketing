using System.ComponentModel.DataAnnotations;
using Sts.Web.Models;

namespace Sts.Web.Validation;

public class AllowedTeamAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string selectedTeam || string.IsNullOrWhiteSpace(selectedTeam))
        {
            return new ValidationResult("Team is required.");
        }

        if (!Enum.TryParse<Team>(selectedTeam, out var parsed) || !Enum.IsDefined(parsed))
        {
            return new ValidationResult("Team must be one of: Development, Support, Sales.");
        }

        return ValidationResult.Success;
    }
}
