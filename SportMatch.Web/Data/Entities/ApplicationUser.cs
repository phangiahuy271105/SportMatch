using Microsoft.AspNetCore.Identity;

namespace SportMatch.Web.Data.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public decimal TrustScore { get; set; } = 5m;
}
