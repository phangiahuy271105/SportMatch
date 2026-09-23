namespace SportMatch.Web.Options;

public sealed class PaymentSettings
{
    public const string SectionName = "Payment";
    public string BankCode { get; set; } = "MB";
    public string BankName { get; set; } = "MBBank";
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal BookingDepositPercent { get; set; } = 30;
    public decimal MatchDepositPercent { get; set; } = 50;
    public int HoldingMinutes { get; set; } = 10;
    public string WebhookApiKey { get; set; } = string.Empty;
    public string ZaloUrl { get; set; } = string.Empty;
}
