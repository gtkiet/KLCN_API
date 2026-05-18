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
    /// Tự động tính số tiền cần thanh toán theo trạng thái:
    ///   PendingDeposit (5) → charge DepositAmount (tiền cọc)
    ///   PendingPayment (1) → charge full TotalAmount (lần duy nhất)
    ///   Confirmed      (2) → charge TotalAmount - TổngĐãTrả (phần còn lại)
    ///
    /// [FIX Bug 6 & 7] Trước đây luôn dùng TotalAmount, gây overcharge lần 2
    /// sau khi đã nộp cọc và không chuyển được sang Completed.
    /// </summary>
    [HttpPost("vnpay/create/{bookingId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> CreateVNPay(int bookingId)
    {
        var booking = await _paymentService.GetBookingForPaymentAsync(bookingId);

        // [FIX Bug 6 & 7] Tính đúng số tiền cần charge thay vì dùng TotalAmount cứng
        var amountDue = await _paymentService.GetAmountDueAsync(bookingId, booking);

        var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                      ?? "127.0.0.1";

        var url = _vnpay.CreatePaymentUrl(
            bookingId,
            amountDue,
            $"Thanh toan san bong SportPlus - Booking #{bookingId}",
            ip);

        return Ok(ApiResponse<object>.Ok(new
        {
            paymentUrl = url,
            amountDue,
            bookingStatus = booking.Status,
            bookingStatusId = booking.StatusId
        }));
    }

    /// <summary>
    /// IPN — VNPay gọi server-to-server sau khi giao dịch hoàn tất.
    /// Không yêu cầu JWT. Đây là nơi DUY NHẤT cập nhật DB.
    /// RecordOnlinePaymentAsync idempotent theo transactionCode.
    /// </summary>
    [HttpGet("vnpay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayIPN()
    {
        if (!_vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess))
            return Ok(new { RspCode = "97", Message = "Invalid signature" });

        if (!isSuccess)
            return Ok(new { RspCode = "00", Message = "Confirmed failure" });

        if (!int.TryParse(txnRef.Split('_')[0], out var bookingId))
            return Ok(new { RspCode = "01", Message = "Invalid order" });

        if (!decimal.TryParse(Request.Query["vnp_Amount"].ToString(), out var rawAmount))
            return Ok(new { RspCode = "01", Message = "Invalid amount" });

        var amount = rawAmount / 100m;
        var txnCode = Request.Query["vnp_TransactionNo"].ToString();

        await _paymentService.RecordOnlinePaymentAsync(
            bookingId, amount, methodId: 3 /* VNPay */, txnCode);

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    /// <summary>
    /// Return — VNPay redirect trình duyệt về đây sau khi thanh toán.
    /// KHÔNG cập nhật DB ở đây — IPN đã làm rồi.
    /// Fallback: nếu IPN sandbox chưa gọi thì xử lý ở đây (idempotent).
    ///
    /// Web    → 302 redirect sang web frontend URL.
    /// Mobile → 302 redirect sang deep link "sportplus://payment/result?..."
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayReturn([FromQuery] string? platform = null)
    {
        var isValid = _vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess);
        var bookingId = int.TryParse(txnRef.Split('_')[0], out var id) ? id : 0;

        // Fallback: đảm bảo DB được cập nhật ngay cả khi IPN chưa đến
        if (isValid && isSuccess && bookingId > 0
            && decimal.TryParse(Request.Query["vnp_Amount"].ToString(), out var rawAmount))
        {
            var amount = rawAmount / 100m;
            var txnCode = Request.Query["vnp_TransactionNo"].ToString();

            await _paymentService.RecordOnlinePaymentAsync(
                bookingId, amount, methodId: 3, txnCode);
        }

        var isMobile = platform == "mobile"
                    || Request.Headers.UserAgent.ToString()
                           .Contains("Flutter", StringComparison.OrdinalIgnoreCase);

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
}