using KLCN_API.Helpers;
using KLCN_API.Middleware;
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

    public PaymentsController(
        IPaymentService paymentService,
        VNPayHelper vnpay,
        MoMoHelper momo)
    {
        _paymentService = paymentService;
        _vnpay = vnpay;
        _momo = momo;
    }

    // ── VNPay ─────────────────────────────────────────────────────

    /// <summary>
    /// Tạo URL thanh toán VNPay — Customer đang chờ đặt cọc (StatusId=5)
    /// hoặc thanh toán phần còn lại (StatusId=2).
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
    /// IPN callback — VNPay gọi server-to-server (GET).
    /// Không yêu cầu JWT. Phải trả đúng format VNPay yêu cầu.
    /// </summary>
    [HttpGet("vnpay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayIPN()
    {
        if (!_vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess))
            return Ok(new { RspCode = "97", Message = "Invalid signature" });

        if (!isSuccess)
            return Ok(new { RspCode = "00", Message = "Confirmed failure" });

        // txnRef = "{bookingId}_{timestamp}"
        if (!int.TryParse(txnRef.Split('_')[0], out var bookingId))
            return Ok(new { RspCode = "01", Message = "Invalid order" });

        if (!decimal.TryParse(Request.Query["vnp_Amount"].ToString(), out var rawAmount))
            return Ok(new { RspCode = "01", Message = "Invalid amount" });

        var amount = rawAmount / 100; // VNPay gửi đơn vị VNĐ * 100
        var txnCode = Request.Query["vnp_TransactionNo"].ToString();

        await _paymentService.RecordOnlinePaymentAsync(
            bookingId, amount, methodId: 3 /* VNPay */, txnCode);

        return Ok(new { RspCode = "00", Message = "Confirm Success" });
    }

    /// <summary>
    /// Return URL — VNPay redirect trình duyệt về sau khi khách thanh toán.
    /// Chỉ hiển thị kết quả, KHÔNG cập nhật DB (IPN đảm nhận việc đó).
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public IActionResult VNPayReturn()
    {
        var isValid = _vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess);
        var bookingId = txnRef.Split('_').FirstOrDefault() ?? "0";

        var frontendUrl = isValid && isSuccess
            ? $"https://yourfrontend.com/booking/{bookingId}/success"
            : $"https://yourfrontend.com/booking/{bookingId}/failed";

        return Redirect(frontendUrl);
    }

    // ── MoMo ──────────────────────────────────────────────────────

    /// <summary>
    /// Tạo URL thanh toán MoMo — Customer đang chờ đặt cọc (StatusId=5)
    /// hoặc thanh toán phần còn lại (StatusId=2).
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
    /// IPN callback — MoMo gọi server-to-server (POST JSON).
    /// Không yêu cầu JWT.
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
    /// Return URL — MoMo redirect trình duyệt về sau khi thanh toán.
    /// Chỉ hiển thị kết quả.
    /// </summary>
    [HttpGet("momo/return")]
    [AllowAnonymous]
    public IActionResult MoMoReturn(
        [FromQuery] string orderId, [FromQuery] int resultCode)
    {
        var bookingId = MoMoHelper.ParseBookingId(orderId);

        var frontendUrl = resultCode == 0
            ? $"https://yourfrontend.com/booking/{bookingId}/success"
            : $"https://yourfrontend.com/booking/{bookingId}/failed";

        return Redirect(frontendUrl);
    }
}