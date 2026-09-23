using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SportMatch.Web.Models.ViewModels;
using SportMatch.Web.Services;
using Microsoft.EntityFrameworkCore;
using SportMatch.Web.Data;
using SportMatch.Web.Data.Entities;
using Microsoft.Extensions.Options;
using SportMatch.Web.Options;

namespace SportMatch.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController : Controller
{
    private readonly IBookingStore _bookingStore;
    private readonly SportMatchDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly PaymentSettings _paymentSettings;

    public AdminController(IBookingStore bookingStore, SportMatchDbContext db, IWebHostEnvironment environment, IOptions<PaymentSettings> paymentSettings)
    {
        _bookingStore = bookingStore;
        _db = db;
        _environment = environment;
        _paymentSettings = paymentSettings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var phones = (await _db.Bookings.AsNoTracking().Select(x => x.PhoneNumber).ToListAsync())
            .Concat(await _db.MatchPosts.AsNoTracking().Select(x => x.PhoneNumber).ToListAsync())
            .Concat(await _db.MatchJoinRequests.AsNoTracking().Select(x => x.PhoneNumber).ToListAsync())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return View(new AdminDashboardViewModel
        {
            Bookings = await _bookingStore.GetAllAsync(), VenueCount = await _db.VenueComplexes.CountAsync(),
            CourtCount = await _db.SportCourts.CountAsync(), CustomerCount = phones,
            PaymentWebhookConfigured = !string.IsNullOrWhiteSpace(_paymentSettings.WebhookApiKey)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Operations()
    {
        var paymentLogs = await _db.PaymentWebhookLogs.AsNoTracking().OrderByDescending(x => x.ReceivedAtUtc).Take(100).ToListAsync();
        var auditLogs = await _db.AdminAuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(100).ToListAsync();
        return View(new AdminOperationsViewModel
        {
            PaymentLogs = paymentLogs.Select(x => new AdminPaymentLogViewModel { TransactionId = x.TransactionId, ReceivedAt = x.ReceivedAtUtc.ToLocalTime(), Amount = x.Amount, Content = x.Content, Accepted = x.Accepted, Result = x.Result }).ToList(),
            AuditLogs = auditLogs.Select(x => new AdminAuditRowViewModel { OccurredAt = x.OccurredAtUtc.ToLocalTime(), AdminName = x.AdminName, Action = x.Action, EntityType = x.EntityType, EntityId = x.EntityId, Details = x.Details }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Bookings(string? search, string? status)
    {
        var bookings = await _bookingStore.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(search)) bookings = bookings.Where(x => (x.BookingCode + " " + x.CustomerName + " " + x.PhoneNumber + " " + x.VenueName).Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(status)) bookings = bookings.Where(x => x.Status == status).ToList();
        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(new AdminDashboardViewModel { Bookings = bookings });
    }

    [HttpGet]
    public async Task<IActionResult> Customers()
    {
        var customers = new Dictionary<string, CustomerAggregate>(StringComparer.OrdinalIgnoreCase);
        foreach (var booking in await _db.Bookings.AsNoTracking().ToListAsync())
        {
            var customer = GetCustomer(customers, booking.PhoneNumber, booking.CustomerName, booking.Email, booking.CreatedAtUtc);
            customer.BookingCount++;
        }
        foreach (var match in await _db.MatchPosts.AsNoTracking().ToListAsync())
        {
            var customer = GetCustomer(customers, match.PhoneNumber, match.HostName, null, match.CreatedAtUtc);
            customer.HostedMatchCount++;
        }
        foreach (var request in await _db.MatchJoinRequests.AsNoTracking().ToListAsync())
        {
            var customer = GetCustomer(customers, request.PhoneNumber, request.ApplicantName, null, request.CreatedAtUtc);
            customer.JoinRequestCount++;
        }
        return View(customers.Values.OrderByDescending(x => x.LastActivity).Select(x => new AdminCustomerViewModel
        {
            Name = x.Name, PhoneNumber = x.PhoneNumber, Email = x.Email, BookingCount = x.BookingCount,
            HostedMatchCount = x.HostedMatchCount, JoinRequestCount = x.JoinRequestCount, LastActivity = x.LastActivity.ToLocalTime()
        }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Matches()
    {
        var matches = await _db.MatchPosts.AsNoTracking().Include(x => x.JoinRequests).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        return View(new AdminMatchListViewModel
        {
            Matches = matches.Select(x => new AdminMatchRowViewModel
            {
                MatchCode = x.MatchCode, HostName = x.HostName, PhoneNumber = x.PhoneNumber,
                SportName = x.SportName, VenueName = x.VenueName, MatchDate = x.MatchDate,
                StartTime = x.StartTime.ToString("HH:mm"), Status = x.Status,
                RequestCount = x.JoinRequests.Count, ConfirmedCount = x.JoinRequests.Count(r => r.Status == "Đã xác nhận"),
                Requests = x.JoinRequests.OrderByDescending(r => r.CreatedAtUtc).Select(r => new MatchRequestViewModel
                {
                    RequestCode = r.RequestCode, ApplicantName = r.ApplicantName, PhoneNumber = r.PhoneNumber,
                    Status = r.Status == "Chờ thanh toán" && r.HoldExpiresAtUtc <= DateTime.UtcNow ? "Hết hạn" : r.Status,
                    DepositAmount = r.DepositAmount, HoldExpiresAt = r.HoldExpiresAtUtc?.ToLocalTime()
                }).ToList()
            }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMatchRequest(string requestCode, string status)
    {
        if (status is not ("Đã xác nhận" or "Từ chối")) return BadRequest();
        var request = await _db.MatchJoinRequests.Include(x => x.MatchPost).ThenInclude(x => x!.JoinRequests).SingleOrDefaultAsync(x => x.RequestCode == requestCode);
        if (request is null) return NotFound();
        if (status == "Đã xác nhận" && request.Status != "Chờ thanh toán") TempData["AdminError"] = "Yêu cầu chưa ở trạng thái chờ thanh toán.";
        else
        {
            request.Status = status;
            if (status == "Đã xác nhận") request.PaidAtUtc ??= DateTime.UtcNow;
            if (request.MatchPost!.JoinRequests.Count(x => x.Status == "Đã xác nhận") >= request.MatchPost.NeededPlayers) request.MatchPost.Status = "Đã đủ người";
            await _db.SaveChangesAsync();
            await AuditAsync("Cập nhật cọc ghép kèo", "MatchJoinRequest", requestCode, status);
        }
        return RedirectToAction(nameof(Matches));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseMatch(string matchCode)
    {
        var match = await _db.MatchPosts.SingleOrDefaultAsync(x => x.MatchCode == matchCode);
        if (match is null) return NotFound();
        match.Status = "Đã đóng";
        await _db.SaveChangesAsync();
        await AuditAsync("Đóng kèo", "MatchPost", matchCode, "Admin đóng kèo");
        return RedirectToAction(nameof(Matches));
    }

    [HttpGet]
    public async Task<IActionResult> Venues()
    {
        var entities = await _db.VenueComplexes.AsNoTracking().Include(x => x.Courts).OrderBy(x => x.Name).ToListAsync();
        var venues = entities.Select(x => new AdminVenueRowViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                District = x.District,
                PhoneNumber = x.PhoneNumber,
                OpeningHours = $"{x.OpenTime:HH:mm} – {x.CloseTime:HH:mm}",
                ImagePath = x.ImagePath,
                CourtCount = x.Courts.Count,
                Sports = x.Courts.Select(c => c.SportName).Distinct().ToList()
            }).ToList();
        return View(new AdminVenueListViewModel { Venues = venues });
    }

    [HttpGet]
    public IActionResult CreateVenue() => View("VenueForm", new AdminVenueFormViewModel());

    [HttpGet]
    public async Task<IActionResult> Courts(int id)
    {
        var venue = await _db.VenueComplexes.AsNoTracking().Include(x => x.Courts).SingleOrDefaultAsync(x => x.Id == id);
        if (venue is null) return NotFound();
        ViewData["VenueName"] = venue.Name;
        ViewData["VenueId"] = id;
        return View(venue.Courts.Select(x => new AdminCourtViewModel { Id = x.Id, VenueComplexId = id, Name = x.Name, SportName = x.SportName, CourtType = x.CourtType, OffPeakPrice = x.OffPeakPrice, PeakPrice = x.PeakPrice }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Court(int venueId, int? id)
    {
        if (!await _db.VenueComplexes.AnyAsync(x => x.Id == venueId)) return NotFound();
        if (id is null) return View(new AdminCourtViewModel { VenueComplexId = venueId });
        var court = await _db.SportCourts.AsNoTracking().Include(x => x.TimeSlots).SingleOrDefaultAsync(x => x.Id == id && x.VenueComplexId == venueId);
        if (court is null) return NotFound();
        return View(new AdminCourtViewModel { Id = court.Id, VenueComplexId = venueId, Name = court.Name, SportName = court.SportName, CourtType = court.CourtType, OffPeakPrice = court.OffPeakPrice, PeakPrice = court.PeakPrice, ActiveSlots = court.TimeSlots.Where(x => x.IsActive).Select(x => x.Id).ToList(), Slots = court.TimeSlots.OrderBy(x => x.StartTime).Select(x => new AdminSlotViewModel { Id = x.Id, Time = x.StartTime.ToString("HH:mm") }).ToList() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Court(AdminCourtViewModel model)
    {
        var venue = await _db.VenueComplexes.FindAsync(model.VenueComplexId);
        if (venue is null) return NotFound();
        var court = model.Id == 0 ? new SportCourt { VenueComplexId = venue.Id } : await _db.SportCourts.Include(x => x.TimeSlots).SingleOrDefaultAsync(x => x.Id == model.Id && x.VenueComplexId == venue.Id);
        if (court is null) return NotFound();
        model.Slots = court.TimeSlots.OrderBy(x => x.StartTime).Select(x => new AdminSlotViewModel { Id = x.Id, Time = x.StartTime.ToString("HH:mm") }).ToList();
        if (!ModelState.IsValid) return View(model);
        court.Name = model.Name.Trim(); court.SportName = model.SportName; court.CourtType = model.CourtType.Trim();
        court.OffPeakPrice = model.OffPeakPrice; court.PeakPrice = model.PeakPrice;
        if (model.Id == 0)
        {
            for (var minutes = (int)venue.OpenTime.ToTimeSpan().TotalMinutes; minutes + 60 <= venue.CloseTime.ToTimeSpan().TotalMinutes; minutes += 60)
                court.TimeSlots.Add(new CourtTimeSlot { StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes)) });
            _db.SportCourts.Add(court);
        }
        else foreach (var slot in court.TimeSlots) slot.IsActive = model.ActiveSlots.Contains(slot.Id);
        await _db.SaveChangesAsync();
        await AuditAsync(model.Id == 0 ? "Thêm sân con" : "Cập nhật sân con", "SportCourt", court.Id.ToString(), court.Name);
        return RedirectToAction(nameof(Courts), new { id = venue.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> CreateVenue(AdminVenueFormViewModel model)
    {
        await ValidateVenueFormAsync(model);
        if (!ModelState.IsValid) return View("VenueForm", model);

        var venue = new VenueComplex();
        await ApplyVenueFormAsync(venue, model);
        _db.VenueComplexes.Add(venue);
        await _db.SaveChangesAsync();
        await AuditAsync("Thêm cụm sân", "VenueComplex", venue.Id.ToString(), venue.Name);
        TempData["AdminMessage"] = "Đã thêm cụm sân mới.";
        return RedirectToAction(nameof(Venues));
    }

    [HttpGet]
    public async Task<IActionResult> EditVenue(int id)
    {
        var venue = await _db.VenueComplexes.AsNoTracking().Include(x => x.Courts).SingleOrDefaultAsync(x => x.Id == id);
        if (venue is null) return NotFound();
        var court = venue.Courts.OrderBy(x => x.Id).FirstOrDefault();
        return View("VenueForm", new AdminVenueFormViewModel
        {
            Id = venue.Id,
            Name = venue.Name,
            Address = venue.Address,
            District = venue.District,
            PhoneNumber = venue.PhoneNumber,
            OpenTime = venue.OpenTime,
            CloseTime = venue.CloseTime,
            Amenities = venue.Amenities,
            Description = venue.Description,
            ExistingImagePath = venue.ImagePath,
            CourtName = court?.Name ?? string.Empty,
            SportName = court?.SportName ?? "Bóng đá mini",
            CourtType = court?.CourtType ?? "Sân 5",
            OffPeakPrice = court?.OffPeakPrice ?? 180000,
            PeakPrice = court?.PeakPrice ?? 280000
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> EditVenue(AdminVenueFormViewModel model)
    {
        await ValidateVenueFormAsync(model);
        if (!ModelState.IsValid) return View("VenueForm", model);
        var venue = await _db.VenueComplexes.Include(x => x.Courts).ThenInclude(x => x.TimeSlots).SingleOrDefaultAsync(x => x.Id == model.Id);
        if (venue is null) return NotFound();
        await ApplyVenueFormAsync(venue, model);
        await _db.SaveChangesAsync();
        await AuditAsync("Cập nhật cụm sân", "VenueComplex", venue.Id.ToString(), venue.Name);
        TempData["AdminMessage"] = "Đã cập nhật thông tin sân.";
        return RedirectToAction(nameof(Venues));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateBookingStatusViewModel request)
    {
        if (!ModelState.IsValid || !await _bookingStore.UpdateStatusAsync(request.BookingCode, request.Status))
        {
            TempData["AdminError"] = "Không thể cập nhật booking.";
        }
        else await AuditAsync("Cập nhật booking", "Booking", request.BookingCode, request.Status);

        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCancellation(string bookingCode, string decision)
    {
        var booking = await _db.Bookings.SingleOrDefaultAsync(x => x.BookingCode == bookingCode && x.Status == "Yêu cầu hủy");
        if (booking is null) return NotFound();
        if (decision == "reject")
        {
            booking.Status = "Đã xác nhận";
            booking.CancellationRequestedAtUtc = null;
            booking.CancellationReason = null;
            booking.RefundPercent = null;
        }
        else
        {
            var percent = decision == "full" ? 100 : booking.RefundPercent ?? BookingCancellationPolicy.RefundPercent(booking, booking.CancellationRequestedAtUtc);
            booking.RefundPercent = percent;
            booking.RefundAmount = decimal.Round(booking.DepositAmount * percent / 100m, 0);
            booking.RefundStatus = booking.RefundAmount > 0 ? "Chờ hoàn tiền" : "Không hoàn cọc";
            booking.Status = "Đã hủy";
            booking.CancelledAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        await AuditAsync("Xử lý yêu cầu hủy", "Booking", bookingCode, $"{decision}; hoàn {booking.RefundPercent ?? 0}%");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRefunded(string bookingCode)
    {
        var booking = await _db.Bookings.SingleOrDefaultAsync(x => x.BookingCode == bookingCode && x.RefundStatus == "Chờ hoàn tiền");
        if (booking is null) return NotFound();
        booking.RefundStatus = "Đã hoàn tiền";
        await _db.SaveChangesAsync();
        await AuditAsync("Xác nhận hoàn tiền", "Booking", bookingCode, $"Đã hoàn {booking.RefundAmount:0}đ");
        return RedirectToAction(nameof(Bookings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelByVenue(string bookingCode, string reason)
    {
        var booking = await _db.Bookings.SingleOrDefaultAsync(x => x.BookingCode == bookingCode && x.Status == "Đã xác nhận");
        if (booking is null) return NotFound();
        booking.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Sự cố sân hoặc điều kiện khách quan" : reason.Trim();
        booking.CancellationRequestedAtUtc = DateTime.UtcNow;
        booking.CancelledAtUtc = DateTime.UtcNow;
        booking.RefundPercent = 100;
        booking.RefundAmount = booking.DepositAmount;
        booking.RefundStatus = "Chờ hoàn tiền";
        booking.Status = "Đã hủy";
        await _db.SaveChangesAsync();
        await AuditAsync("Hủy do sân", "Booking", bookingCode, booking.CancellationReason);
        return RedirectToAction(nameof(Bookings));
    }

    private async Task ValidateVenueFormAsync(AdminVenueFormViewModel model)
    {
        if (model.CloseTime <= model.OpenTime) ModelState.AddModelError(nameof(model.CloseTime), "Giờ đóng cửa phải sau giờ mở cửa.");
        if (model.Image is null) return;
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var contentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (model.Image.Length == 0) ModelState.AddModelError(nameof(model.Image), "Tệp ảnh đang trống.");
        if (model.Image.Length > 5 * 1024 * 1024) ModelState.AddModelError(nameof(model.Image), "Ảnh không được vượt quá 5 MB.");
        if (!extensions.Contains(Path.GetExtension(model.Image.FileName).ToLowerInvariant())) ModelState.AddModelError(nameof(model.Image), "Chỉ nhận ảnh JPG, PNG hoặc WebP.");
        if (!contentTypes.Contains(model.Image.ContentType.ToLowerInvariant())) ModelState.AddModelError(nameof(model.Image), "Định dạng nội dung ảnh không hợp lệ.");
        if (model.Image.Length > 0 && !await HasValidImageSignatureAsync(model.Image)) ModelState.AddModelError(nameof(model.Image), "Nội dung tệp không phải ảnh JPG, PNG hoặc WebP hợp lệ.");
    }

    private static async Task<bool> HasValidImageSignatureAsync(IFormFile image)
    {
        var header = new byte[12];
        await using var stream = image.OpenReadStream();
        var count = await stream.ReadAsync(header);
        var isJpeg = count >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff;
        var isPng = count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });
        var isWebp = count >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        return isJpeg || isPng || isWebp;
    }

    private async Task ApplyVenueFormAsync(VenueComplex venue, AdminVenueFormViewModel model)
    {
        var previousOpenTime = venue.OpenTime;
        var previousCloseTime = venue.CloseTime;
        venue.Name = model.Name.Trim();
        venue.Address = model.Address.Trim();
        venue.District = model.District.Trim();
        venue.PhoneNumber = model.PhoneNumber.Trim();
        venue.OpenTime = model.OpenTime;
        venue.CloseTime = model.CloseTime;
        venue.Amenities = model.Amenities?.Trim() ?? string.Empty;
        venue.Description = model.Description?.Trim() ?? string.Empty;
        if (model.Image is not null) venue.ImagePath = await SaveImageAsync(model.Image);

        var court = venue.Courts.OrderBy(x => x.Id).FirstOrDefault();
        if (court is null)
        {
            court = new SportCourt();
            venue.Courts.Add(court);
        }
        court.Name = model.CourtName.Trim();
        court.SportName = model.SportName;
        court.CourtType = model.CourtType.Trim();
        court.OffPeakPrice = model.OffPeakPrice;
        court.PeakPrice = model.PeakPrice;
        if (court.TimeSlots.Count == 0)
            for (var minutes = (int)model.OpenTime.ToTimeSpan().TotalMinutes; minutes + 60 <= model.CloseTime.ToTimeSpan().TotalMinutes; minutes += 60)
                court.TimeSlots.Add(new CourtTimeSlot { StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes)), DurationMinutes = 60, IsActive = true });

        var validTimes = new HashSet<TimeOnly>();
        for (var minutes = (int)model.OpenTime.ToTimeSpan().TotalMinutes; minutes + 60 <= model.CloseTime.ToTimeSpan().TotalMinutes; minutes += 60)
            validTimes.Add(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes)));
        foreach (var venueCourt in venue.Courts)
        {
            foreach (var slot in venueCourt.TimeSlots.Where(x => !validTimes.Contains(x.StartTime))) slot.IsActive = false;
            foreach (var slot in venueCourt.TimeSlots.Where(x => validTimes.Contains(x.StartTime) && (x.StartTime < previousOpenTime || x.StartTime >= previousCloseTime))) slot.IsActive = true;
            foreach (var time in validTimes.Where(time => venueCourt.TimeSlots.All(x => x.StartTime != time)))
                venueCourt.TimeSlots.Add(new CourtTimeSlot { StartTime = time, DurationMinutes = 60, IsActive = true });
        }
    }

    private static CustomerAggregate GetCustomer(Dictionary<string, CustomerAggregate> customers, string phone, string name, string? email, DateTime activity)
    {
        if (!customers.TryGetValue(phone, out var customer)) customers[phone] = customer = new CustomerAggregate { PhoneNumber = phone, Name = name, Email = email, LastActivity = activity };
        if (activity >= customer.LastActivity) { customer.Name = name; customer.Email ??= email; customer.LastActivity = activity; }
        return customer;
    }

    private sealed class CustomerAggregate
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string? Email { get; set; }
        public int BookingCount { get; set; }
        public int HostedMatchCount { get; set; }
        public int JoinRequestCount { get; set; }
        public DateTime LastActivity { get; set; }
    }

    private async Task<string> SaveImageAsync(IFormFile image)
    {
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "venues");
        Directory.CreateDirectory(uploadDirectory);
        await using var stream = System.IO.File.Create(Path.Combine(uploadDirectory, fileName));
        await image.CopyToAsync(stream);
        return $"/uploads/venues/{fileName}";
    }

    private async Task AuditAsync(string action, string entityType, string entityId, string details)
    {
        _db.AdminAuditLogs.Add(new AdminAuditLog
        {
            OccurredAtUtc = DateTime.UtcNow, AdminName = User.Identity?.Name ?? "Admin",
            Action = action, EntityType = entityType, EntityId = entityId, Details = details
        });
        await _db.SaveChangesAsync();
    }
}
