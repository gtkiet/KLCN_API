//using KLCN_API.Helpers;
//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;
//using Microsoft.Data.SqlClient;

//namespace KLCN_API.Services;

//public class BookingService : IBookingService
//{
//    private readonly IBookingRepository _bookingRepo;
//    private readonly IServiceRepository _serviceRepo;
//    private readonly StoredProcedureHelper _sp;

//    public BookingService(
//        IBookingRepository bookingRepo,
//        IServiceRepository serviceRepo,
//        StoredProcedureHelper sp)
//    {
//        _bookingRepo = bookingRepo;
//        _serviceRepo = serviceRepo;
//        _sp = sp;
//    }

//    // ── Hold Slots ───────────────────────────────────────────────

//    public async Task HoldSlotsAsync(HoldSlotsRequest request, int userId)
//    {
//        var ids = string.Join(",", request.FieldSlotIds);
//        await _sp.ExecuteAsync("sp_HoldSlots",
//            new SqlParameter("@FieldSlotIds", ids),
//            new SqlParameter("@UserId", userId));
//    }

//    // ── Create Booking ───────────────────────────────────────────

//    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int userId)
//    {
//        // Tạo booking header
//        var booking = new Booking
//        {
//            UserId = userId,
//            StatusId = 1,  // Chờ thanh toán
//            Note = request.Note,
//            CreatedAt = DateTime.UtcNow,
//            UpdatedAt = DateTime.UtcNow
//        };
//        var created = await _bookingRepo.CreateAsync(booking);

//        // Gắn dịch vụ nếu có
//        foreach (var item in request.Services)
//        {
//            var svc = await _serviceRepo.GetByIdAsync(item.ServiceId)
//                ?? throw new NotFoundException("Dịch vụ", item.ServiceId);

//            await _bookingRepo.AddBookingServiceAsync(new BookingService
//            {
//                BookingId = created.BookingId,
//                ServiceId = item.ServiceId,
//                Quantity = item.Quantity,
//                UnitPrice = svc.Price
//            });
//        }

//        // Gọi SP xác nhận + tính tiền
//        var ids = string.Join(",", request.FieldSlotIds);
//        await _sp.ExecuteAsync("sp_ConfirmBooking",
//            new SqlParameter("@BookingId", created.BookingId),
//            new SqlParameter("@FieldSlotIds", ids),
//            new SqlParameter("@IsFullPayment", request.IsFullPayment ? 1 : 0),
//            new SqlParameter("@UserId", userId));

//        // Áp voucher nếu có
//        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
//            await ApplyVoucherAsync(created.BookingId,
//                new ApplyVoucherRequest { Code = request.PromotionCode }, userId);

//        return await GetWithDetailsOrThrowAsync(created.BookingId);
//    }

//    // ── Get ──────────────────────────────────────────────────────

//    public async Task<BookingResponse> GetByIdAsync(int bookingId, int requesterId, bool isAdminOrStaff)
//    {
//        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        if (!isAdminOrStaff && booking.UserId != requesterId)
//            throw new ForbiddenException("Bạn không có quyền xem booking này.");

//        return MapDetail(booking);
//    }

//    public async Task<PagedResponse<BookingSummaryResponse>> GetBookingsAsync(GetBookingsRequest request)
//    {
//        var (items, total) = await _bookingRepo.GetBookingsAsync(
//            request.UserId, request.StatusId,
//            request.DateFrom, request.DateTo, request.FieldId,
//            request.Page, request.PageSize);

//        return new PagedResponse<BookingSummaryResponse>
//        {
//            Items = items.Select(MapSummary).ToList(),
//            TotalCount = total,
//            Page = request.Page,
//            PageSize = request.PageSize
//        };
//    }

//    public async Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(
//        int userId, int page, int pageSize)
//    {
//        var (items, total) = await _bookingRepo.GetBookingsAsync(
//            userId, statusId: null, dateFrom: null, dateTo: null,
//            fieldId: null, page, pageSize);

//        return new PagedResponse<BookingSummaryResponse>
//        {
//            Items = items.Select(MapSummary).ToList(),
//            TotalCount = total,
//            Page = page,
//            PageSize = pageSize
//        };
//    }

//    // ── Cancel ───────────────────────────────────────────────────

//    public async Task CancelAsync(int bookingId, CancelBookingRequest request,
//        int userId, bool isAdminOverride)
//    {
//        var booking = await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        if (!isAdminOverride && booking.UserId != userId)
//            throw new ForbiddenException("Bạn không có quyền hủy booking này.");

//        await _sp.ExecuteAsync("sp_CancelBooking",
//            new SqlParameter("@BookingId", bookingId),
//            new SqlParameter("@UserId", userId),
//            new SqlParameter("@Reason", (object?)request.Reason ?? DBNull.Value),
//            new SqlParameter("@IsAdminOverride", isAdminOverride ? 1 : 0));
//    }

//    // ── Reschedule ───────────────────────────────────────────────

//    public async Task RescheduleAsync(int bookingId, RescheduleRequest request, int userId)
//    {
//        var booking = await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        if (booking.UserId != userId)
//            throw new ForbiddenException("Bạn không có quyền đổi lịch booking này.");

