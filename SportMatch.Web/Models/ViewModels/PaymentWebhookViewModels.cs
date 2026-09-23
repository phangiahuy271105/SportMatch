using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace SportMatch.Web.Models.ViewModels;

public sealed class SePayWebhookViewModel
{
    [JsonPropertyName("id"), Range(1, long.MaxValue)] public long Id { get; set; }
    [JsonPropertyName("accountNumber"), Required, StringLength(30)] public string AccountNumber { get; set; } = string.Empty;
    [JsonPropertyName("code"), StringLength(100)] public string? Code { get; set; }
    [JsonPropertyName("content"), Required, StringLength(500)] public string Content { get; set; } = string.Empty;
    [JsonPropertyName("transferType"), Required, StringLength(10)] public string TransferType { get; set; } = string.Empty;
    [JsonPropertyName("transferAmount"), Range(typeof(decimal), "1", "1000000000")] public decimal TransferAmount { get; set; }
}
