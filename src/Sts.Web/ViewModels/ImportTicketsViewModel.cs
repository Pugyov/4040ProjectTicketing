using System.ComponentModel.DataAnnotations;

namespace Sts.Web.ViewModels;

public class ImportTicketsViewModel
{
    [Required(ErrorMessage = "A JSON file is required.")]
    public IFormFile? File { get; set; }
}
