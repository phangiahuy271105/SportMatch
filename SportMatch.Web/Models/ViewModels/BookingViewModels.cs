using System.ComponentModel.DataAnnotations;

namespace SportMatch.Web.Models.ViewModels;

public sealed class BookingIndexViewModel
{
    public BookingFilterViewModel Filter { get; init; } = new();
    public IReadOnlyList<SportOptionViewModel> Sports { get; init; } = [];
    public IReadOnlyList<VenueCardViewModel> Venues { get; init; } = [];
    public IReadOnlyList<QuickMatchViewModel> QuickMatches { get; init; } = [];
}

public sealed class BookingFilterViewModel
{
    [Display(Name = "Môn thể thao")]
    public string Sport { get; set; } = "all";

    [Display(Name = "Khu vực")]
    public string District { get; set; } = "all";

    [DataType(DataType.Date)]
    [Display(Name = "Ngày chơi")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public sealed class SportOptionViewModel
{
    public required string Value { get; init; }
    public required string Name { get; init; }
    public required string Icon { get; init; }
}

public sealed class VenueCardViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Sport { get; init; }
    public required string SportName { get; init; }
    public required string District { get; init; }
    public required string Address { get; init; }
    public required string VisualClass { get; init; }
    public string? ImagePath { get; init; }
    public decimal PricePerHour { get; init; }
    public decimal PeakPrice { get; init; }
    public decimal Rating { get; init; }
    public int ReviewCount { get; init; }
    public int AvailableSlotCount { get; init; }
    public IReadOnlyList<string> Features { get; init; } = [];
    public IReadOnlyList<TimeSlotViewModel> TimeSlots { get; init; } = [];
}

public sealed class TimeSlotViewModel
{
    public required string StartTime { get; init; }
    public bool IsAvailable { get; init; }
}

public sealed class QuickMatchViewModel
{
    public int Id { get; init; }
    public required string SportName { get; init; }
    public required string Icon { get; init; }
    public required string Title { get; init; }
    public required string VenueName { get; init; }
    public required string TimeRange { get; init; }
    public int AvailableSpots { get; init; }
}

public sealed class CreateBookingViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn sân hợp lệ.")]
    public int VenueId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày chơi.")]
    [DataType(DataType.Date)]
    public DateOnly BookingDate { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khung giờ.")]
    [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Khung giờ không hợp lệ.")]
    public string StartTime { get; set; } = string.Empty;

    [Range(1, 4, ErrorMessage = "Chỉ có thể đặt từ 1 đến 4 khung giờ liên tiếp.")]
    public int SlotCount { get; set; } = 1;

    [Required(ErrorMessage = "Vui lòng nhập họ tên người đặt.")]
    [StringLength(80, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string? Email { get; set; }

    public bool OpenForMatchmaking { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý điều khoản và chính sách trước khi đặt sân.")]
    public bool AcceptPolicy { get; set; }
}
