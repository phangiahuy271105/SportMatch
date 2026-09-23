using SportMatch.Web.Data.Entities;
using SportMatch.Web.Services;

var localStart = new DateTime(2030, 6, 20, 19, 0, 0, DateTimeKind.Local);
var booking = new Booking
{
    BookingDate = DateOnly.FromDateTime(localStart),
    StartTime = TimeOnly.FromDateTime(localStart)
};

AssertEqual(100, BookingCancellationPolicy.RefundPercent(booking, localStart.AddHours(-25).ToUniversalTime()), "Hoàn 100% trước trên 24 giờ");
AssertEqual(50, BookingCancellationPolicy.RefundPercent(booking, localStart.AddHours(-18).ToUniversalTime()), "Hoàn 50% trong khoảng 12–24 giờ");
AssertEqual(0, BookingCancellationPolicy.RefundPercent(booking, localStart.AddHours(-11).ToUniversalTime()), "Không hoàn dưới 12 giờ");
AssertTrue(BookingCancellationPolicy.CanCustomerCancel(booking, localStart.AddHours(-12)), "Cho hủy đúng mốc 12 giờ");
AssertTrue(!BookingCancellationPolicy.CanCustomerCancel(booking, localStart.AddHours(-11)), "Chặn hủy dưới 12 giờ");
AssertTrue(new VenueComplex().IsActive, "Cụm sân mới mặc định hoạt động");
AssertTrue(new SportCourt().IsActive, "Sân con mới mặc định hoạt động");

booking.Status = BookingLifecyclePolicy.Confirmed;
booking.SlotCount = 1;
AssertEqual(BookingLifecyclePolicy.Completed, BookingLifecyclePolicy.DisplayStatus(booking, localStart.AddHours(1)), "Tự hoàn thành khi hết giờ sân");
AssertTrue(BookingLifecyclePolicy.CanPurge(booking, localStart.AddHours(1)), "Cho dọn booking đã hoàn thành");
booking.Status = BookingLifecyclePolicy.Cancelled;
booking.RefundStatus = "Chờ hoàn tiền";
AssertTrue(!BookingLifecyclePolicy.CanPurge(booking, localStart.AddDays(1)), "Giữ booking còn chờ hoàn tiền");

Console.WriteLine("SportMatch smoke tests: 10/10 passed.");
return;

static void AssertEqual<T>(T expected, T actual, string name) where T : IEquatable<T>
{
    if (!expected.Equals(actual)) throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}.");
}

static void AssertTrue(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"{name}: failed.");
}
