using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportMatch.Web.Data.Entities;
using SportMatch.Web.Models.ViewModels;
using SportMatch.Web.Options;
using SportMatch.Web.Services;
using SportMatch.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace SportMatch.Web.Controllers;

public sealed class BookingController : Controller
{
    private readonly IBookingStore _bookingStore;
    private readonly PaymentSettings _payment;
    private readonly SportMatchDbContext _db;
    private readonly BookingAccess _access;

    public BookingController(IBookingStore bookingStore, IOptions<PaymentSettings> payment, SportMatchDbContext db, BookingAccess access)
    {
        _bookingStore = bookingStore;
        _payment = payment.Value;
        _db = db;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] BookingFilterViewModel filter)
    {
        if (filter.Date == default || filter.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            filter.Date = DateOnly.FromDateTime(DateTime.Today);
        }

        var courtsQuery = _db.SportCourts.AsNoTracking().Include(x => x.VenueComplex).Include(x => x.TimeSlots)
            .Where(x => x.IsActive && x.VenueComplex!.IsActive).AsQueryable();
        if (filter.Sport != "all")
        {
            var sportName = SportNameFromSlug(filter.Sport);
            courtsQuery = courtsQuery.Where(x => x.SportName == sportName);
        }
        if (filter.District != "all") courtsQuery = courtsQuery.Where(x => x.VenueComplex!.District == filter.District);
        var courts = await courtsQuery.OrderBy(x => x.VenueComplex!.Name).ThenBy(x => x.Name).ToListAsync();
        var activeBookings = await _db.Bookings.AsNoTracking()
            .Where(x => x.BookingDate == filter.Date && (x.Status == "Đã xác nhận" || x.Status == "Yêu cầu hủy" || (x.Status == "Chờ thanh toán" && x.HoldExpiresAtUtc > DateTime.UtcNow)))
            .ToListAsync();
        var venues = courts.Select(court => ToVenueCard(court, activeBookings, filter.Date)).ToList();
        var matchEntities = await _db.MatchPosts.AsNoTracking().Include(x => x.JoinRequests)
            .Where(x => x.Status == "Đang tuyển" && x.MatchDate >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(x => x.MatchDate).ThenBy(x => x.StartTime).Take(3).ToListAsync();
        var quickMatches = matchEntities.Select(x => new QuickMatchViewModel
        {
            Id = x.Id, SportName = x.SportName, Icon = x.Sport switch { "football" => "⚽", "badminton" => "◒", "pickleball" => "◉", "basketball" => "●", _ => "✦" },
            Title = $"{x.SportName} — cần thêm đồng đội", VenueName = x.VenueName, TimeRange = x.StartTime.ToString("HH:mm"),
            AvailableSpots = Math.Max(0, x.NeededPlayers - x.JoinRequests.Count(r => r.Status == "Đã xác nhận" || (r.Status == "Chờ thanh toán" && r.HoldExpiresAtUtc > DateTime.UtcNow)))
        }).ToList();

        var viewModel = new BookingIndexViewModel
        {
            Filter = filter,
            Sports = BuildSports(),
            Venues = venues,
            QuickMatches = quickMatches
        };

        return View(viewModel);
    }

    [HttpPost]
    [EnableRateLimiting("public-write")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateBookingViewModel request)
    {
        var court = await _db.SportCourts.Include(x => x.VenueComplex).Include(x => x.TimeSlots).SingleOrDefaultAsync(x => x.Id == request.VenueId);

        if (court is null)
        {
            ModelState.AddModelError(nameof(request.VenueId), "Sân không tồn tại.");
        }

        if (request.BookingDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(request.BookingDate), "Không thể đặt sân trong quá khứ.");
        }

        var parsedTime = TimeOnly.TryParseExact(request.StartTime, "HH:mm", out var slotTime);
        var selectedSlot = parsedTime ? court?.TimeSlots.SingleOrDefault(slot => slot.StartTime == slotTime && slot.IsActive) : null;
        if (court is not null && selectedSlot is null)
        {
            ModelState.AddModelError(nameof(request.StartTime), "Khung giờ này không còn trống.");
        }
        if (parsedTime && request.BookingDate == DateOnly.FromDateTime(DateTime.Today) && slotTime <= TimeOnly.FromDateTime(DateTime.Now))
            ModelState.AddModelError(nameof(request.StartTime), "Khung giờ này đã qua. Vui lòng chọn giờ khác.");

        if (court is not null && parsedTime && request.SlotCount is >= 1 and <= 4)
        {
            var end = slotTime.ToTimeSpan().Add(TimeSpan.FromHours(request.SlotCount));
            if (slotTime < court.VenueComplex!.OpenTime || end > court.VenueComplex.CloseTime.ToTimeSpan() ||
                Enumerable.Range(0, request.SlotCount).Any(i => !court.TimeSlots.Any(s => s.IsActive && s.StartTime == slotTime.AddHours(i))))
                ModelState.AddModelError(nameof(request.SlotCount), "Các khung giờ liên tiếp không mở đặt hoặc vượt giờ đóng cửa.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                message = "Thông tin đặt sân chưa hợp lệ.",
                errors = ModelState
                    .Where(item => item.Value?.Errors.Count > 0)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray())
            });
        }

        var bookingCode = $"SM{DateTime.UtcNow:MMddHHmmssfff}";
        var totalAmount = Enumerable.Range(0, request.SlotCount).Sum(i => slotTime.AddHours(i) >= new TimeOnly(16, 0) ? court!.PeakPrice : court!.OffPeakPrice);
        var deposit = decimal.Round(totalAmount * _payment.BookingDepositPercent / 100m, 0);
        var startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        var expiresAt = DateTime.UtcNow.AddMinutes(_payment.HoldingMinutes);

        var created = await _bookingStore.TryAddAsync(new Booking
        {
            BookingCode = bookingCode,
            SportCourtId = request.VenueId,
            CustomerName = request.CustomerName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            BookingDate = request.BookingDate,
            StartTime = startTime,
            SlotCount = request.SlotCount,
            TotalAmount = totalAmount,
            DepositAmount = deposit,
            OpenForMatchmaking = request.OpenForMatchmaking,
            MatchNeededPlayers = request.MatchNeededPlayers,
            MatchLevel = request.MatchLevel,
            MatchCostPerPerson = request.MatchCostPerPerson,
            Status = "Chờ thanh toán",
            CreatedAtUtc = DateTime.UtcNow,
            HoldExpiresAtUtc = expiresAt
        });
        if (!created) return Conflict(new { message = "Khung giờ vừa được người khác giữ. Vui lòng chọn giờ khác." });
        _access.Remember(HttpContext, bookingCode);

        return Ok(new
        {
            message = "Đặt sân thành công!",
            bookingCode,
            venue = court!.VenueComplex!.Name,
            date = request.BookingDate.ToString("dd/MM/yyyy"),
            time = request.StartTime,
            deposit,
            matchmakingCreated = request.OpenForMatchmaking,
            paymentUrl = Url.Action(nameof(Payment), new { code = bookingCode })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Payment(string code)
    {
        if (!await _access.CanRead(HttpContext, code)) return NotFound();
        var booking = await _bookingStore.GetByCodeAsync(code);
        if (booking is null) return NotFound();

        var transferContent = booking.BookingCode;
        var qrUrl = $"https://img.vietqr.io/image/{Uri.EscapeDataString(_payment.BankCode)}-{Uri.EscapeDataString(_payment.AccountNumber)}-compact2.png" +
                    $"?amount={booking.Deposit:0}&addInfo={Uri.EscapeDataString(transferContent)}&accountName={Uri.EscapeDataString(_payment.AccountName)}";

        return View(new PaymentViewModel
        {
            Booking = booking,
            QrCodeUrl = qrUrl,
            BankName = _payment.BankName,
            AccountNumber = _payment.AccountNumber,
            AccountName = _payment.AccountName,
            TransferContent = transferContent,
            ExpiresAt = booking.HoldExpiresAtUtc.ToLocalTime(),
            ZaloUrl = string.IsNullOrWhiteSpace(_payment.ZaloUrl) ? null : _payment.ZaloUrl
        });
    }

    [HttpGet]
    public async Task<IActionResult> PaymentStatus(string code)
    {
        if (!await _access.CanRead(HttpContext, code)) return NotFound();
        var booking = await _bookingStore.GetByCodeAsync(code);
        return booking is null ? NotFound() : Ok(new { status = booking.Status });
    }

    private static IReadOnlyList<SportOptionViewModel> BuildSports() =>
    [
        new() { Value = "all", Name = "Tất cả", Icon = "✦" },
        new() { Value = "football", Name = "Bóng đá", Icon = "⚽" },
        new() { Value = "badminton", Name = "Cầu lông", Icon = "◒" },
        new() { Value = "pickleball", Name = "Pickleball", Icon = "◉" },
        new() { Value = "basketball", Name = "Bóng rổ", Icon = "●" },
        new() { Value = "tennis", Name = "Tennis", Icon = "◉" },
        new() { Value = "volleyball", Name = "Bóng chuyền", Icon = "●" }
    ];

    private static VenueCardViewModel ToVenueCard(SportCourt court, IReadOnlyList<Booking> activeBookings, DateOnly bookingDate)
    {
        var reservations = activeBookings.Where(x => x.SportCourtId == court.Id).ToList();
        var slots = court.TimeSlots.Where(x => x.IsActive).OrderBy(x => x.StartTime).Select(slot =>
        {
            var time = slot.StartTime.ToTimeSpan();
            var isPast = bookingDate == DateOnly.FromDateTime(DateTime.Today) && slot.StartTime <= TimeOnly.FromDateTime(DateTime.Now);
            var available = !isPast && !reservations.Any(x => time >= x.StartTime.ToTimeSpan() && time < x.StartTime.ToTimeSpan().Add(TimeSpan.FromHours(x.SlotCount)));
            return new TimeSlotViewModel { StartTime = slot.StartTime.ToString("HH:mm"), IsAvailable = available };
        }).ToList();
        var visualClasses = new[] { "venue-green", "venue-blue", "venue-orange", "venue-copper", "venue-purple", "venue-teal" };

        return new VenueCardViewModel
        {
            Id = court.Id,
            Name = $"{court.VenueComplex!.Name} · {court.Name}",
            VenueName = court.VenueComplex.Name,
            CourtName = court.Name,
            Sport = SportSlug(court.SportName),
            SportName = court.SportName,
            District = court.VenueComplex.District,
            Address = court.VenueComplex.Address,
            VisualClass = visualClasses[(court.Id - 1) % visualClasses.Length],
            ImagePath = court.ImagePath ?? court.VenueComplex.ImagePath,
            PricePerHour = court.OffPeakPrice,
            PeakPrice = court.PeakPrice,
            Rating = 5m,
            ReviewCount = 0,
            AvailableSlotCount = slots.Count(x => x.IsAvailable),
            Features = court.VenueComplex.Amenities.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            TimeSlots = slots
        };
    }

    private static string SportNameFromSlug(string slug) => slug switch
    {
        "football" => "Bóng đá mini",
        "badminton" => "Cầu lông",
        "pickleball" => "Pickleball",
        "basketball" => "Bóng rổ",
        "tennis" => "Tennis",
        "volleyball" => "Bóng chuyền",
        _ => string.Empty
    };

    private static string SportSlug(string name) => name switch
    {
        "Bóng đá mini" => "football",
        "Cầu lông" => "badminton",
        "Pickleball" => "pickleball",
        "Bóng rổ" => "basketball",
        "Tennis" => "tennis",
        "Bóng chuyền" => "volleyball",
        _ => "other"
    };

    private static IReadOnlyList<QuickMatchViewModel> BuildQuickMatches() =>
    [
        new() { Id = 1, SportName = "Bóng đá mini", Icon = "⚽", Title = "Kèo giao hữu 5v5 — thiếu tiền đạo", VenueName = "Sân bóng KTX Khu B", TimeRange = "19:00 – 21:00", AvailableSpots = 2 },
        new() { Id = 2, SportName = "Cầu lông", Icon = "◒", Title = "Đánh đôi vui vẻ sau giờ học", VenueName = "Smash Badminton", TimeRange = "18:30 – 20:30", AvailableSpots = 1 },
        new() { Id = 3, SportName = "Pickleball", Icon = "◉", Title = "Tìm cặp đấu trình 3.0–3.5", VenueName = "Saigon Pickle Club", TimeRange = "19:00 – 21:00", AvailableSpots = 2 }
    ];
}
