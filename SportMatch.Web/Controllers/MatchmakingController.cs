using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportMatch.Web.Data;
using SportMatch.Web.Data.Entities;
using SportMatch.Web.Models.ViewModels;
using SportMatch.Web.Options;
using SportMatch.Web.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Data;

namespace SportMatch.Web.Controllers;

public sealed class MatchmakingController(SportMatchDbContext db, MatchAccess access, IOptions<PaymentSettings> paymentOptions) : Controller
{
    private readonly PaymentSettings payment = paymentOptions.Value;

    [HttpGet]
    public async Task<IActionResult> Index(string sport = "all")
    {
        var query = db.MatchPosts.AsNoTracking().Include(x => x.JoinRequests)
            .Where(x => x.Status == "Đang tuyển" && x.MatchDate >= DateOnly.FromDateTime(DateTime.Today));
        if (sport != "all") query = query.Where(x => x.Sport == sport);
        var matches = await query.OrderBy(x => x.MatchDate).ThenBy(x => x.StartTime).ToListAsync();
        var hostCodes = access.HostCodes(HttpContext);
        var owned = await db.MatchPosts.AsNoTracking().Include(x => x.JoinRequests)
            .Where(x => hostCodes.Contains(x.MatchCode)).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        return View(new MatchmakingIndexViewModel
        {
            ActiveSport = sport,
            Matches = matches.Select(ToCard).ToList(),
            OwnedMatches = owned.Select(x => new OwnedMatchViewModel
            {
                MatchCode = x.MatchCode, Title = $"{x.SportName} tại {x.VenueName}", MatchDate = x.MatchDate,
                StartTime = x.StartTime.ToString("HH:mm"), Status = x.Status,
                PendingRequests = x.JoinRequests.Count(r => r.Status == "Chờ duyệt")
            }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> MyMatches()
    {
        var hostCodes = access.HostCodes(HttpContext);
        var requestCodes = access.RequestCodes(HttpContext);
        var hosted = await db.MatchPosts.AsNoTracking().Include(x => x.JoinRequests)
            .Where(x => hostCodes.Contains(x.MatchCode)).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        var requests = await db.MatchJoinRequests.AsNoTracking().Include(x => x.MatchPost)
            .Where(x => requestCodes.Contains(x.RequestCode)).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        return View(new MyMatchesViewModel
        {
            HostedMatches = hosted.Select(x => new OwnedMatchViewModel
            {
                MatchCode = x.MatchCode, Title = $"{x.SportName} tại {x.VenueName}", MatchDate = x.MatchDate,
                StartTime = x.StartTime.ToString("HH:mm"), Status = x.Status,
                PendingRequests = x.JoinRequests.Count(r => r.Status == "Chờ duyệt")
            }).ToList(),
            JoinRequests = requests.Select(x => new MyJoinRequestViewModel
            {
                RequestCode = x.RequestCode, MatchTitle = x.MatchPost!.SportName, VenueName = x.MatchPost.VenueName,
                MatchDate = x.MatchPost.MatchDate, StartTime = x.MatchPost.StartTime.ToString("HH:mm"),
                Status = EffectiveStatus(x), DepositAmount = x.DepositAmount
            }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Create([FromForm] CreateMatchViewModel request)
    {
        if (request.MatchDate < DateOnly.FromDateTime(DateTime.Today)) ModelState.AddModelError(nameof(request.MatchDate), "Ngày chơi không thể nằm trong quá khứ.");
        if (!TimeOnly.TryParseExact(request.StartTime, "HH:mm", out var startTime)) ModelState.AddModelError(nameof(request.StartTime), "Giờ bắt đầu không hợp lệ.");
        if (!ModelState.IsValid) return BadRequest(new { message = "Thông tin kèo chưa hợp lệ.", errors = Errors() });
        var match = new MatchPost
        {
            MatchCode = NewCode("MK"), HostName = request.HostName.Trim(), PhoneNumber = request.PhoneNumber.Trim(),
            Sport = request.Sport, SportName = SportName(request.Sport), VenueName = request.VenueName.Trim(),
            MatchDate = request.MatchDate, StartTime = startTime, Level = request.Level,
            NeededPlayers = request.NeededPlayers, CostPerPerson = request.CostPerPerson,
            Status = "Đang tuyển", CreatedAtUtc = DateTime.UtcNow
        };
        db.MatchPosts.Add(match);
        await db.SaveChangesAsync();
        access.RememberHost(HttpContext, match.MatchCode);
        return Ok(new { message = "Đăng kèo thành công!", manageUrl = Url.Action(nameof(Manage), new { code = match.MatchCode }) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("public-write")]
    public async Task<IActionResult> Join([FromForm] JoinMatchViewModel request)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Vui lòng nhập đúng họ tên và số điện thoại.", errors = Errors() });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var match = await db.MatchPosts.Include(x => x.JoinRequests).SingleOrDefaultAsync(x => x.Id == request.MatchId);
        if (match is null || match.Status != "Đang tuyển") return NotFound(new { message = "Kèo không còn nhận người." });
        var confirmed = match.JoinRequests.Count(x => x.Status == "Đã xác nhận");
        var holding = match.JoinRequests.Count(x => x.Status == "Chờ thanh toán" && x.HoldExpiresAtUtc > DateTime.UtcNow);
        if (confirmed + holding >= match.NeededPlayers) return Conflict(new { message = "Kèo này đã đủ người hoặc đang giữ chỗ." });
        if (match.JoinRequests.Any(x => x.PhoneNumber == request.PhoneNumber && x.Status != "Từ chối")) return Conflict(new { message = "Số điện thoại này đã gửi yêu cầu cho kèo." });
        var joinRequest = new MatchJoinRequest
        {
            RequestCode = NewCode("TG"), ApplicantName = request.ApplicantName.Trim(), PhoneNumber = request.PhoneNumber.Trim(),
            Status = "Chờ duyệt", DepositAmount = decimal.Round(match.CostPerPerson * payment.MatchDepositPercent / 100m, 0), CreatedAtUtc = DateTime.UtcNow
        };
        match.JoinRequests.Add(joinRequest);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        access.RememberRequest(HttpContext, joinRequest.RequestCode);
        return Ok(new { message = "Đã gửi yêu cầu cho chủ kèo!", statusUrl = Url.Action(nameof(RequestDetails), new { code = joinRequest.RequestCode }) });
    }

    [HttpGet]
    public async Task<IActionResult> Manage(string code)
    {
        if (!access.HostCodes(HttpContext).Contains(code)) return NotFound();
        var match = await db.MatchPosts.AsNoTracking().Include(x => x.JoinRequests).SingleOrDefaultAsync(x => x.MatchCode == code);
        if (match is null) return NotFound();
        return View(new MatchManageViewModel
        {
            Match = ToCard(match),
            Requests = match.JoinRequests.OrderByDescending(x => x.CreatedAtUtc).Select(x => new MatchRequestViewModel
            {
                RequestCode = x.RequestCode, ApplicantName = x.ApplicantName, PhoneNumber = x.PhoneNumber,
                Status = EffectiveStatus(x), DepositAmount = x.DepositAmount, HoldExpiresAt = x.HoldExpiresAtUtc?.ToLocalTime()
            }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(string matchCode, string requestCode, string decision)
    {
        if (!access.HostCodes(HttpContext).Contains(matchCode)) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var joinRequest = await db.MatchJoinRequests.Include(x => x.MatchPost).ThenInclude(x => x!.JoinRequests)
            .SingleOrDefaultAsync(x => x.RequestCode == requestCode && x.MatchPost!.MatchCode == matchCode);
        if (joinRequest is null || joinRequest.Status != "Chờ duyệt") return NotFound();
        if (decision == "accept")
        {
            var occupied = joinRequest.MatchPost!.JoinRequests.Count(x => x.Status == "Đã xác nhận" || (x.Status == "Chờ thanh toán" && x.HoldExpiresAtUtc > DateTime.UtcNow));
            if (occupied >= joinRequest.MatchPost.NeededPlayers) TempData["MatchError"] = "Kèo đã đủ vị trí.";
            else { joinRequest.Status = "Chờ thanh toán"; joinRequest.HoldExpiresAtUtc = DateTime.UtcNow.AddMinutes(payment.HoldingMinutes); }
        }
        else joinRequest.Status = "Từ chối";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return RedirectToAction(nameof(Manage), new { code = matchCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseOwned(string matchCode)
    {
        if (!access.HostCodes(HttpContext).Contains(matchCode)) return NotFound();
        var match = await db.MatchPosts.SingleOrDefaultAsync(x => x.MatchCode == matchCode);
        if (match is null) return NotFound();
        match.Status = "Đã đóng";
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RequestDetails(string code)
    {
        if (!access.RequestCodes(HttpContext).Contains(code)) return NotFound();
        var request = await db.MatchJoinRequests.AsNoTracking().Include(x => x.MatchPost).SingleOrDefaultAsync(x => x.RequestCode == code);
        if (request is null) return NotFound();
        var status = EffectiveStatus(request);
        var canPay = status == "Chờ thanh toán";
        var qrUrl = canPay ? BuildQr(request.DepositAmount, request.RequestCode) : null;
        return View("Request", new MatchRequestStatusViewModel
        {
            RequestCode = request.RequestCode, Status = status, MatchTitle = request.MatchPost!.SportName,
            VenueName = request.MatchPost.VenueName, MatchDate = request.MatchPost.MatchDate,
            StartTime = request.MatchPost.StartTime.ToString("HH:mm"), DepositAmount = request.DepositAmount,
            ExpiresAt = request.HoldExpiresAtUtc?.ToLocalTime(), QrCodeUrl = qrUrl,
            HostName = status == "Đã xác nhận" ? request.MatchPost.HostName : null,
            HostPhone = status == "Đã xác nhận" ? request.MatchPost.PhoneNumber : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> RequestStatus(string code)
    {
        if (!access.RequestCodes(HttpContext).Contains(code)) return NotFound();
        var request = await db.MatchJoinRequests.AsNoTracking().SingleOrDefaultAsync(x => x.RequestCode == code);
        return request is null ? NotFound() : Ok(new { status = EffectiveStatus(request) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> RecoverHost(string matchCode, string phoneNumber)
    {
        var match = await db.MatchPosts.AsNoTracking().SingleOrDefaultAsync(x => x.MatchCode == matchCode && x.PhoneNumber == phoneNumber);
        if (match is null) TempData["RecoverError"] = "Mã kèo hoặc số điện thoại chủ kèo không đúng.";
        else access.RememberHost(HttpContext, match.MatchCode);
        return match is null ? RedirectToAction(nameof(MyMatches)) : RedirectToAction(nameof(Manage), new { code = match.MatchCode });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> RecoverRequest(string requestCode, string phoneNumber)
    {
        var request = await db.MatchJoinRequests.AsNoTracking().SingleOrDefaultAsync(x => x.RequestCode == requestCode && x.PhoneNumber == phoneNumber);
        if (request is null) TempData["RecoverError"] = "Mã yêu cầu hoặc số điện thoại không đúng.";
        else access.RememberRequest(HttpContext, request.RequestCode);
        return request is null ? RedirectToAction(nameof(MyMatches)) : RedirectToAction(nameof(RequestDetails), new { code = request.RequestCode });
    }

    private Dictionary<string, string[]> Errors() => ModelState.Where(x => x.Value?.Errors.Count > 0).ToDictionary(x => x.Key, x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
    private string BuildQr(decimal amount, string content) => $"https://img.vietqr.io/image/{Uri.EscapeDataString(payment.BankCode)}-{Uri.EscapeDataString(payment.AccountNumber)}-compact2.png?amount={amount:0}&addInfo={Uri.EscapeDataString(content)}&accountName={Uri.EscapeDataString(payment.AccountName)}";
    private static string NewCode(string prefix) => $"{prefix}{DateTime.UtcNow:MMddHHmmss}{Random.Shared.Next(100, 999)}";
    private static string EffectiveStatus(MatchJoinRequest request) => request.Status == "Chờ thanh toán" && request.HoldExpiresAtUtc <= DateTime.UtcNow ? "Hết hạn" : request.Status;
    private static MatchCardViewModel ToCard(MatchPost x) => new()
    {
        Id = x.Id, MatchCode = x.MatchCode, Sport = x.Sport, SportName = x.SportName,
        Title = $"{x.SportName} — cần thêm đồng đội", VenueName = x.VenueName, District = x.District,
        MatchDate = x.MatchDate, TimeRange = x.StartTime.ToString("HH:mm"), Level = x.Level,
        AvailableSpots = Math.Max(0, x.NeededPlayers - x.JoinRequests.Count(r => r.Status == "Đã xác nhận" || (r.Status == "Chờ thanh toán" && r.HoldExpiresAtUtc > DateTime.UtcNow))),
        CostPerPerson = x.CostPerPerson, HostName = x.HostName
    };
    private static string SportName(string value) => value switch { "football" => "Bóng đá mini", "badminton" => "Cầu lông", "pickleball" => "Pickleball", "basketball" => "Bóng rổ", "tennis" => "Tennis", "volleyball" => "Bóng chuyền", _ => "Thể thao" };
}
