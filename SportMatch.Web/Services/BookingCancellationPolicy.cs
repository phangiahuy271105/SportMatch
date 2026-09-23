using SportMatch.Web.Data.Entities;

namespace SportMatch.Web.Services;

public static class BookingCancellationPolicy
{
    public static DateTime StartAt(Booking booking) => booking.BookingDate.ToDateTime(booking.StartTime);

    public static int RefundPercent(Booking booking, DateTime? requestedAt = null)
    {
        var requestedLocal = (requestedAt ?? DateTime.UtcNow).ToLocalTime();
        var remaining = StartAt(booking) - requestedLocal;
        if (remaining >= TimeSpan.FromHours(24)) return 100;
        if (remaining >= TimeSpan.FromHours(12)) return 50;
        return 0;
    }
}
