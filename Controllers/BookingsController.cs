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

    public BookingsController(
        IBookingService bookingService,
        IPaymentService paymentService)
    {
        _bookingService = bookingService;
        _paymentService = paymentService;
    }

    // ── Hold & Create ─────────────────────────────────────────────

    /// <summary>
    /// Giữ slot tạm — Customer.
    /// Gọi trước CreateBooking để đảm bảo slot còn trống.
    /// </summary>
    [HttpPost("hold")]
    [AuthorizeRoles(RoleEnum.Customer)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> HoldSlots([FromBody] HoldSlotsRequest request)
    {
        await _bookingService.HoldSlotsAsync(request, User.GetUserId());
        return Ok(ApiResponse.Ok(
            "Giữ slot thành công. Vui lòng hoàn tất đặt sân trong thời gian quy định."));
    }

    /// <summary>
    /// Tạo booking từ các slot đang giữ — Customer.
    /// Sau khi thành công (StatusId=5), gọi tiếp POST /api/payments/momo/create/{bookingId}
    /// hoặc /api/payments/vnpay/create/{bookingId} để lấy URL thanh toán cọc.
    /// </summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Customer)]
    [ProducesResponseType(typeof(ApiResponse<BookingResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        var result = await _bookingService.CreateBookingAsync(request, User.GetUserId());

        return Ok(ApiResponse<BookingResponse>.Ok(
            result,
            "Đặt sân thành công. Vui lòng thanh toán cọc để xác nhận."));
    }

    /// <summary>
    /// Đặt sân tại quầy — Admin/Staff đặt hộ khách.
    /// - IsFullPayment = false: tạo booking theo flow chờ cọc như cũ
    /// - IsFullPayment = true : khách thanh toán đủ ngay tại quầy
    /// </summary>
    [HttpPost("admin/walk-in")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<BookingResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> CreateAdminWalkIn([FromBody] CreateAdminWalkInBookingRequest request)
    {
        var result = await _bookingService.CreateAdminWalkInBookingAsync(request, User.GetUserId());

        return Ok(ApiResponse<BookingResponse>.Ok(
            result,
            request.PaymentOption == WalkInPaymentOption.PaidInFull
                ? "Đặt sân tại quầy và thanh toán thành công."
                : "Đặt sân tại quầy thành công."));
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
        [FromQuery] int? statusId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _bookingService.GetMyBookingsAsync(
            User.GetUserId(), statusId, page, pageSize);

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
        var result = await _bookingService.GetByIdAsync(
            bookingId,
            User.GetUserId(),
            User.IsAdminOrStaff());

        return Ok(ApiResponse<BookingResponse>.Ok(result));
    }

    // ── Actions ───────────────────────────────────────────────────

    /// <summary>
    /// Hủy booking.
    /// Customer chỉ hủy được booking của mình.
    /// Admin/Staff có thể hủy bất kỳ booking nào (SP bỏ qua ràng buộc giờ).
    /// </summary>
    [HttpPost("{bookingId:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Cancel(
        int bookingId,
        [FromBody] CancelBookingRequest request)
    {
        await _bookingService.CancelAsync(
            bookingId,
            request,
            User.GetUserId(),
            User.IsAdminOrStaff());

        return Ok(ApiResponse.Ok("Hủy booking thành công."));
    }

    /// <summary>Đổi lịch 1 slot trong booking — Customer (chỉ booking của mình).</summary>
    [HttpPost("{bookingId:int}/reschedule")]
    [AuthorizeRoles(RoleEnum.Customer)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Reschedule(
        int bookingId,
        [FromBody] RescheduleRequest request)
    {
        await _bookingService.RescheduleAsync(bookingId, request, User.GetUserId());
        return Ok(ApiResponse.Ok("Đổi lịch thành công."));
    }

    /// <summary>Áp dụng mã voucher vào booking — Customer (chỉ booking của mình).</summary>
    [HttpPost("{bookingId:int}/apply-voucher")]
    [AuthorizeRoles(RoleEnum.Customer)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ApplyVoucher(
        int bookingId,
        [FromBody] ApplyVoucherRequest request)
    {
        await _bookingService.ApplyVoucherAsync(bookingId, request, User.GetUserId());
        return Ok(ApiResponse.Ok("Áp dụng voucher thành công."));
    }

    // ── Payments ──────────────────────────────────────────────────

    /// <summary>
    /// Thanh toán phần còn lại sau khi đã cọc — Staff hoặc Admin.
    /// MethodId: 1=Trực tiếp, 2=MoMo, 3=VNPay.
    /// </summary>
    [HttpPost("{bookingId:int}/payment")]
    [AuthorizeRoles(RoleEnum.Staff, RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> RecordFullPayment(
        int bookingId,
        [FromBody] ConfirmPaymentRequest request)
    {
        await _paymentService.RecordFullPaymentAsync(
            bookingId,
            request,
            User.GetUserId());

        return Ok(ApiResponse.Ok("Thanh toán thành công."));
    }

    /// <summary>Lấy lịch sử thanh toán của booking.</summary>
    [HttpGet("{bookingId:int}/payments")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetPayments(int bookingId)
    {
        // Kiểm tra quyền truy cập trước
        await _bookingService.GetByIdAsync(
            bookingId,
            User.GetUserId(),
            User.IsAdminOrStaff());

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
        await _bookingService.GetByIdAsync(
            bookingId,
            User.GetUserId(),
            User.IsAdminOrStaff());

        var result = await _paymentService.GetDepositByBookingAsync(bookingId);
        return Ok(ApiResponse<DepositResponse?>.Ok(result));
    }
}