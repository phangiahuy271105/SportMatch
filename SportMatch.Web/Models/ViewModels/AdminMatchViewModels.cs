namespace SportMatch.Web.Models.ViewModels;

public sealed class AdminMatchListViewModel
{
    public IReadOnlyList<AdminMatchRowViewModel> Matches { get; init; } = [];
}

public sealed class AdminMatchRowViewModel
{
    public required string MatchCode { get; init; }
    public required string HostName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string SportName { get; init; }
    public required string VenueName { get; init; }
    public DateOnly MatchDate { get; init; }
    public required string StartTime { get; init; }
    public required string Status { get; init; }
    public int RequestCount { get; init; }
    public int ConfirmedCount { get; init; }
    public IReadOnlyList<MatchRequestViewModel> Requests { get; init; } = [];
}
