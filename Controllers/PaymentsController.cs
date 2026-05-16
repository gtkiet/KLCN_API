using KLCN_API.Configurations;
using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly VNPayHelper _vnpay;
    private readonly MoMoHelper _momo;
    private readonly FrontendSettings _frontend;

    public PaymentsController(
        IPaymentService paymentService,
        VNPayHelper vnpay,
        MoMoHelper momo,
        FrontendSettings frontend)
    {
        _paymentService = paymentService;
        _vnpay = vnpay;
        _momo = momo;
        _frontend = frontend;
    }

    // ── VNPay ─────────────────────────────────────────────────────

    /// <summary>
    /// Tạo URL thanh toán VNPay.
    /// Booking phải ở StatusId=5 (chờ cọc) hoặc StatusId=2 (chờ thanh toán full).
    /// </summary>
    [HttpPost("vnpay/create/{bookingId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> CreateVNPay(int bookingId)
    {
        var booking = await _paymentService.GetBookingForPaymentAsync(bookingId);
        var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                      ?? "127.0.0.1";

        var url = _vnpay.CreatePaymentUrl(
            bookingId,
            booking.TotalAmount ?? 0,
            $"Thanh toan san bong SportPlus - Booking #{bookingId}",
            ip);

        return Ok(ApiResponse<object>.Ok(new { paymentUrl = url }));
    }

    /// <summary>
    /// IPN — VNPay gọi server-to-server sau khi giao dịch hoàn tất.
    /// Không yêu cầu JWT. Đây là nơi DUY NHẤT cập nhật DB.
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

        var amount = rawAmount / 100;
        var txnCode = Request.Query["vnp_TransactionNo"].ToString();

        await _paymentService.RecordOnlinePaymentAsync(
            bookingId, amount, methodId: 3 /* VNPay */, txnCode);

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    /// <summary>
    /// Return — VNPay redirect về sau khi thanh toán.
    /// Web  → redirect sang web frontend URL.
    /// Mobile Flutter → redirect sang deep link "sportplus://payment/result?..."
    ///   Flutter bắt link bằng uni_links, đọc status + bookingId,
    ///   rồi gọi GET /api/bookings/{bookingId} để lấy trạng thái mới nhất.
    /// KHÔNG cập nhật DB ở đây — IPN đã làm rồi.
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public IActionResult VNPayReturn([FromQuery] string? platform = null)
    {
        var isValid = _vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess);
        var bookingId = int.TryParse(txnRef.Split('_')[0], out var id) ? id : 0;
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

    // ── MoMo ──────────────────────────────────────────────────────

    /// <summary>
    /// Tạo URL thanh toán MoMo.
    /// Booking phải ở StatusId=5 (chờ cọc) hoặc StatusId=2 (chờ thanh toán full).
    /// </summary>
    [HttpPost("momo/create/{bookingId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> CreateMoMo(int bookingId)
    {
        var booking = await _paymentService.GetBookingForPaymentAsync(bookingId);

        var payUrl = await _momo.CreatePaymentAsync(
            bookingId,
            booking.TotalAmount ?? 0,
            $"Thanh toan san bong SportPlus - Booking #{bookingId}");

        return Ok(ApiResponse<object>.Ok(new { paymentUrl = payUrl }));
    }

    /// <summary>
    /// IPN — MoMo gọi server-to-server (POST JSON) sau khi giao dịch hoàn tất.
    /// Không yêu cầu JWT. Đây là nơi DUY NHẤT cập nhật DB.
    /// </summary>
    [HttpPost("momo/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> MoMoIPN([FromBody] JsonElement body)
    {
        string Get(string key) =>
            body.TryGetProperty(key, out var v) ? v.GetString() ?? string.Empty : string.Empty;

        var partnerCode = Get("partnerCode");
        var orderId = Get("orderId");
        var requestId = Get("requestId");
        var amount = Get("amount");
        var orderInfo = Get("orderInfo");
        var orderType = Get("orderType");
        var transId = Get("transId");
        var resultCode = body.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
        var message = Get("message");
        var payType = Get("payType");
        var responseTime = Get("responseTime");
        var extraData = Get("extraData");
        var signature = Get("signature");

        if (!_momo.ValidateIpn(partnerCode, orderId, requestId, amount,
                orderInfo, orderType, transId, resultCode, message,
                payType, responseTime, extraData, signature))
            return Ok(new { message = "Invalid signature" });

        if (resultCode != 0)
            return Ok(new { message = "Payment failed, no action" });

        var bookingId = MoMoHelper.ParseBookingId(orderId);
        if (bookingId == 0)
            return Ok(new { message = "Invalid orderId" });

        if (!decimal.TryParse(amount, out var amountDecimal))
            return Ok(new { message = "Invalid amount" });

        await _paymentService.RecordOnlinePaymentAsync(
            bookingId, amountDecimal, methodId: 2 /* MoMo */, transId);

        return Ok(new { message = "Success" });
    }

    /// <summary>
    /// Return — MoMo redirect về sau khi thanh toán.
    /// Web  → redirect sang web frontend URL.
    /// Mobile Flutter → redirect sang deep link "sportplus://payment/result?..."
    ///   Flutter bắt link bằng uni_links, đọc status + bookingId,
    ///   rồi gọi GET /api/bookings/{bookingId} để lấy trạng thái mới nhất.
    /// KHÔNG cập nhật DB ở đây — IPN đã làm rồi.
    /// </summary>
    [HttpGet("momo/return")]
    [AllowAnonymous]
    public IActionResult MoMoReturn(
        [FromQuery] string orderId,
        [FromQuery] int resultCode,
        [FromQuery] string? platform = null)
    {
        var bookingId = MoMoHelper.ParseBookingId(orderId);
        var isMobile = platform == "mobile"
                        || Request.Headers.UserAgent.ToString()
                               .Contains("Flutter", StringComparison.OrdinalIgnoreCase);

        string redirectUrl;

        if (isMobile && _frontend.HasMobileDeepLink)
        {
            redirectUrl = resultCode == 0
                ? _frontend.BuildMobileSuccessUrl(bookingId)
                : _frontend.BuildMobileFailedUrl(bookingId);
        }
        else
        {
            redirectUrl = resultCode == 0
                ? _frontend.BuildSuccessUrl(bookingId)
                : _frontend.BuildFailedUrl(bookingId);
        }

        return Redirect(redirectUrl);
    }
}