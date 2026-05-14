using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IPaymentService _paymentService;

    public BookingsController(IBookingService bookingService, IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _paymentService = paymentService;
    }

    // ── Hold & create ─────────────────────────────────────────────

    /// <summary>
    /// Giữ slot tạm trong N phút — Customer.
    /// Phải gọi trước khi tạo booking để đảm bảo slot còn trống.
    /// </summary>
    [HttpPost("hold")]
    [AuthorizeRoles(RoleEnum.Customer)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> HoldSlots([FromBody] HoldSlotsRequest request)
    {
        var userId = User.GetUserId();
        await _bookingService.HoldSlotsAsync(request, userId);
        return Ok(ApiResponse.Ok(
            "Giữ slot thành công. Vui lòng hoàn tất đặt sân trong thời gian quy định."));
    }

    /// <summary>
    /// Tạo booking từ các slot đang giữ — Customer.
    /// Tự động xác nhận slot, tính tiền, tạo deposit nếu IsFullPayment=false.
    /// </summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Customer)]
    [ProducesResponseType(typeof(ApiResponse<BookingResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        var userId = User.GetUserId();
        var result = await _bookingService.CreateBookingAsync(request, userId);
        return Ok(ApiResponse<BookingResponse>.Ok(result, "Đặt sân thành công."));
    }

    // ── Read ──────────────────────────────────────────────────────

    /// <summary>Lấy danh sách booking có filter + phân trang — Admin và Staff.</summary>
    [HttpGet]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BookingSummaryResponse>>), 200)]
    public async Task<IActionResult> GetBookings([FromQuery] GetBookingsRequest request)
    {
        var result = await _bookingService.GetBookingsAsync(request);
        return Ok(ApiResponse<PagedResponse<BookingSummaryResponse>>.Ok(result));
    }

    /// <summary>Lấy danh sách booking của chính mình — Customer.</summary>
    [HttpGet("my")]
    [AuthorizeRoles(RoleEnum.Customer)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BookingSummaryResponse>>), 200)]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();
        var result = await _bookingService.GetMyBookingsAsync(userId, null, page, pageSize);
        return Ok(ApiResponse<PagedResponse<BookingSummaryResponse>>.Ok(result));
    }

    /// <summary>
    /// Lấy chi tiết 1 booking.
    /// Customer chỉ xem được booking của mình; Admin/Staff xem được tất cả.
    /// </summary>
    [HttpGet("{bookingId:int}")]
    [ProducesResponseType(typeof(ApiResponse<BookingResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetById(int bookingId)
    {
        var userId = User.GetUserId();
        var isAdminOrStaff = User.IsAdminOrStaff();
        var result = await _bookingService.GetByIdAsync(bookingId, userId, isAdminOrStaff);
        return Ok(ApiResponse<BookingResponse>.Ok(result));
    }

    // ── Actions ───────────────────────────────────────────────────

    /// <summary>
    /// Hủy booking.
    /// Customer chỉ hủy được booking của mình và còn trong hạn hủy.
    /// Admin/Staff có thể hủy bất kỳ booking nào (bỏ qua ràng buộc giờ).
    /// </summary>
    [HttpPost("{bookingId:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Cancel(
        int bookingId, [FromBody] CancelBookingRequest request)
    {
        var userId = User.GetUserId();
        var isAdminOverride = User.IsAdminOrStaff();
        await _bookingService.CancelAsync(bookingId, request, userId, isAdminOverride);
        return Ok(ApiResponse.Ok("Hủy booking thành công."));
    }

    /// <summary>Đổi lịch 1 slot trong booking — Customer (chỉ booking của mình).</summary>
    [HttpPost("{bookingId:int}/reschedule")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Reschedule(
        int bookingId, [FromBody] RescheduleRequest request)
    {
        var userId = User.GetUserId();
        await _bookingService.RescheduleAsync(bookingId, request, userId);
        return Ok(ApiResponse.Ok("Đổi lịch thành công."));
    }

    /// <summary>Áp dụng mã voucher vào booking — Customer (chỉ booking của mình).</summary>
    [HttpPost("{bookingId:int}/apply-voucher")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ApplyVoucher(
        int bookingId, [FromBody] ApplyVoucherRequest request)
    {
        var userId = User.GetUserId();
        await _bookingService.ApplyVoucherAsync(bookingId, request, userId);
        return Ok(ApiResponse.Ok("Áp dụng voucher thành công."));
    }

    // ── Payments ──────────────────────────────────────────────────

    /// <summary>
    /// Ghi nhận thanh toán đặt cọc.
    /// Customer tự nộp cọc online; Staff/Admin ghi nhận tại quầy.
    /// </summary>
    [HttpPost("{bookingId:int}/deposit")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> RecordDeposit(
        int bookingId, [FromBody] RecordDepositRequest request)
    {
        var userId = User.GetUserId();
        await _paymentService.RecordDepositAsync(bookingId, request, userId);
        return Ok(ApiResponse.Ok("Ghi nhận đặt cọc thành công."));
    }

    /// <summary>Thanh toán phần còn lại — Staff hoặc Admin (tại quầy).</summary>
    [HttpPost("{bookingId:int}/payment")]
    [AuthorizeRoles(RoleEnum.Staff, RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> RecordFullPayment(
        int bookingId, [FromBody] ConfirmPaymentRequest request)  // ConfirmPaymentRequest, không phải RecordFullPaymentRequest
    {
        var userId = User.GetUserId();
        await _paymentService.RecordFullPaymentAsync(bookingId, request, userId);
        return Ok(ApiResponse.Ok("Thanh toán thành công."));
    }

    /// <summary>Lấy lịch sử thanh toán của booking.</summary>
    [HttpGet("{bookingId:int}/payments")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetPayments(int bookingId)
    {
        // Kiểm tra quyền truy cập qua GetById trước
        var userId = User.GetUserId();
        var isAdminOrStaff = User.IsAdminOrStaff();
        await _bookingService.GetByIdAsync(bookingId, userId, isAdminOrStaff);

        var result = await _paymentService.GetPaymentsByBookingAsync(bookingId);
        return Ok(ApiResponse<List<PaymentResponse>>.Ok(result));
    }

    /// <summary>Lấy thông tin đặt cọc của booking.</summary>
    [HttpGet("{bookingId:int}/deposit")]
    [ProducesResponseType(typeof(ApiResponse<DepositResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetDeposit(int bookingId)
    {
        var userId = User.GetUserId();
        var isAdminOrStaff = User.IsAdminOrStaff();
        await _bookingService.GetByIdAsync(bookingId, userId, isAdminOrStaff);

        var result = await _paymentService.GetDepositByBookingAsync(bookingId);
        return Ok(ApiResponse<DepositResponse?>.Ok(result));
    }
}