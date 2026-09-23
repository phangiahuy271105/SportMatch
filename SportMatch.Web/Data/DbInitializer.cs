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
        {
            if (await roleManager.RoleExistsAsync(role)) continue;
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Không thể tạo quyền {role}: {string.Join("; ", roleResult.Errors.Select(x => x.Description))}");
        }

        var existingAdmin = await userManager.FindByEmailAsync(adminOptions.Email);
        if (existingAdmin is null && !string.IsNullOrWhiteSpace(adminOptions.Password))
            await SeedUserAsync(userManager, adminOptions.Email, adminOptions.PhoneNumber, adminOptions.FullName, "Admin", adminOptions.Password);
        else if (existingAdmin is null)
            logger.LogWarning("Chưa tạo tài khoản Admin. Hãy cấu hình AdminBootstrap:Password bằng user secrets hoặc AdminBootstrap__Password bằng biến môi trường.");

        // Dữ liệu demo chỉ được tạo cho database mới. Database đang sử dụng có
        // ít nhất một cụm sân sẽ được giữ nguyên, kể cả ảnh do admin đã tải lên.
        if (await db.VenueComplexes.AnyAsync()) return;

        db.VenueComplexes.AddRange(
            BuildVenue(
                "Cụm sân Thể thao SunSport Gò Vấp",
                "18A Phan Văn Trị, Phường 10, Quận Gò Vấp, TP.HCM",
                "Gò Vấp",
                "0987654321",
                "/images/demo/sunsport-football-5.png",
                ("Sân bóng 5 số 1", "Bóng đá mini", "Sân 5", "/images/demo/sunsport-football-5.png"),
                ("Sân bóng 7 số 1", "Bóng đá mini", "Sân 7", "/images/demo/sunsport-football-7.webp"),
                ("Sân cầu lông số 1", "Cầu lông", "Tiêu chuẩn", "/images/demo/sunsport-badminton.jpg")),
            BuildVenue(
                "SportHub Thủ Đức",
                "Khu đô thị ĐHQG, TP. Thủ Đức, TP.HCM",
                "Thủ Đức",
                "0900000011",
                "/images/demo/sporthub-football.png",
                ("Sân bóng số 1", "Bóng đá mini", "Sân 5", "/images/demo/sporthub-football.png"),
                ("Sân cầu lông số 1", "Cầu lông", "Tiêu chuẩn", "/images/demo/sporthub-badminton.jpg")),
            BuildVenue(
                "Bình Thạnh Active Center",
                "Ung Văn Khiêm, Quận Bình Thạnh, TP.HCM",
                "Bình Thạnh",
                "0900000012",
                "/images/demo/binh-thanh-football.png",
                ("Sân bóng số 1", "Bóng đá mini", "Sân 5", "/images/demo/binh-thanh-football.png"),
                ("Sân cầu lông số 1", "Cầu lông", "Tiêu chuẩn", "/images/demo/binh-thanh-badminton.jpg")));

        db.MatchPosts.AddRange(
            BuildMatch("MKDEMO01", "football", "Bóng đá mini", "Quốc Anh", "0912345678", "Sân bóng KTX Khu B", 1, new TimeOnly(19, 0), "Intermediate", 2, 70000),
            BuildMatch("MKDEMO02", "badminton", "Cầu lông", "Ngọc Mai", "0988123456", "Smash Badminton", 2, new TimeOnly(18, 30), "Casual", 1, 45000),
            BuildMatch("MKDEMO03", "pickleball", "Pickleball", "Huy Hoàng", "0909123456", "Saigon Pickle Club", 3, new TimeOnly(19, 0), "Competitive", 2, 90000));
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
        var user = new ApplicationUser
        {
            UserName = string.IsNullOrWhiteSpace(phone) ? email : phone,
            PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone,
            Email = email,
            FullName = name,
            EmailConfirmed = true
        };
        var result = await manager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Không thể tạo tài khoản admin: {string.Join("; ", result.Errors.Select(x => x.Description))}");

        var roleResult = await manager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException($"Không thể gán quyền admin: {string.Join("; ", roleResult.Errors.Select(x => x.Description))}");
    }

    private static VenueComplex BuildVenue(
        string name,
        string address,
        string district,
        string phone,
        string imagePath,
        params (string Name, string SportName, string CourtType, string ImagePath)[] courts)
    {
        var venue = new VenueComplex
        {
            Name = name, Address = address, District = district, PhoneNumber = phone,
            OpenTime = new TimeOnly(6, 0), CloseTime = new TimeOnly(23, 0),
            Amenities = "Wifi miễn phí; Căn tin nước giải khát; Bãi giữ xe có bảo vệ; Đèn LED; Phòng thay đồ; Tủ gửi đồ",
            ImagePath = imagePath
        };

        foreach (var item in courts)
        {
            var court = new SportCourt
            {
                Name = item.Name,
                SportName = item.SportName,
                CourtType = item.CourtType,
                ImagePath = item.ImagePath,
                OffPeakPrice = 180000,
                PeakPrice = 280000
            };
            for (var hour = 6; hour < 23; hour++) court.TimeSlots.Add(new CourtTimeSlot { StartTime = new TimeOnly(hour, 0), DurationMinutes = 60 });
            venue.Courts.Add(court);
        }
        return venue;
    }
}
