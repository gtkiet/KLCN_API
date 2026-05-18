using KLCN_API.Data;
using KLCN_API.Helpers;
using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;
using BookingEntity = KLCN_API.Models.Entities.Booking;
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

    // ── Create booking (customer flow) ───────────────────────────

    /// <summary>
    /// Customer tự tạo booking.
    /// Luôn đi theo flow cũ: chờ đặt cọc / pending payment.
    /// </summary>
    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int userId)
    {
        return await CreateBookingInternalAsync(
            customerId: userId,
            request: request,
            actorUserId: userId,
            isFullPayment: false,
            paymentMethodId: null,
            transactionCode: null);
    }

    // ── Create booking at counter (walk-in) ──────────────────────

    /// <summary>
    /// Admin/Staff đặt sân hộ khách tại quầy.
    /// - IsFullPayment = false: tạo booking theo flow chờ cọc như cũ
    /// - IsFullPayment = true: trả đủ tiền ngay tại quầy
    /// </summary>
    public async Task<BookingResponse> CreateWalkInBookingAsync(CreateWalkInBookingRequest request, int actorUserId)
    {
        var customer = await _ctx.Users.FindAsync(request.CustomerId)
            ?? throw new NotFoundException("Khách hàng", request.CustomerId);

        if (customer.RoleId != (int)RoleEnum.Customer)
            throw new BusinessException("Người được chọn không phải khách hàng.", 400);

        if (request.FieldSlotIds == null || !request.FieldSlotIds.Any())
            throw new BusinessException("Phải chọn ít nhất 1 slot.", 400);

        if (request.IsFullPayment && !request.PaymentMethodId.HasValue)
            throw new BusinessException("Thanh toán đủ tại quầy phải có phương thức thanh toán.", 400);

        var createRequest = new CreateBookingRequest
        {
            FieldSlotIds = request.FieldSlotIds,
            PromotionCode = request.PromotionCode,
            Note = request.Note,
            Services = request.Services ?? new List<BookingServiceItem>()
        };

        return await CreateBookingInternalAsync(
            customerId: request.CustomerId,
            request: createRequest,
            actorUserId: actorUserId,
            isFullPayment: request.IsFullPayment,
            paymentMethodId: request.PaymentMethodId,
            transactionCode: request.TransactionCode);
    }

    // ── Shared create logic ───────────────────────────────────────

    private async Task<BookingResponse> CreateBookingInternalAsync(
        int customerId,
        CreateBookingRequest request,
        int actorUserId,
        bool isFullPayment,
        int? paymentMethodId,
        string? transactionCode)
    {
        if (request.FieldSlotIds == null || !request.FieldSlotIds.Any())
            throw new BusinessException("Phải chọn ít nhất 1 slot.", 400);

        // Bọc transaction để rollback toàn bộ nếu SP throw
        // (ví dụ: slot bị đặt bởi người khác, duplicate key...)
        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            // 1) Tạo booking header trước để lấy BookingId
            var booking = new BookingEntity
            {
                UserId = customerId,
                // Customer online: IsFullPayment=true → bỏ qua cọc, cổng thanh toán xử lý sau
                // Walk-in      : IsFullPayment=true → trả đủ ngay tại quầy
                StatusId = isFullPayment
                    ? (int)BookingStatusEnum.Confirmed
                    : (int)BookingStatusEnum.PendingPayment,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _bookingRepo.CreateAsync(booking);

            // 2) Gắn dịch vụ đi kèm
            if (request.Services != null)
            {
                foreach (var item in request.Services)
                {
                    var svc = await _serviceRepo.GetByIdAsync(item.ServiceId)
                        ?? throw new NotFoundException("Dịch vụ", item.ServiceId);

                    if (!svc.IsAvailable)
                        throw new BusinessException($"Dịch vụ '{svc.Name}' hiện không khả dụng.", 400);

                    await _bookingRepo.AddBookingServiceAsync(new BookingServiceEntity
                    {
                        BookingId = created.BookingId,
                        ServiceId = item.ServiceId,
                        Quantity = item.Quantity,
                        UnitPrice = svc.Price
                    });
                }
            }

            // 3) Confirm booking bằng stored procedure
            //    - false: flow cũ chờ cọc
            //    - true : xác nhận và chuẩn bị thanh toán đủ
            var slotIds = string.Join(",", request.FieldSlotIds);

            await StoredProcedureHelper.ConfirmBookingAsync(
                _ctx,
                bookingId: created.BookingId,
                fieldSlotIds: slotIds,
                isFullPayment: isFullPayment,
                userId: actorUserId);

            // 4) Áp khuyến mãi nếu có
            if (!string.IsNullOrWhiteSpace(request.PromotionCode))
            {
                await StoredProcedureHelper.ApplyPromotionAsync(
                    _ctx,
                    bookingId: created.BookingId,
                    code: request.PromotionCode.Trim().ToUpper(),
                    userId: actorUserId);
            }

            // 5) Walk-in + trả đủ tại quầy → ghi nhận payment ngay, không qua cổng
            //    Customer online + IsFullPayment=true → KHÔNG ghi ở đây,
            //    họ sẽ thanh toán qua VNPay sau khi tạo booking xong.
            if (isFullPayment && paymentMethodId.HasValue)
            {
                var txCode = string.IsNullOrWhiteSpace(transactionCode)
                    ? $"WALKIN-{created.BookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                    : transactionCode.Trim();

                await StoredProcedureHelper.RecordFullPaymentAsync(
                    _ctx,
                    bookingId: created.BookingId,
                    methodId: paymentMethodId.Value,
                    transactionCode: txCode,
                    userId: actorUserId);
            }

            await tx.CommitAsync();
            return await GetWithDetailsOrThrowAsync(created.BookingId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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

    public async Task<PagedResponse<BookingSummaryResponse>> GetBookingsAsync(GetBookingsRequest request)
    {
        var (items, total) = await _bookingRepo.GetBookingsAsync(
            request.UserId,
            request.StatusId,
            request.DateFrom,
            request.DateTo,
            request.FieldId,
            request.Page,
            request.PageSize);

        return new PagedResponse<BookingSummaryResponse>
        {
            Items = items.Select(MapSummary).ToList(),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

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
        int bookingId,
        CancelBookingRequest request,
        int userId,
        bool isAdminOverride)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (!isAdminOverride && booking.UserId != userId)
            throw new ForbiddenException("Bạn không có quyền hủy booking này.");

        await StoredProcedureHelper.CancelBookingAsync(
            _ctx,
            bookingId,
            userId,
            request.Reason,
            isAdminOverride);
    }

    // ── Reschedule ────────────────────────────────────────────────

    public async Task RescheduleAsync(int bookingId, RescheduleRequest request, int userId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.UserId != userId)
            throw new ForbiddenException("Bạn không có quyền đổi lịch booking này.");

        await StoredProcedureHelper.RescheduleBookingAsync(
            _ctx,
            request.BookingDetailId,
            request.NewFieldSlotId,
            userId);
    }

    // ── Apply voucher ─────────────────────────────────────────────

    public async Task ApplyVoucherAsync(int bookingId, ApplyVoucherRequest request, int userId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.UserId != userId)
            throw new ForbiddenException("Bạn không có quyền áp voucher cho booking này.");

        if (booking.StatusId != (int)BookingStatusEnum.PendingPayment
            && booking.StatusId != (int)BookingStatusEnum.PendingDeposit)
        {
            throw new BusinessException(
                "Chỉ có thể áp voucher khi booking đang chờ thanh toán hoặc chờ đặt cọc.",
                400);
        }

        await StoredProcedureHelper.ApplyPromotionAsync(
            _ctx,
            bookingId,
            request.Code.Trim().ToUpper(),
            userId);
    }

    // ── Private helpers ───────────────────────────────────────────

    private async Task<BookingResponse> GetWithDetailsOrThrowAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        return MapDetail(booking);
    }

    // ── Mappers ───────────────────────────────────────────────────

    private static BookingResponse MapDetail(BookingEntity b) => new()
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
        }).ToList() ?? new List<BookingDetailResponse>(),
        Services = b.BookingServices?.Select(bs => new BookingServiceResponse
        {
            ServiceId = bs.ServiceId,
            ServiceName = bs.Service?.Name ?? string.Empty,
            Quantity = bs.Quantity,
            UnitPrice = bs.UnitPrice
        }).ToList() ?? new List<BookingServiceResponse>(),
        Deposit = b.Deposit is null ? null : new DepositResponse
        {
            DepositId = b.Deposit.DepositId,
            BookingId = b.BookingId,
            RequiredAmount = b.Deposit.RequiredAmount,
            PaidAmount = b.Deposit.PaidAmount,
            Status = b.Deposit.Status?.Name ?? string.Empty,
            StatusId = b.Deposit.StatusId,
            DeadlineAt = b.Deposit.DeadlineAt,
            MinutesLeft = Math.Max(0, (int)(b.Deposit.DeadlineAt - DateTime.UtcNow).TotalMinutes),
            PaidAt = b.Deposit.PaidAt
        }
    };

    private static BookingSummaryResponse MapSummary(BookingEntity b)
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