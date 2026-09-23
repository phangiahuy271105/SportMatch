namespace SportMatch.Web.Models.ViewModels;

public sealed class AdminPaymentLogViewModel
{
    public long TransactionId { get; init; }
    public DateTime ReceivedAt { get; init; }
    public decimal Amount { get; init; }
    public required string Content { get; init; }
    public bool Accepted { get; init; }
    public required string Result { get; init; }
}

public sealed class AdminAuditRowViewModel
{
    public DateTime OccurredAt { get; init; }
    public required string AdminName { get; init; }
    public required string Action { get; init; }
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public required string Details { get; init; }
}

public sealed class AdminOperationsViewModel
{
    public IReadOnlyList<AdminPaymentLogViewModel> PaymentLogs { get; init; } = [];
    public IReadOnlyList<AdminAuditRowViewModel> AuditLogs { get; init; } = [];
}
