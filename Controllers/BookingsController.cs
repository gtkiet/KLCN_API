//using KLCN_API.Filters;
//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/bookings")]
//[Authorize]
//public class BookingsController : ControllerBase
//{
//    private readonly IBookingService _bookingService;
//    private readonly IPaymentService _paymentService;

//    public BookingsController(IBookingService bookingService, IPaymentService paymentService)
//    {
//        _bookingService = bookingService;
//        _paymentService = paymentService;
//    }

//    /// <summary>Giữ slot tạm — Customer.</summary>
//    [HttpPost("hold")]
//    [AuthorizeRoles(RoleEnum.Customer)]
//    public async Task<IActionResult> HoldSlots([FromBody] HoldSlotsRequest request)
//    {
//        var userId = User.GetUserId();
//        await _bookingService.HoldSlotsAsync(request, userId);
//        return Ok(ApiResponse.Ok("Giữ slot thành công. Vui lòng hoàn tất đặt sân trong thời gian quy định."));
//    }

//    /// <summary>Tạo booking — Customer.</summary>
//    [HttpPost]
//    [AuthorizeRoles(RoleEnum.Customer)]
//    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
//    {
//        var userId = User.GetUserId();
//        var result = await _bookingService.CreateBookingAsync(request, userId);
//        return Ok(ApiResponse<BookingResponse>.Ok(result, "Đặt sân thành công."));
//    }

//    /// <summary>Lấy danh sách booking — Admin và Staff.</summary>
//    [HttpGet]
//    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//    public async Task<IActionResult> GetBookings([FromQuery] GetBookingsRequest request)
//    {
//        var result = await _bookingService.GetBookingsAsync(request);
//        return Ok(ApiResponse<PagedResponse<BookingResponse>>.Ok(result));
//    }

//    /// <summary>Lấy booking của bản thân — Customer.</summary>
//    [HttpGet("my")]
//    [AuthorizeRoles(RoleEnum.Customer)]
//    public async Task<IActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//    {
//        var userId = User.GetUserId();
//        var result = await _bookingService.GetMyBookingsAsync(userId, page, pageSize);
//        return Ok(ApiResponse<PagedResponse<BookingResponse>>.Ok(result));
//    }

//    /// <summary>Lấy chi tiết 1 booking.</summary>
//    [HttpGet("{bookingId:int}")]
//    public async Task<IActionResult> GetById(int bookingId)
//    {
//        var userId = User.GetUserId();
//        var role = User.GetRole();
//        var result = await _bookingService.GetBookingByIdAsync(bookingId, userId, role);
//        return Ok(ApiResponse<BookingResponse>.Ok(result));
//    }

//    /// <summary>Hủy booking.</summary>
//    [HttpPost("{bookingId:int}/cancel")]
//    public async Task<IActionResult> Cancel(int bookingId, [FromBody] CancelBookingRequest request)
//    {
//        var userId = User.GetUserId();
//        var isAdmin = User.IsAdminOrStaff();
//        await _bookingService.CancelBookingAsync(bookingId, request, userId, isAdmin);
//        return Ok(ApiResponse.Ok("Hủy booking thành công."));
//    }

//    /// <summary>Đổi lịch 1 slot trong booking.</summary>
//    [HttpPost("{bookingId:int}/reschedule")]
//    public async Task<IActionResult> Reschedule(int bookingId, [FromBody] RescheduleRequest request)
//    {
//        var userId = User.GetUserId();
//        await _bookingService.RescheduleAsync(bookingId, request, userId);
//        return Ok(ApiResponse.Ok("Đổi lịch thành công."));
//    }

//    /// <summary>Áp dụng voucher vào booking.</summary>
//    [HttpPost("{bookingId:int}/apply-voucher")]
//    public async Task<IActionResult> ApplyVoucher(int bookingId, [FromBody] ApplyVoucherRequest request)
//    {
//        var userId = User.GetUserId();
//        await _bookingService.ApplyVoucherAsync(bookingId, request, userId);
//        return Ok(ApiResponse.Ok("Áp dụng voucher thành công."));
//    }

//    /// <summary>Ghi nhận đặt cọc — Customer hoặc Staff.</summary>
//    [HttpPost("{bookingId:int}/deposit")]
//    [AuthorizeRoles(RoleEnum.Customer, RoleEnum.Staff, RoleEnum.Admin)]
//    public async Task<IActionResult> RecordDeposit(int bookingId, [FromBody] RecordDepositRequest request)
//    {
//        var userId = User.GetUserId();
//        await _paymentService.RecordDepositAsync(bookingId, request, userId);
//        return Ok(ApiResponse.Ok("Ghi nhận đặt cọc thành công."));
//    }

//    /// <summary>Thanh toán phần còn lại — Staff hoặc Admin.</summary>
//    [HttpPost("{bookingId:int}/payment")]
//    [AuthorizeRoles(RoleEnum.Staff, RoleEnum.Admin)]
//    public async Task<IActionResult> RecordFullPayment(int bookingId, [FromBody] RecordFullPaymentRequest request)
//    {
//        var userId = User.GetUserId();
//        await _paymentService.RecordFullPaymentAsync(bookingId, request, userId);
//        return Ok(ApiResponse.Ok("Thanh toán thành công."));
//    }
//}