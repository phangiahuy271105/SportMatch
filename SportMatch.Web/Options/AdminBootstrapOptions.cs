namespace SportMatch.Web.Options;

public sealed class AdminBootstrapOptions
{
    public const string SectionName = "AdminBootstrap";
    public string Email { get; set; } = "admin@sportmatch.vn";
    public string PhoneNumber { get; set; } = "";
    public string FullName { get; set; } = "Quản trị SportMatch";
    public string Password { get; set; } = "";
}
