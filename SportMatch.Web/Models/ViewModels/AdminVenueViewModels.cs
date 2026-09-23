using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SportMatch.Web.Models.ViewModels;

public sealed class AdminVenueListViewModel
{
    public IReadOnlyList<AdminVenueRowViewModel> Venues { get; init; } = [];
}

public sealed class AdminVenueRowViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string District { get; init; }
    public required string PhoneNumber { get; init; }
    public required string OpeningHours { get; init; }
    public string? ImagePath { get; init; }
    public bool IsActive { get; init; }
    public int CourtCount { get; init; }
    public IReadOnlyList<string> Sports { get; init; } = [];
}

public sealed class AdminVenueFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên cụm sân.")]
    [StringLength(150)]
    [Display(Name = "Tên cụm sân")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(250)]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập quận/huyện.")]
    [StringLength(50)]
    [Display(Name = "Quận/Huyện")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Giờ mở cửa")]
    public TimeOnly OpenTime { get; set; } = new(6, 0);

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Giờ đóng cửa")]
    public TimeOnly CloseTime { get; set; } = new(23, 0);

    [StringLength(500)]
    [Display(Name = "Tiện ích")]
    public string Amenities { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? Image { get; set; }
    public string? ExistingImagePath { get; set; }
    public bool RemoveImage { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sân con.")]
    [StringLength(100)]
    [Display(Name = "Tên sân con")]
    public string CourtName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn môn thể thao.")]
    [Display(Name = "Môn thể thao")]
    public string SportName { get; set; } = "Bóng đá mini";

    [Required]
    [StringLength(30)]
    [Display(Name = "Loại sân")]
    public string CourtType { get; set; } = "Sân 5";

    [Range(0, 10000000, ErrorMessage = "Giá giờ thường không hợp lệ.")]
    [Display(Name = "Giá giờ thường")]
    public decimal OffPeakPrice { get; set; } = 180000;

    [Range(0, 10000000, ErrorMessage = "Giá giờ vàng không hợp lệ.")]
    [Display(Name = "Giá giờ vàng")]
    public decimal PeakPrice { get; set; } = 280000;
}
