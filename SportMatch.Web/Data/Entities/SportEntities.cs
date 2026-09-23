using System.ComponentModel.DataAnnotations;

namespace SportMatch.Web.Data.Entities;

public sealed class VenueComplex
{
    public int Id { get; set; }
    [MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(250)] public string Address { get; set; } = string.Empty;
    [MaxLength(50)] public string District { get; set; } = string.Empty;
    [MaxLength(20)] public string PhoneNumber { get; set; } = string.Empty;
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    [MaxLength(500)] public string Amenities { get; set; } = string.Empty;
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [MaxLength(300)] public string? ImagePath { get; set; }
    public ICollection<SportCourt> Courts { get; set; } = [];
}

public sealed class SportCourt
{
    public int Id { get; set; }
    public int VenueComplexId { get; set; }
    public VenueComplex? VenueComplex { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string SportName { get; set; } = string.Empty;
    [MaxLength(30)] public string CourtType { get; set; } = string.Empty;
    public decimal OffPeakPrice { get; set; }
    public decimal PeakPrice { get; set; }
    public ICollection<CourtTimeSlot> TimeSlots { get; set; } = [];
}

public sealed class CourtTimeSlot
{
    public int Id { get; set; }
    public int SportCourtId { get; set; }
    public SportCourt? SportCourt { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public bool IsActive { get; set; } = true;
}

public sealed class Booking
{
    [MaxLength(450)] public string? UserId { get; set; }
    public int Id { get; set; }
    [MaxLength(20)] public string BookingCode { get; set; } = string.Empty;
    public int SportCourtId { get; set; }
    public SportCourt? SportCourt { get; set; }
    [MaxLength(80)] public string CustomerName { get; set; } = string.Empty;
    [MaxLength(20)] public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(150)] public string? Email { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public int SlotCount { get; set; } = 1;
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public bool OpenForMatchmaking { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Chờ thanh toán";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime HoldExpiresAtUtc { get; set; }
    public long? PaymentTransactionId { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? CancellationRequestedAtUtc { get; set; }
    [MaxLength(500)] public string? CancellationReason { get; set; }
    public int? RefundPercent { get; set; }
    public decimal RefundAmount { get; set; }
    [MaxLength(30)] public string? RefundStatus { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}

public sealed class MatchPost
{
    public int Id { get; set; }
    [MaxLength(20)] public string MatchCode { get; set; } = string.Empty;
    [MaxLength(80)] public string HostName { get; set; } = string.Empty;
    [MaxLength(20)] public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(30)] public string Sport { get; set; } = string.Empty;
    [MaxLength(50)] public string SportName { get; set; } = string.Empty;
    [MaxLength(150)] public string VenueName { get; set; } = string.Empty;
    [MaxLength(50)] public string District { get; set; } = "TP.HCM";
    public DateOnly MatchDate { get; set; }
    public TimeOnly StartTime { get; set; }
    [MaxLength(30)] public string Level { get; set; } = string.Empty;
    public int NeededPlayers { get; set; }
    public decimal CostPerPerson { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Đang tuyển";
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<MatchJoinRequest> JoinRequests { get; set; } = [];
}

public sealed class MatchJoinRequest
{
    public int Id { get; set; }
    [MaxLength(20)] public string RequestCode { get; set; } = string.Empty;
    public int MatchPostId { get; set; }
    public MatchPost? MatchPost { get; set; }
    [MaxLength(80)] public string ApplicantName { get; set; } = string.Empty;
    [MaxLength(20)] public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(20)] public string Status { get; set; } = "Chờ duyệt";
    public decimal DepositAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? HoldExpiresAtUtc { get; set; }
    public long? PaymentTransactionId { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}

public sealed class PaymentWebhookLog
{
    public long Id { get; set; }
    public long TransactionId { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    [MaxLength(30)] public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    [MaxLength(500)] public string Content { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    [MaxLength(40)] public string Result { get; set; } = string.Empty;
}

public sealed class AdminAuditLog
{
    public long Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    [MaxLength(150)] public string AdminName { get; set; } = string.Empty;
    [MaxLength(80)] public string Action { get; set; } = string.Empty;
    [MaxLength(50)] public string EntityType { get; set; } = string.Empty;
    [MaxLength(100)] public string EntityId { get; set; } = string.Empty;
    [MaxLength(500)] public string Details { get; set; } = string.Empty;
}
