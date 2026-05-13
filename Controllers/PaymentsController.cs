//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//// Controllers/PaymentsController.cs
//[ApiController]
//[Route("api/payments")]
//public class PaymentsController : ControllerBase
//{
//    private readonly IPaymentService _paymentService;
//    private readonly VNPayHelper _vnpay;

//    public PaymentsController(IPaymentService paymentService, VNPayHelper vnpay)
//    {
//        _paymentService = paymentService;
//        _vnpay = vnpay;
//    }

//    /// <summary>Tạo URL thanh toán VNPay cho booking.</summary>
//    [HttpPost("vnpay/create/{bookingId:int}")]
//    [Authorize]
//    public async Task<IActionResult> CreateVNPay(int bookingId)
//    {
//        var booking = await _paymentService.GetBookingForPaymentAsync(bookingId);
//        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

//        var url = _vnpay.CreatePaymentUrl(
//            bookingId,
//            booking.TotalAmount ?? 0,
//            $"Thanh toan san bong SportPlus - Booking #{bookingId}",
//            ip);

//        return Ok(ApiResponse<object>.Ok(new { paymentUrl = url }));
//    }

//    /// <summary>
//    /// IPN callback — VNPay gọi server-to-server để xác nhận kết quả.
//    /// Endpoint này KHÔNG yêu cầu JWT (VNPay server gọi trực tiếp).
//    /// </summary>
//    [HttpGet("vnpay/ipn")]
//    [AllowAnonymous]
//    public async Task<IActionResult> VNPayIPN()
//    {
//        if (!_vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess))
//            return Ok(new { RspCode = "97", Message = "Invalid signature" });

//        if (!isSuccess)
//            return Ok(new { RspCode = "00", Message = "Confirmed failure" });

//        // txnRef = "{bookingId}_{timestamp}"
//        var bookingId = int.Parse(txnRef.Split('_')[0]);
//        var amount = decimal.Parse(Request.Query["vnp_Amount"]!) / 100;
//        var txnCode = Request.Query["vnp_TransactionNo"].ToString();

//        await _paymentService.RecordOnlinePaymentAsync(
//            bookingId, amount, methodId: 3 /* VNPay */, txnCode);

//        // VNPay yêu cầu response đúng format này
//        return Ok(new { RspCode = "00", Message = "Confirm Success" });
//    }

//    /// <summary>
//    /// Return URL — redirect sau khi khách thanh toán xong.
//    /// Chỉ dùng để hiển thị kết quả, KHÔNG cập nhật DB ở đây.
//    /// </summary>
//    [HttpGet("vnpay/return")]
//    [AllowAnonymous]
//    public IActionResult VNPayReturn()
//    {
//        var isValid = _vnpay.ValidateSignature(Request.Query, out var txnRef, out var isSuccess);
//        var bookingId = txnRef.Split('_')[0];

//        // Redirect về frontend với kết quả
//        var frontendUrl = isValid && isSuccess
//            ? $"https://yourfrontend.com/booking/{bookingId}/success"
//            : $"https://yourfrontend.com/booking/{bookingId}/failed";

//        return Redirect(frontendUrl);
//    }
//}