//        await _sp.ExecuteAsync("sp_RescheduleBooking",
//            new SqlParameter("@BookingDetailId", request.BookingDetailId),
//            new SqlParameter("@NewFieldSlotId", request.NewFieldSlotId),
//            new SqlParameter("@UserId", userId));
//    }

//    // ── Apply Voucher ────────────────────────────────────────────

//    public async Task ApplyVoucherAsync(int bookingId, ApplyVoucherRequest request, int userId)
//    {
//        var booking = await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        if (booking.UserId != userId)
//            throw new ForbiddenException("Bạn không có quyền áp voucher cho booking này.");

//        await _sp.ExecuteAsync("sp_ApplyPromotion",
//            new SqlParameter("@BookingId", bookingId),
//            new SqlParameter("@Code", request.Code.Trim().ToUpper()),
//            new SqlParameter("@UserId", userId));
//    }

//    // ── Helpers ──────────────────────────────────────────────────

//    private async Task<BookingResponse> GetWithDetailsOrThrowAsync(int bookingId)
//    {
//        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);
//        return MapDetail(booking);
//    }

//    // ── Mappers ──────────────────────────────────────────────────

//    private static BookingResponse MapDetail(Booking b) => new()
//    {
//        BookingId = b.BookingId,
//        Customer = MapUser(b.User),
//        Status = b.Status?.Name ?? string.Empty,
//        StatusId = b.StatusId,
//        SubTotal = b.SubTotal,
//        DiscountAmount = b.DiscountAmount,
//        TaxAmount = b.TaxAmount,
//        TotalAmount = b.TotalAmount,
//        DepositAmount = b.DepositAmount,
//        PromotionCode = b.Promotion?.Code,
//        Note = b.Note,
//        CancelReason = b.CancelReason,
//        RescheduleCount = b.RescheduleCount,
//        CreatedAt = b.CreatedAt,
//        UpdatedAt = b.UpdatedAt,
//        Details = b.Details?.Select(d => new BookingDetailResponse
//        {
//            BookingDetailId = d.BookingDetailId,
//            FieldId = d.FieldSlot.FieldId,
//            FieldName = d.FieldSlot.Field?.Name ?? string.Empty,
//            FieldType = d.FieldSlot.Field?.Type?.Name ?? string.Empty,
//            SlotDate = d.FieldSlot.SlotDate,
//            StartTime = d.FieldSlot.Slot.StartTime,
//            EndTime = d.FieldSlot.Slot.EndTime,
//            Price = d.Price
//        }).ToList() ?? [],
//        Services = b.BookingServices?.Select(bs => new BookingServiceResponse
//        {
//            ServiceId = bs.ServiceId,
//            ServiceName = bs.Service?.Name ?? string.Empty,
//            Quantity = bs.Quantity,
//            UnitPrice = bs.UnitPrice
//        }).ToList() ?? [],
//        Deposit = b.Deposit is null ? null : new DepositResponse
//        {
//            DepositId = b.Deposit.DepositId,
//            BookingId = b.BookingId,
//            RequiredAmount = b.Deposit.RequiredAmount,
//            PaidAmount = b.Deposit.PaidAmount,
//            Status = b.Deposit.Status?.Name ?? string.Empty,
//            StatusId = b.Deposit.StatusId,
//            DeadlineAt = b.Deposit.DeadlineAt,
//            MinutesLeft = (int)(b.Deposit.DeadlineAt - DateTime.UtcNow).TotalMinutes,
//            PaidAt = b.Deposit.PaidAt
//        }
//    };

//    private static BookingSummaryResponse MapSummary(Booking b)
//    {
//        var earliest = b.Details?
//            .OrderBy(d => d.FieldSlot.SlotDate)
//            .ThenBy(d => d.FieldSlot.Slot.StartTime)
//            .FirstOrDefault();

//        return new BookingSummaryResponse
//        {
//            BookingId = b.BookingId,
//            CustomerName = b.User?.FullName ?? string.Empty,
//            CustomerPhone = b.User?.Phone ?? string.Empty,
//            Status = b.Status?.Name ?? string.Empty,
//            StatusId = b.StatusId,
//            TotalAmount = b.TotalAmount,
//            SlotCount = b.Details?.Count ?? 0,
//            EarliestSlotDate = earliest?.FieldSlot.SlotDate,
//            EarliestSlotTime = earliest?.FieldSlot.Slot.StartTime,
//            FieldName = earliest?.FieldSlot.Field?.Name,
//            CreatedAt = b.CreatedAt
//        };
//    }

//    private static UserResponse MapUser(User? u) => u is null ? new() : new()
//    {
//        UserId = u.UserId,
//        FullName = u.FullName,
//        Email = u.Email,
//        Phone = u.Phone,
//        Role = u.Role?.Name ?? string.Empty,
//        RoleId = u.RoleId,
//        Status = u.Status?.Name ?? string.Empty,
//        StatusId = u.StatusId,
//        AvatarUrl = u.Profile?.AvatarUrl,
//        CreatedAt = u.CreatedAt
//    };
//}