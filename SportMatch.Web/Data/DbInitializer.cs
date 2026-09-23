using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SportMatch.Web.Data.Entities;
using Microsoft.Extensions.Options;
using SportMatch.Web.Options;

namespace SportMatch.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<SportMatchDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminOptions = services.GetRequiredService<IOptions<AdminBootstrapOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        foreach (var role in new[] { "Admin" })
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));

        var existingAdmin = await userManager.FindByEmailAsync(adminOptions.Email);
        if (existingAdmin is null && !string.IsNullOrWhiteSpace(adminOptions.Password))
            await SeedUserAsync(userManager, adminOptions.Email, adminOptions.PhoneNumber, adminOptions.FullName, "Admin", adminOptions.Password);
        else if (existingAdmin is null)
            logger.LogWarning("Chưa tạo tài khoản Admin. Hãy cấu hình AdminBootstrap__Password bằng biến môi trường trước lần chạy đầu tiên.");

        if (!await db.MatchPosts.AnyAsync())
        {
            db.MatchPosts.AddRange(
                BuildMatch("MKDEMO01", "football", "Bóng đá mini", "Quốc Anh", "0912345678", "Sân bóng KTX Khu B", 1, new TimeOnly(19, 0), "Intermediate", 2, 70000),
                BuildMatch("MKDEMO02", "badminton", "Cầu lông", "Ngọc Mai", "0988123456", "Smash Badminton", 2, new TimeOnly(18, 30), "Casual", 1, 45000),
                BuildMatch("MKDEMO03", "pickleball", "Pickleball", "Huy Hoàng", "0909123456", "Saigon Pickle Club", 3, new TimeOnly(19, 0), "Competitive", 2, 90000));
            await db.SaveChangesAsync();
        }

        if (await db.VenueComplexes.AnyAsync()) return;

        db.VenueComplexes.AddRange(
            BuildVenue("Cụm sân Thể thao SunSport Gò Vấp", "18A Phan Văn Trị, Phường 10, Quận Gò Vấp, TP.HCM", "Gò Vấp", "0987654321", true),
            BuildVenue("SportHub Thủ Đức", "Khu đô thị ĐHQG, TP. Thủ Đức, TP.HCM", "Thủ Đức", "0900000011", false),
            BuildVenue("Bình Thạnh Active Center", "Ung Văn Khiêm, Quận Bình Thạnh, TP.HCM", "Bình Thạnh", "0900000012", false));
        await db.SaveChangesAsync();
    }

    private static MatchPost BuildMatch(string code, string sport, string sportName, string hostName, string phone, string venue, int days, TimeOnly time, string level, int players, decimal cost) => new()
    {
        MatchCode = code, Sport = sport, SportName = sportName, HostName = hostName, PhoneNumber = phone,
        VenueName = venue, District = "TP.HCM", MatchDate = DateOnly.FromDateTime(DateTime.Today.AddDays(days)), StartTime = time,
        Level = level, NeededPlayers = players, CostPerPerson = cost, Status = "Đang tuyển", CreatedAtUtc = DateTime.UtcNow
    };

    private static async Task SeedUserAsync(UserManager<ApplicationUser> manager, string email, string phone, string name, string role, string password)
    {
        if (await manager.FindByEmailAsync(email) is not null) return;
        var user = new ApplicationUser { UserName = phone, PhoneNumber = phone, Email = email, FullName = name, EmailConfirmed = true };
        var result = await manager.CreateAsync(user, password);
        if (result.Succeeded) await manager.AddToRoleAsync(user, role);
    }

    private static VenueComplex BuildVenue(string name, string address, string district, string phone, bool isSunSport)
    {
        var venue = new VenueComplex
        {
            Name = name, Address = address, District = district, PhoneNumber = phone,
            OpenTime = new TimeOnly(6, 0), CloseTime = new TimeOnly(23, 0),
            Amenities = "Wifi miễn phí; Căn tin nước giải khát; Bãi giữ xe có bảo vệ; Đèn LED; Phòng thay đồ; Tủ gửi đồ"
        };
        var courtNames = isSunSport
            ? new[] { ("Sân bóng 5 số 1", "Bóng đá mini", "Sân 5"), ("Sân bóng 7 số 1", "Bóng đá mini", "Sân 7"), ("Sân cầu lông số 1", "Cầu lông", "Tiêu chuẩn") }
            : new[] { ("Sân bóng số 1", "Bóng đá mini", "Sân 5"), ("Sân cầu lông số 1", "Cầu lông", "Tiêu chuẩn") };
        foreach (var item in courtNames)
        {
            var court = new SportCourt { Name = item.Item1, SportName = item.Item2, CourtType = item.Item3, OffPeakPrice = 180000, PeakPrice = 280000 };
            for (var hour = 6; hour < 23; hour++) court.TimeSlots.Add(new CourtTimeSlot { StartTime = new TimeOnly(hour, 0), DurationMinutes = 60 });
            venue.Courts.Add(court);
        }
        return venue;
    }
}
