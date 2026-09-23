using Microsoft.AspNetCore.Mvc;
using SportMatch.Web.Models.ViewModels;
using SportMatch.Web.Services;
using SportMatch.Web.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace SportMatch.Web.Controllers;

public sealed class ScheduleController : Controller
{
    private readonly IBookingStore _bookingStore;
    private readonly SportMatchDbContext _db;
    private readonly BookingAccess _access;
    private readonly MatchAccess _matchAccess;

    public ScheduleController(IBookingStore bookingStore, SportMatchDbContext db, BookingAccess access, MatchAccess matchAccess)
    {
        _bookingStore = bookingStore;
        _db = db;
        _access = access;
        _matchAccess = matchAccess;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? code)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var codes = _access.Codes(HttpContext);
        var query = _db.Bookings.AsNoTracking().Where(x => codes.Contains(x.BookingCode) || (userId != null && x.UserId == userId));
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(x => x.BookingCode == code);
        var ownedCodes = await query.OrderByDescending(x => x.BookingDate).Select(x => x.BookingCode).ToListAsync();
        var bookings = new List<ScheduledBookingViewModel>();
        foreach (var ownedCode in ownedCodes)
        {
            var booking = await _bookingStore.GetByCodeAsync(ownedCode);
            if (booking is not null)
            {
                bookings.Add(booking);
            }
        }
        _matchAccess.RememberHosts(HttpContext, bookings.Where(x => !string.IsNullOrWhiteSpace(x.MatchCode)).Select(x => x.MatchCode!));
        return View(new ScheduleIndexViewModel { Bookings = bookings });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> Lookup(string code, string phone)
    {
        var booking = await _db.Bookings.AsNoTracking().SingleOrDefaultAsync(x => x.BookingCode == code && x.PhoneNumber == phone);
        if (booking is null) TempData["LookupError"] = "Mã đặt sân hoặc số điện thoại không đúng.";
        else _access.Remember(HttpContext, booking.BookingCode);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Cancel(string bookingCode, string reason)
    {
        if (!await _access.CanRead(HttpContext, bookingCode)) return NotFound();
        var booking = await _db.Bookings.Include(x => x.MatchPost).SingleOrDefaultAsync(x => x.BookingCode == bookingCode);
        if (booking is null) return NotFound();
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
        {
            TempData["ScheduleError"] = "Vui lòng nhập lý do hủy rõ ràng.";
            return RedirectToAction(nameof(Index));
        }
        if (!BookingCancellationPolicy.CanCustomerCancel(booking) || booking.Status is not ("Đã xác nhận" or "Chờ thanh toán"))
        {
            TempData["ScheduleError"] = "Chỉ có thể hủy trước giờ thi đấu ít nhất 12 tiếng.";
            return RedirectToAction(nameof(Index));
        }
        booking.CancellationReason = reason.Trim();
        booking.CancellationRequestedAtUtc = DateTime.UtcNow;
        booking.RefundPercent = BookingCancellationPolicy.RefundPercent(booking, booking.CancellationRequestedAtUtc);
        if (booking.Status == "Chờ thanh toán")
        {
            booking.Status = "Đã hủy";
            booking.RefundAmount = 0;
            booking.RefundStatus = "Không phát sinh cọc";
            booking.CancelledAtUtc = DateTime.UtcNow;
            TempData["ScheduleMessage"] = "Đã hủy lịch chưa thanh toán.";
        }
        else
        {
            booking.Status = "Yêu cầu hủy";
            TempData["ScheduleMessage"] = $"Đã gửi yêu cầu hủy. Mức hoàn dự kiến {booking.RefundPercent}%.";
        }
        if (booking.MatchPost is not null && booking.Status == "Đã hủy") booking.MatchPost.Status = "Đã đóng";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
