using System.ComponentModel.DataAnnotations;
using Sts.Web.Validation;

namespace Sts.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", ErrorMessage = "Email must include a valid domain (for example: name@example.com).")]
    [StringLength(254, ErrorMessage = "Email must be at most 254 characters long.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Password must be between 5 and 100 characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(Password), ErrorMessage = "Password and Confirm password must match.")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Confirm password must be between 5 and 100 characters long.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters long.")]
    public string Name { get; set; } = string.Empty;

    [AllowedTeam]
    public string Team { get; set; } = string.Empty;
}
