using System.ComponentModel.DataAnnotations;
using Sts.Web.Models;

namespace Sts.Web.Validation;

public class AllowedTicketStatusAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string selectedStatus || string.IsNullOrWhiteSpace(selectedStatus))
        {
            return new ValidationResult("Status is required.");
        }

        if (!Enum.TryParse<TicketStatus>(selectedStatus, out var parsed) || !Enum.IsDefined(parsed))
        {
            return new ValidationResult("Status must be one of: New, Open, Closed.");
        }

        return ValidationResult.Success;
    }
}
