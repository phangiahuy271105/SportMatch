using SportMatch.Web.Data.Entities;

namespace SportMatch.Web.Services;

public static class BookingLifecyclePolicy
{
    public const string Pending = "Chờ thanh toán";
    public const string Confirmed = "Đã xác nhận";
    public const string Completed = "Đã hoàn thành";
    public const string Cancelled = "Đã hủy";
    public const string Expired = "Hết hạn";

    public static string DisplayStatus(Booking booking, DateTime? localNow = null)
    {
        var now = localNow ?? DateTime.Now;
        if (booking.Status == Pending && booking.HoldExpiresAtUtc <= now.ToUniversalTime()) return Expired;
        if (booking.Status == Confirmed && EndAtLocal(booking) <= now) return Completed;
        return booking.Status;
    }

    public static bool IsHistory(Booking booking, DateTime? localNow = null)
    {
        var status = DisplayStatus(booking, localNow);
        return status is Completed or Cancelled or Expired;
    }

    public static bool CanPurge(Booking booking, DateTime? localNow = null)
    {
        var status = DisplayStatus(booking, localNow);
        return status is Completed or Expired ||
               status == Cancelled && booking.RefundStatus != "Chờ hoàn tiền";
    }

    private static DateTime EndAtLocal(Booking booking)
    {
        var start = booking.BookingDate.ToDateTime(booking.StartTime);
        return start.AddMinutes(Math.Max(booking.SlotCount, 1) * 60);
    }
}
