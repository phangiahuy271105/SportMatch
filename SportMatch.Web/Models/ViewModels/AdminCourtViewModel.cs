using System.ComponentModel.DataAnnotations;
namespace SportMatch.Web.Models.ViewModels;
public sealed class AdminCourtViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int VenueComplexId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, RegularExpression("^(Bóng đá mini|Cầu lông|Pickleball|Bóng rổ|Tennis|Bóng chuyền)$")] public string SportName { get; set; } = "Bóng đá mini";
    [Required, StringLength(30)] public string CourtType { get; set; } = "Tiêu chuẩn";
    [Range(0, 10000000)] public decimal OffPeakPrice { get; set; } = 180000;
    [Range(0, 10000000)] public decimal PeakPrice { get; set; } = 280000;
    public List<int> ActiveSlots { get; set; } = [];
    public List<AdminSlotViewModel> Slots { get; set; } = [];
}
public sealed class AdminSlotViewModel
{
    public int Id { get; init; }
    public string Time { get; init; } = "";
}
