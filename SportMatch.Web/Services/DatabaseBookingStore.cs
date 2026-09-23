using Microsoft.EntityFrameworkCore;
using System.Data;
using SportMatch.Web.Data;
using SportMatch.Web.Data.Entities;
using SportMatch.Web.Models.ViewModels;

namespace SportMatch.Web.Services;

public interface IBookingStore
{
    Task<IReadOnlyList<ScheduledBookingViewModel>> GetAllAsync();
    Task<ScheduledBookingViewModel?> GetByCodeAsync(string bookingCode);
    Task<bool> TryAddAsync(Booking booking);
    Task<bool> UpdateStatusAsync(string bookingCode, string status);
    Task<bool> ConfirmPaymentAsync(long transactionId, string? paymentCode, string content, decimal amount);
}

public sealed class DatabaseBookingStore(SportMatchDbContext db) : IBookingStore
{
    public async Task<IReadOnlyList<ScheduledBookingViewModel>> GetAllAsync()
    {
        var bookings = await db.Bookings.AsNoTracking().Include(x => x.SportCourt).ThenInclude(x => x!.VenueComplex)
            .OrderBy(x => x.BookingDate).ThenBy(x => x.StartTime)
            .ToListAsync();
        return bookings.Select(Map).ToList();
    }

    public async Task<ScheduledBookingViewModel?> GetByCodeAsync(string bookingCode)
    {
        var booking = await db.Bookings.AsNoTracking().Include(x => x.SportCourt).ThenInclude(x => x!.VenueComplex)
            .SingleOrDefaultAsync(x => x.BookingCode == bookingCode);
        return booking is null ? null : Map(booking);
    }

    public async Task<bool> TryAddAsync(Booking booking)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var sameDay = await db.Bookings.Where(x => x.SportCourtId == booking.SportCourtId && x.BookingDate == booking.BookingDate &&
            (x.Status == "Đã xác nhận" || x.Status == "Yêu cầu hủy" || (x.Status == "Chờ thanh toán" && x.HoldExpiresAtUtc > DateTime.UtcNow))).ToListAsync();
        var requestedStart = booking.StartTime.ToTimeSpan();
        var requestedEnd = requestedStart.Add(TimeSpan.FromMinutes(booking.SlotCount * 60));
        if (sameDay.Any(x => requestedStart < x.StartTime.ToTimeSpan().Add(TimeSpan.FromMinutes(x.SlotCount * 60)) && x.StartTime.ToTimeSpan() < requestedEnd))
            return false;
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(string bookingCode, string status)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(x => x.BookingCode == bookingCode);
        if (booking is null) return false;
        if (status == "Đã xác nhận" && (booking.Status != "Chờ thanh toán" || booking.HoldExpiresAtUtc <= DateTime.UtcNow)) return false;
        booking.Status = status;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConfirmPaymentAsync(long transactionId, string? paymentCode, string content, decimal amount)
    {
        if (await db.Bookings.AnyAsync(x => x.PaymentTransactionId == transactionId)) return true;
        var candidates = await db.Bookings.Where(x => x.Status == "Chờ thanh toán").ToListAsync();
        var booking = candidates.FirstOrDefault(x =>
            string.Equals(x.BookingCode, paymentCode, StringComparison.OrdinalIgnoreCase) ||
            content.Contains(x.BookingCode, StringComparison.OrdinalIgnoreCase));
        if (booking is null || amount < booking.DepositAmount || booking.HoldExpiresAtUtc < DateTime.UtcNow) return false;
        booking.Status = "Đã xác nhận";
        booking.PaymentTransactionId = transactionId;
        booking.PaidAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private static ScheduledBookingViewModel Map(Booking x) => new()
    {
        BookingCode = x.BookingCode,
        VenueName = x.SportCourt?.VenueComplex?.Name ?? "SportMatch",
        CourtName = x.SportCourt?.Name ?? string.Empty,
        SportName = x.SportCourt?.SportName ?? string.Empty,
        District = x.SportCourt?.VenueComplex?.District ?? string.Empty,
        CustomerName = x.CustomerName,
        PhoneNumber = x.PhoneNumber,
        Email = x.Email,
        BookingDate = x.BookingDate,
        StartTime = x.StartTime.ToString("HH:mm"),
        SlotCount = x.SlotCount,
        Deposit = x.DepositAmount,
        OpenForMatchmaking = x.OpenForMatchmaking,
        Status = x.Status == "Chờ thanh toán" && x.HoldExpiresAtUtc <= DateTime.UtcNow ? "Hết hạn" : x.Status,
        HoldExpiresAtUtc = x.HoldExpiresAtUtc,
        CancellationReason = x.CancellationReason,
        RefundPercent = x.RefundPercent,
        RefundAmount = x.RefundAmount,
        RefundStatus = x.RefundStatus,
        CanCancel = (x.Status == "Đã xác nhận" || (x.Status == "Chờ thanh toán" && x.HoldExpiresAtUtc > DateTime.UtcNow)) && BookingCancellationPolicy.StartAt(x) > DateTime.Now,
        EstimatedRefundPercent = BookingCancellationPolicy.RefundPercent(x)
    };
}
