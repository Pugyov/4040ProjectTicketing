using System.ComponentModel.DataAnnotations;
using Sts.Web.Validation;

namespace Sts.Web.ViewModels;

public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject must be at most 200 characters long.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description must be at most 2000 characters long.")]
    public string? Description { get; set; }

    [AllowedTeam]
    public string Team { get; set; } = string.Empty;

    [AllowedTicketStatus]
    public string Status { get; set; } = string.Empty;
}
