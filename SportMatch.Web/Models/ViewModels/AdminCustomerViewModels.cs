namespace SportMatch.Web.Models.ViewModels;

public sealed class AdminCustomerViewModel
{
    public required string Name { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public int BookingCount { get; init; }
    public int HostedMatchCount { get; init; }
    public int JoinRequestCount { get; init; }
    public DateTime LastActivity { get; init; }
}
