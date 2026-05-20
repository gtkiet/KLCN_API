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
    ///   PendingPayment (1) → charge full TotalAmount
    ///   Confirmed      (2) → charge TotalAmount - TổngĐãTrả (phần còn lại)
    ///
    /// FIX MOBILE: URL tạo ra nhúng platform=mobile vào ReturnUrl khi gọi từ app,
    /// để VNPayReturn biết redirect về deep link thay vì web frontend.
    /// Flutter truyền header X-Platform: mobile hoặc query ?platform=mobile.
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

        // FIX MOBILE: Nhúng platform=mobile vào ReturnUrl để VNPayReturn
        // redirect đúng về deep link thay vì web frontend.
        // VNPay sandbox không thay đổi ReturnUrl nên cách này hoạt động.
        var isMobileRequest = platform == "mobile"
            || Request.Headers["X-Platform"].ToString()
                      .Equals("mobile", StringComparison.OrdinalIgnoreCase);

        var url = _vnpay.CreatePaymentUrl(
            bookingId,
            amountDue,
            $"Thanh toan san bong SportPlus - Booking #{bookingId}",
            ip,
            // Truyền platform xuống helper để embed vào ReturnUrl query string
            isMobileRequest ? "mobile" : null);

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
    /// Không yêu cầu JWT. Đây là nơi DUY NHẤT cập nhật DB (production).
    ///
    /// VẤN ĐỀ SANDBOX: VNPay sandbox thường KHÔNG gọi IPN.
    /// Return endpoint đã có fallback xử lý DB để bù — xem VNPayReturn.
    /// Khi lên production IPN sẽ hoạt động bình thường và idempotency
    /// đảm bảo không bị ghi đôi dù cả hai cùng chạy.
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
            bookingId, amount, methodId: (int)PaymentMethodEnum.VNPay, txnCode);

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    /// <summary>
    /// Return — VNPay redirect trình duyệt về đây sau khi khách hoàn tất thanh toán.
    ///
    /// SANDBOX FALLBACK: Sandbox không gọi IPN nên Return phải cập nhật DB ở đây.
    /// RecordOnlinePaymentAsync idempotent theo transactionCode — không bị ghi đôi
    /// khi production có cả IPN lẫn Return cùng chạy.
    ///
    /// FIX MOBILE: Đọc ?platform=mobile từ ReturnUrl (được nhúng lúc tạo URL).
    /// Nếu là mobile → redirect sang deep link "sportplus://payment/result?..."
    /// thay vì web frontend URL.
    ///
    /// Luồng mobile:
    ///   Flutter → POST /vnpay/create?platform=mobile
    ///          → nhận paymentUrl (có ReturnUrl chứa &platform=mobile)
    ///          → mở url_launcher / webview
    ///          → VNPay xong → GET /vnpay/return?platform=mobile&...
    ///          → server redirect → sportplus://payment/result?status=success&bookingId=X
    ///          → Flutter bắt deep link, đóng browser, hiển thị kết quả
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayReturn([FromQuery] string? platform = null)
    {
        var isValid = _vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess);
        var bookingId = int.TryParse(txnRef.Split('_')[0], out var id) ? id : 0;

        // SANDBOX FALLBACK: cập nhật DB ở đây vì IPN sandbox thường không gọi
        if (isValid && isSuccess && bookingId > 0
            && decimal.TryParse(Request.Query["vnp_Amount"].ToString(), out var rawAmount))
        {
            var amount = rawAmount / 100m;
            var txnCode = Request.Query["vnp_TransactionNo"].ToString();

            // idempotent — gọi nhiều lần cũng chỉ ghi 1 lần nhờ ExistsByTransactionCodeAsync
            await _paymentService.RecordOnlinePaymentAsync(
                bookingId, amount, methodId: (int)PaymentMethodEnum.VNPay, txnCode);
        }

        // FIX MOBILE: platform được nhúng vào ReturnUrl khi tạo URL
        var isMobile = platform == "mobile"
                    || Request.Headers.UserAgent.ToString()
                              .Contains("Flutter", StringComparison.OrdinalIgnoreCase);

        string redirectUrl;

        if (isMobile && _frontend.HasMobileDeepLink)
        {
            // → sportplus://payment/result?status=success&bookingId=88
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