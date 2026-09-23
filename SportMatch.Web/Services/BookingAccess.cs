using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportMatch.Web.Data;

namespace SportMatch.Web.Services;

public sealed class BookingAccess(IDataProtectionProvider protection, SportMatchDbContext db)
{
    private readonly IDataProtector protector = protection.CreateProtector("SportMatch.GuestBookings.v1");
    public List<string> Codes(HttpContext context)
    {
        try
        {
            var value = context.Request.Cookies["SportMatch.Bookings"];
            return value is null ? [] : JsonSerializer.Deserialize<List<string>>(protector.Unprotect(value)) ?? [];
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException) { return []; }
    }
    public void Remember(HttpContext context, string code)
    {
        var codes = Codes(context);
        codes.Remove(code);
        codes.Add(code);
        context.Response.Cookies.Append("SportMatch.Bookings", protector.Protect(JsonSerializer.Serialize(codes.TakeLast(40))),
            new CookieOptions { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddMonths(6), IsEssential = true });
    }
    public async Task<bool> CanRead(HttpContext context, string code)
    {
        if (context.User.IsInRole("Admin") || Codes(context).Contains(code)) return true;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is not null && await db.Bookings.AnyAsync(x => x.BookingCode == code && x.UserId == userId);
    }
}
