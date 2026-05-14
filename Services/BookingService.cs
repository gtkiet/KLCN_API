using KLCN_API.Data;
using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

using BookingServiceEntity = KLCN_API.Models.Entities.BookingService;

namespace KLCN_API.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly SportPlusDbContext _ctx;

    public BookingService(
        IBookingRepository bookingRepo,
        IServiceRepository serviceRepo,
        SportPlusDbContext ctx)
    {
        _bookingRepo = bookingRepo;
        _serviceRepo = serviceRepo;
        _ctx = ctx;
    }

    // ── Hold slots ────────────────────────────────────────────────

    public async Task HoldSlotsAsync(HoldSlotsRequest request, int userId)
        => await StoredProcedureHelper.HoldSlotsAsync(_ctx, request.FieldSlotIds, userId);

    // ── Create booking ────────────────────────────────────────────

    /// <summary>
    /// Tạo booking: gắn dịch vụ → sp_ConfirmBooking (IsFullPayment=0, luôn tạo Deposit)
    /// → sp_ApplyPromotion nếu có mã → trả về BookingResponse với StatusId=5 (Chờ đặt cọc).
    /// Customer tiếp tục gọi POST /api/payments/momo/create/{bookingId} để lấy URL cọc.
    /// </summary>
    public async Task<BookingResponse> CreateBookingAsync(
        CreateBookingRequest request, int userId)
    {
        // Tạo booking header trước để có BookingId
        var booking = new Models.Entities.Booking
        {
            UserId = userId,
            StatusId = (int)BookingStatusEnum.PendingPayment,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var created = await _bookingRepo.CreateAsync(booking);

        // Gắn dịch vụ đi kèm — validate từng service tồn tại và khả dụng
        foreach (var item in request.Services)
        {
            var svc = await _serviceRepo.GetByIdAsync(item.ServiceId)
                ?? throw new NotFoundException("Dịch vụ", item.ServiceId);

            if (!svc.IsAvailable)
                throw new BusinessException(
                    $"Dịch vụ '{svc.Name}' hiện không khả dụng.", 400);

            await _bookingRepo.AddBookingServiceAsync(new BookingServiceEntity
            {
                BookingId = created.BookingId,
                ServiceId = item.ServiceId,
                Quantity = item.Quantity,
                UnitPrice = svc.Price
            });
        }

        // sp_ConfirmBooking: slot Đang giữ → Đã đặt, tính tiền, tạo Deposit.
        // IsFullPayment=false vì bắt buộc phải đặt cọc trước khi xác nhận.
        var slotIds = string.Join(",", request.FieldSlotIds);
        await StoredProcedureHelper.ConfirmBookingAsync(
            _ctx,
            bookingId: created.BookingId,
            fieldSlotIds: slotIds,
            isFullPayment: false,   // luôn tạo Deposit, StatusId → 5
            userId: userId);

        // sp_ApplyPromotion phải chạy SAU ConfirmBooking vì cần SubTotal đã tính
        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
            await StoredProcedureHelper.ApplyPromotionAsync(
                _ctx,
                bookingId: created.BookingId,
                code: request.PromotionCode.Trim().ToUpper(),
                userId: userId);

        return await GetWithDetailsOrThrowAsync(created.BookingId);
    }

    // ── Get ───────────────────────────────────────────────────────

    public async Task<BookingResponse> GetByIdAsync(
        int bookingId, int requesterId, bool isAdminOrStaff)
    {
        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (!isAdminOrStaff && booking.UserId != requesterId)
            throw new ForbiddenException("Bạn không có quyền xem booking này.");

        return MapDetail(booking);
    }

    public async Task<PagedResponse<BookingSummaryResponse>> GetBookingsAsync(
        GetBookingsRequest request)
    {
        var (items, total) = await _bookingRepo.GetBookingsAsync(
            request.UserId, request.StatusId,
            request.DateFrom, request.DateTo, request.FieldId,
            request.Page, request.PageSize);

        return new PagedResponse<BookingSummaryResponse>
        {
            Items = items.Select(MapSummary).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Lấy danh sách booking của chính user.
    /// statusId=null → lấy tất cả; có giá trị → lọc theo trạng thái.
    /// </summary>
    public async Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(
        int userId, int? statusId, int page, int pageSize)
    {
        var (items, total) = await _bookingRepo.GetBookingsAsync(
            userId,
            statusId,
            dateFrom: null,
            dateTo: null,
            fieldId: null,
            page,
            pageSize);

        return new PagedResponse<BookingSummaryResponse>
        {
            Items = items.Select(MapSummary).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    // ── Cancel ────────────────────────────────────────────────────

    public async Task CancelAsync(
        int bookingId, CancelBookingRequest request,
        int userId, bool isAdminOverride)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (!isAdminOverride && booking.UserId != userId)
            throw new ForbiddenException("Bạn không có quyền hủy booking này.");

        await StoredProcedureHelper.CancelBookingAsync(
            _ctx, bookingId, userId, request.Reason, isAdminOverride);
    }

    // ── Reschedule ────────────────────────────────────────────────

    public async Task RescheduleAsync(
        int bookingId, RescheduleRequest request, int userId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.UserId != userId)
            throw new ForbiddenException("Bạn không có quyền đổi lịch booking này.");

        await StoredProcedureHelper.RescheduleBookingAsync(
            _ctx, request.BookingDetailId, request.NewFieldSlotId, userId);
    }

    // ── Apply voucher ─────────────────────────────────────────────

    public async Task ApplyVoucherAsync(
        int bookingId, ApplyVoucherRequest request, int userId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.UserId != userId)
            throw new ForbiddenException("Bạn không có quyền áp voucher cho booking này.");

        // sp_ApplyPromotion chỉ cho phép áp khi StatusId IN (1, 5)
        if (booking.StatusId != (int)BookingStatusEnum.PendingPayment
            && booking.StatusId != (int)BookingStatusEnum.PendingDeposit)
            throw new BusinessException(
                "Chỉ có thể áp voucher khi booking đang chờ thanh toán hoặc chờ đặt cọc.", 400);

        await StoredProcedureHelper.ApplyPromotionAsync(
            _ctx, bookingId, request.Code.Trim().ToUpper(), userId);
    }

    // ── Private helpers ───────────────────────────────────────────

    private async Task<BookingResponse> GetWithDetailsOrThrowAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);
        return MapDetail(booking);
    }

    // ── Mappers ───────────────────────────────────────────────────

    private static BookingResponse MapDetail(Models.Entities.Booking b) => new()
    {
        BookingId = b.BookingId,
        Customer = UserMapper.ToResponse(b.User),
        Status = b.Status?.Name ?? string.Empty,
        StatusId = b.StatusId,
        SubTotal = b.SubTotal,
        DiscountAmount = b.DiscountAmount,
        TaxAmount = b.TaxAmount,
        TotalAmount = b.TotalAmount,
        DepositAmount = b.DepositAmount,
        PromotionCode = b.Promotion?.Code,
        Note = b.Note,
        CancelReason = b.CancelReason,
        RescheduleCount = b.RescheduleCount,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt,
        Details = b.BookingDetails?.Select(d => new BookingDetailResponse
        {
            BookingDetailId = d.BookingDetailId,
            FieldId = d.FieldSlot.FieldId,
            FieldName = d.FieldSlot.Field?.Name ?? string.Empty,
            FieldType = d.FieldSlot.Field?.Type?.Name ?? string.Empty,
            SlotDate = d.FieldSlot.SlotDate,
            StartTime = d.FieldSlot.TimeSlot.StartTime,
            EndTime = d.FieldSlot.TimeSlot.EndTime,
            Price = d.Price
        }).ToList() ?? [],
        Services = b.BookingServices?.Select(bs => new BookingServiceResponse
        {
            ServiceId = bs.ServiceId,
            ServiceName = bs.Service?.Name ?? string.Empty,
            Quantity = bs.Quantity,
            UnitPrice = bs.UnitPrice
        }).ToList() ?? [],
        Deposit = b.Deposit is null ? null : new DepositResponse
        {
            DepositId = b.Deposit.DepositId,
            BookingId = b.BookingId,
            RequiredAmount = b.Deposit.RequiredAmount,
            PaidAmount = b.Deposit.PaidAmount,
            Status = b.Deposit.Status?.Name ?? string.Empty,
            StatusId = b.Deposit.StatusId,
            DeadlineAt = b.Deposit.DeadlineAt,
            MinutesLeft = Math.Max(0,
                (int)(b.Deposit.DeadlineAt - DateTime.UtcNow).TotalMinutes),
            PaidAt = b.Deposit.PaidAt
        }
    };

    private static BookingSummaryResponse MapSummary(Models.Entities.Booking b)
    {
        var earliest = b.BookingDetails?
            .OrderBy(d => d.FieldSlot.SlotDate)
            .ThenBy(d => d.FieldSlot.TimeSlot.StartTime)
            .FirstOrDefault();

        return new BookingSummaryResponse
        {
            BookingId = b.BookingId,
            CustomerName = b.User?.FullName ?? string.Empty,
            CustomerPhone = b.User?.Phone ?? string.Empty,
            Status = b.Status?.Name ?? string.Empty,
            StatusId = b.StatusId,
            TotalAmount = b.TotalAmount,
            SlotCount = b.BookingDetails?.Count ?? 0,
            EarliestSlotDate = earliest?.FieldSlot.SlotDate,
            EarliestSlotTime = earliest?.FieldSlot.TimeSlot.StartTime,
            FieldName = earliest?.FieldSlot.Field?.Name,
            CreatedAt = b.CreatedAt
        };
    }
}