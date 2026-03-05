using Microsoft.AspNetCore.Identity;

namespace Sts.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    public Team Team { get; set; }
}
