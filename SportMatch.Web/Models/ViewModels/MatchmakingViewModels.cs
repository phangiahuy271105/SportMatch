using System.ComponentModel.DataAnnotations;

namespace SportMatch.Web.Models.ViewModels;

public sealed class MatchmakingIndexViewModel
{
    public string ActiveSport { get; init; } = "all";
    public IReadOnlyList<MatchCardViewModel> Matches { get; init; } = [];
    public CreateMatchViewModel CreateForm { get; init; } = new();
    public IReadOnlyList<OwnedMatchViewModel> OwnedMatches { get; init; } = [];
}

public sealed class MatchCardViewModel
{
    public int Id { get; init; }
    public required string Sport { get; init; }
    public required string SportName { get; init; }
    public required string Title { get; init; }
    public required string VenueName { get; init; }
    public required string District { get; init; }
    public DateOnly MatchDate { get; init; }
    public required string TimeRange { get; init; }
    public required string Level { get; init; }
    public int AvailableSpots { get; init; }
    public decimal CostPerPerson { get; init; }
    public required string HostName { get; init; }
    public required string MatchCode { get; init; }
}

public sealed class CreateMatchViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên chủ kèo.")]
    [StringLength(80, MinimumLength = 2)]
    [Display(Name = "Tên chủ kèo")]
    public string HostName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn môn thể thao.")]
    [Display(Name = "Môn thể thao")]
    public string Sport { get; set; } = "football";

    [Required(ErrorMessage = "Vui lòng nhập tên sân.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên sân phải từ 3 đến 100 ký tự.")]
    [Display(Name = "Sân thi đấu")]
    public string VenueName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn ngày chơi.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày chơi")]
    public DateOnly MatchDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Vui lòng nhập giờ bắt đầu.")]
    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Giờ bắt đầu không hợp lệ.")]
    [Display(Name = "Giờ bắt đầu")]
    public string StartTime { get; set; } = "19:00";

    [Range(1, 20, ErrorMessage = "Số người cần thêm phải từ 1 đến 20.")]
    [Display(Name = "Số người cần thêm")]
    public int NeededPlayers { get; set; } = 2;

    [Required(ErrorMessage = "Vui lòng chọn trình độ.")]
    [Display(Name = "Trình độ")]
    public string Level { get; set; } = "Vui vẻ";

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Chi phí mỗi người không hợp lệ.")]
    [Display(Name = "Chi phí mỗi người")]
    public decimal CostPerPerson { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý điều khoản và chính sách trước khi đăng kèo.")]
    public bool AcceptPolicy { get; set; }
}

public sealed class JoinMatchViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Kèo không hợp lệ.")]
    public int MatchId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    public string ApplicantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý điều khoản và chính sách trước khi tham gia.")]
    public bool AcceptPolicy { get; set; }
}

public sealed class OwnedMatchViewModel
{
    public required string MatchCode { get; init; }
    public required string Title { get; init; }
    public DateOnly MatchDate { get; init; }
    public required string StartTime { get; init; }
    public required string Status { get; init; }
    public int PendingRequests { get; init; }
}

public sealed class MatchManageViewModel
{
    public required MatchCardViewModel Match { get; init; }
    public IReadOnlyList<MatchRequestViewModel> Requests { get; init; } = [];
}

public sealed class MatchRequestViewModel
{
    public required string RequestCode { get; init; }
    public required string ApplicantName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Status { get; init; }
    public decimal DepositAmount { get; init; }
    public DateTime? HoldExpiresAt { get; init; }
}

public sealed class MatchRequestStatusViewModel
{
    public required string RequestCode { get; init; }
    public required string Status { get; init; }
    public required string MatchTitle { get; init; }
    public required string VenueName { get; init; }
    public DateOnly MatchDate { get; init; }
    public required string StartTime { get; init; }
    public decimal DepositAmount { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? QrCodeUrl { get; init; }
    public string? HostName { get; init; }
    public string? HostPhone { get; init; }
}

public sealed class MyMatchesViewModel
{
    public IReadOnlyList<OwnedMatchViewModel> HostedMatches { get; init; } = [];
    public IReadOnlyList<MyJoinRequestViewModel> JoinRequests { get; init; } = [];
}

public sealed class MyJoinRequestViewModel
{
    public required string RequestCode { get; init; }
    public required string MatchTitle { get; init; }
    public required string VenueName { get; init; }
    public DateOnly MatchDate { get; init; }
    public required string StartTime { get; init; }
    public required string Status { get; init; }
    public decimal DepositAmount { get; init; }
}
