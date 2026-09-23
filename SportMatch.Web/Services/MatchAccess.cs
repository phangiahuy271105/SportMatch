using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text.Json;

namespace SportMatch.Web.Services;

public sealed class MatchAccess(IDataProtectionProvider protection)
{
    private readonly IDataProtector hostProtector = protection.CreateProtector("SportMatch.MatchHosts.v1");
    private readonly IDataProtector requestProtector = protection.CreateProtector("SportMatch.MatchRequests.v1");

    public IReadOnlyList<string> HostCodes(HttpContext context) => Read(context, "SportMatch.HostMatches", hostProtector);
    public IReadOnlyList<string> RequestCodes(HttpContext context) => Read(context, "SportMatch.JoinRequests", requestProtector);
    public void RememberHost(HttpContext context, string code) => Remember(context, "SportMatch.HostMatches", code, hostProtector);
    public void RememberHosts(HttpContext context, IEnumerable<string> codes) => RememberMany(context, "SportMatch.HostMatches", codes, hostProtector);
    public void RememberRequest(HttpContext context, string code) => Remember(context, "SportMatch.JoinRequests", code, requestProtector);

    private static List<string> Read(HttpContext context, string cookieName, IDataProtector protector)
    {
        try
        {
            var value = context.Request.Cookies[cookieName];
            return value is null ? [] : JsonSerializer.Deserialize<List<string>>(protector.Unprotect(value)) ?? [];
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException) { return []; }
    }

    private static void Remember(HttpContext context, string cookieName, string code, IDataProtector protector)
        => RememberMany(context, cookieName, [code], protector);

    private static void RememberMany(HttpContext context, string cookieName, IEnumerable<string> newCodes, IDataProtector protector)
    {
        var codes = Read(context, cookieName, protector);
        foreach (var code in newCodes.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            codes.Remove(code);
            codes.Add(code);
        }
        context.Response.Cookies.Append(cookieName, protector.Protect(JsonSerializer.Serialize(codes.TakeLast(30))),
            new CookieOptions { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddMonths(6), IsEssential = true });
    }
}
