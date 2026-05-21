using KLCN_API.Configurations;
using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly VNPayHelper _vnpay;
    private readonly FrontendSettings _frontend;

    public PaymentsController(
        IPaymentService paymentService,
        VNPayHelper vnpay,
        FrontendSettings frontend)
    {
        _paymentService = paymentService;
        _vnpay = vnpay;
        _frontend = frontend;
    }

    // ── VNPay ─────────────────────────────────────────────────────

    /// <summary>
    /// Tạo URL thanh toán VNPay.
    ///
    /// Format txnRef:
    ///   "{bookingId}_{timestamp}"          (web)
    ///   "{bookingId}_{timestamp}_mobile"   (mobile)
    /// </summary>
    [HttpPost("vnpay/create/{bookingId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> CreateVNPay(
        int bookingId,
        [FromQuery] string? platform = null)
    {
        var booking = await _paymentService.GetBookingForPaymentAsync(bookingId);
        var amountDue = await _paymentService.GetAmountDueAsync(bookingId, booking);

        var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                 ?? "127.0.0.1";

        var isMobile = platform == "mobile"
            || Request.Headers["X-Platform"].ToString()
                      .Equals("mobile", StringComparison.OrdinalIgnoreCase);

        var txnRefSuffix = isMobile ? "_mobile" : string.Empty;

        var url = _vnpay.CreatePaymentUrl(
            bookingId,
            amountDue,
            $"Thanh toan san bong SportPlus - Booking #{bookingId}",
            ip,
            txnRefSuffix);

        return Ok(ApiResponse<object>.Ok(new
        {
            paymentUrl = url,
            amountDue,
            bookingStatus = booking.Status,
            bookingStatusId = booking.StatusId
        }));
    }

    /// <summary>
    /// IPN — VNPay gọi server-to-server.
    /// Sandbox thường không gọi IPN; Return endpoint có fallback.
    /// </summary>
    [HttpGet("vnpay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayIPN()
    {
        if (!_vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess))
            return Ok(new { RspCode = "97", Message = "Invalid signature" });

        if (!isSuccess)
            return Ok(new { RspCode = "00", Message = "Confirmed failure" });

        var bookingId = ParseBookingId(txnRef);
        if (bookingId <= 0)
            return Ok(new { RspCode = "01", Message = "Invalid order" });

        if (!decimal.TryParse(Request.Query["vnp_Amount"].ToString(), out var rawAmount))
            return Ok(new { RspCode = "01", Message = "Invalid amount" });

        var txnCode = Request.Query["vnp_TransactionNo"].ToString();

        await _paymentService.RecordOnlinePaymentAsync(
            bookingId, rawAmount / 100m, (int)PaymentMethodEnum.VNPay, txnCode);

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    /// <summary>
    /// Return — VNPay redirect trình duyệt về đây.
    ///
    /// Sandbox fallback: cập nhật DB ở đây vì IPN sandbox không gọi.
    /// Idempotent → không bị ghi đôi khi production có cả IPN lẫn Return.
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayReturn()
    {
        var isValid = _vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess);
        var bookingId = ParseBookingId(txnRef);

        // Sandbox fallback — idempotent nhờ ExistsByTransactionCodeAsync
        if (isValid && isSuccess && bookingId > 0
            && decimal.TryParse(Request.Query["vnp_Amount"].ToString(), out var rawAmount))
        {
            var txnCode = Request.Query["vnp_TransactionNo"].ToString();
            await _paymentService.RecordOnlinePaymentAsync(
                bookingId, rawAmount / 100m, (int)PaymentMethodEnum.VNPay, txnCode);
        }

        // Đọc platform từ TxnRef suffix — ReturnUrl không bị thay đổi
        var isMobile = txnRef.EndsWith("_mobile", StringComparison.OrdinalIgnoreCase);

        string redirectUrl;

        if (isMobile && _frontend.HasMobileDeepLink)
        {
            redirectUrl = isValid && isSuccess
                ? _frontend.BuildMobileSuccessUrl(bookingId)
                : _frontend.BuildMobileFailedUrl(bookingId);
        }
        else
        {
            redirectUrl = isValid && isSuccess
                ? _frontend.BuildSuccessUrl(bookingId)
                : _frontend.BuildFailedUrl(bookingId);
        }

        return Redirect(redirectUrl);
    }

    // ── Test endpoints (sandbox only) ─────────────────────────────

    [HttpGet("test-success")]
    [AllowAnonymous]
    public IActionResult TestSuccess([FromQuery] int bookingId)
        => Ok(new { success = true, message = "Thanh toán thành công", bookingId });

    [HttpGet("test-failed")]
    [AllowAnonymous]
    public IActionResult TestFailed([FromQuery] int bookingId)
        => Ok(new { success = false, message = "Thanh toán thất bại", bookingId });

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Parse bookingId từ TxnRef.
    /// Format: "{bookingId}_{timestamp}" hoặc "{bookingId}_{timestamp}_mobile"
    /// → lấy phần đầu tiên trước '_'.
    /// </summary>
    private static int ParseBookingId(string txnRef)
    {
        if (string.IsNullOrWhiteSpace(txnRef)) return 0;
        var firstPart = txnRef.Split('_')[0];
        return int.TryParse(firstPart, out var id) ? id : 0;
    }
}