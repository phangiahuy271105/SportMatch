namespace SportMatch.Web.Models.ViewModels;

public sealed class ScheduleIndexViewModel
{
    public IReadOnlyList<ScheduledBookingViewModel> Bookings { get; init; } = [];
}

public sealed class ScheduledBookingViewModel
{
    public required string BookingCode { get; init; }
    public required string VenueName { get; init; }
    public required string CourtName { get; init; }
    public required string SportName { get; init; }
    public required string District { get; init; }
    public required string CustomerName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public DateOnly BookingDate { get; init; }
    public required string StartTime { get; init; }
    public int SlotCount { get; init; }
    public decimal Deposit { get; init; }
    public bool OpenForMatchmaking { get; init; }
    public required string Status { get; init; }
    public DateTime HoldExpiresAtUtc { get; init; }
    public string? CancellationReason { get; init; }
    public int? RefundPercent { get; init; }
    public decimal RefundAmount { get; init; }
    public string? RefundStatus { get; init; }
    public bool CanCancel { get; init; }
    public int EstimatedRefundPercent { get; init; }
}

public sealed class PaymentViewModel
{
    public required ScheduledBookingViewModel Booking { get; init; }
    public required string QrCodeUrl { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountName { get; init; }
    public required string TransferContent { get; init; }
    public string? ZaloUrl { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public sealed class AdminDashboardViewModel
{
    public int CourtCount { get; init; }
    public int CustomerCount { get; init; }
    public IReadOnlyList<ScheduledBookingViewModel> Bookings { get; init; } = [];
    public int VenueCount { get; init; }
    public int TotalBookings => Bookings.Count;
    public int PendingBookings => Bookings.Count(item => item.Status == "Chờ thanh toán");
    public int ConfirmedBookings => Bookings.Count(item => item.Status == "Đã xác nhận");
    public int CancellationRequests => Bookings.Count(item => item.Status == "Yêu cầu hủy");
    public decimal TotalDeposits => Bookings.Where(item => item.Status is "Đã xác nhận" or "Yêu cầu hủy" or "Đã hủy").Sum(item => item.Deposit - (item.RefundStatus == "Đã hoàn tiền" ? item.RefundAmount : 0));
    public bool PaymentWebhookConfigured { get; init; }
}

public sealed class UpdateBookingStatusViewModel
{
    [System.ComponentModel.DataAnnotations.Required]
    public string BookingCode { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression("^(Đã xác nhận|Đã hủy)$")]
    public string Status { get; set; } = string.Empty;
}
