using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportMatch.Web.Models.ViewModels;
using SportMatch.Web.Options;
using SportMatch.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using SportMatch.Web.Data.Entities;

namespace SportMatch.Web.Controllers;

[ApiController]
[Route("api/payments/sepay")]
public sealed class PaymentWebhookController(IBookingStore store, SportMatch.Web.Data.SportMatchDbContext db, IOptions<PaymentSettings> options) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("webhook")]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Receive(SePayWebhookViewModel payload)
    {
        payload.AccountNumber ??= string.Empty;
        payload.Content ??= string.Empty;
        payload.TransferType ??= string.Empty;
        var settings = options.Value;
        var expected = $"Apikey {settings.WebhookApiKey}";
        if (string.IsNullOrWhiteSpace(settings.WebhookApiKey) ||
            !string.Equals(Request.Headers.Authorization, expected, StringComparison.Ordinal))
        {
            await WriteLogAsync(payload, false, "Unauthorized");
            return Unauthorized();
        }
        if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(payload.AccountNumber, settings.AccountNumber, StringComparison.Ordinal))
        {
            await WriteLogAsync(payload, false, "Sai tài khoản hoặc loại giao dịch");
            return BadRequest(new { success = false });
        }

        var confirmed = await store.ConfirmPaymentAsync(payload.Id, payload.Code, payload.Content, payload.TransferAmount);
        if (!confirmed)
        {
            if (await db.MatchJoinRequests.AnyAsync(x => x.PaymentTransactionId == payload.Id)) confirmed = true;
            else
            {
                var requests = await db.MatchJoinRequests.Include(x => x.MatchPost).ThenInclude(x => x!.JoinRequests).Where(x => x.Status == "Chờ thanh toán").ToListAsync();
                var joinRequest = requests.FirstOrDefault(x =>
                    string.Equals(x.RequestCode, payload.Code, StringComparison.OrdinalIgnoreCase) ||
                    payload.Content.Contains(x.RequestCode, StringComparison.OrdinalIgnoreCase));
                if (joinRequest is not null && joinRequest.HoldExpiresAtUtc >= DateTime.UtcNow && payload.TransferAmount >= joinRequest.DepositAmount)
                {
                    joinRequest.Status = "Đã xác nhận";
                    joinRequest.PaymentTransactionId = payload.Id;
                    joinRequest.PaidAtUtc = DateTime.UtcNow;
                    if (joinRequest.MatchPost!.JoinRequests.Count(x => x.Status == "Đã xác nhận") + 1 >= joinRequest.MatchPost.NeededPlayers)
                        joinRequest.MatchPost.Status = "Đã đủ người";
                    await db.SaveChangesAsync();
                    confirmed = true;
                }
            }
        }
        await WriteLogAsync(payload, confirmed, confirmed ? "Đã đối soát" : "Không tìm thấy mã hợp lệ hoặc QR hết hạn");
        return confirmed ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    private async Task WriteLogAsync(SePayWebhookViewModel payload, bool accepted, string result)
    {
        db.PaymentWebhookLogs.Add(new PaymentWebhookLog
        {
            TransactionId = payload.Id, ReceivedAtUtc = DateTime.UtcNow,
            AccountNumber = payload.AccountNumber.Length > 30 ? payload.AccountNumber[..30] : payload.AccountNumber,
            Amount = payload.TransferAmount,
            Content = payload.Content.Length > 500 ? payload.Content[..500] : payload.Content,
            Accepted = accepted, Result = result
        });
        await db.SaveChangesAsync();
    }
}
