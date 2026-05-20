using KLCN_API.Data;
using KLCN_API.Helpers;
using KLCN_API.Mappers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IDepositRepository _depositRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly SportPlusDbContext _ctx;

    public PaymentService(
        IPaymentRepository paymentRepo,
        IDepositRepository depositRepo,
        IBookingRepository bookingRepo,
        SportPlusDbContext ctx)
    {
        _paymentRepo = paymentRepo;
        _depositRepo = depositRepo;
        _bookingRepo = bookingRepo;
        _ctx = ctx;
    }

    // ── Deposit (MoMo / VNPay — online, gọi từ IPN) ──────────────

    public async Task RecordDepositAsync(
        int bookingId, decimal amount, int methodId, string transactionCode)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.StatusId != (int)BookingStatusEnum.PendingDeposit)
            throw new BusinessException(
                "Booking không ở trạng thái chờ đặt cọc.", 400);

        await StoredProcedureHelper.RecordDepositAsync(
            _ctx,
            bookingId: bookingId,
            amount: amount,
            methodId: methodId,
            transactionCode: transactionCode,
            userId: null);
    }

    // ── Full payment (Staff tại quầy) ─────────────────────────────

    /// <summary>
    /// Ghi nhận thanh toán phần còn lại — Staff/Admin tại quầy.
    /// Cho phép 2 trạng thái:
    ///   Confirmed (2)     : đã cọc, cần trả phần còn lại
    ///   PendingPayment (1): full payment, chưa có giao dịch nào
    /// [FIX Bug 5] Trước đây chỉ check StatusId=2, bỏ sót luồng IsFullPayment=true (StatusId=1)
    /// </summary>
    public async Task RecordFullPaymentAsync(
    int bookingId, ConfirmPaymentRequest request, int userId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.StatusId != (int)BookingStatusEnum.Confirmed
            && booking.StatusId != (int)BookingStatusEnum.PendingPayment)
        {
            throw new BusinessException(
                "Booking phải ở trạng thái Đã xác nhận hoặc Chờ thanh toán mới có thể ghi nhận.", 400);
        }

        // AUTO-GENERATE transactionCode nếu staff để trống.
        // Thanh toán tiền mặt không có mã giao dịch thực — sinh mã nội bộ
        // để đảm bảo idempotency và truy vết sau này.
        var txCode = string.IsNullOrWhiteSpace(request.TransactionCode)
            ? $"DIRECT-{bookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : request.TransactionCode.Trim();

        await StoredProcedureHelper.RecordFullPaymentAsync(
            _ctx,
            bookingId: bookingId,
            methodId: request.MethodId,
            transactionCode: txCode,
            userId: userId);
    }

    // ── Online payment router (gọi từ IPN / Return fallback) ─────

    /// <summary>
    /// Router dùng cho IPN callback và Return fallback.
    /// Tự phân loại cọc hay thanh toán còn lại dựa vào StatusId.
    /// Idempotent theo transactionCode.
    ///
    /// PendingDeposit (5) → sp_RecordDeposit  → Confirmed (2)
    /// PendingPayment (1) → sp_RecordFullPayment → Completed (nếu slot đã qua)
    /// Confirmed      (2) → sp_RecordFullPayment → Completed (nếu slot đã qua)
    /// </summary>
    public async Task RecordOnlinePaymentAsync(
        int bookingId, decimal amount, int methodId, string transactionCode)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        // Idempotent: bỏ qua nếu transactionCode đã được ghi nhận
        if (!string.IsNullOrWhiteSpace(transactionCode)
            && await _paymentRepo.ExistsByTransactionCodeAsync(transactionCode))
            return;

        if (booking.StatusId == (int)BookingStatusEnum.PendingDeposit)
        {
            await StoredProcedureHelper.RecordDepositAsync(
                _ctx, bookingId, amount, methodId, transactionCode, userId: null);
        }
        else if (booking.StatusId == (int)BookingStatusEnum.PendingPayment
              || booking.StatusId == (int)BookingStatusEnum.Confirmed)
        {
            // sp_RecordFullPayment chỉ chấp nhận StatusId=2.
            // PendingPayment(1) xảy ra khi customer chọn IsFullPayment=true và SP
            // sp_ConfirmBooking vẫn set StatusId=2 (xem SQL: @IsFullPayment=1 → StatusId=2).
            // Vậy trên thực tế cả hai nhánh đều call cùng một SP.
            await StoredProcedureHelper.RecordFullPaymentAsync(
                _ctx, bookingId, methodId, transactionCode, userId: null);
        }
        // Các StatusId khác (Cancelled, Completed) → bỏ qua
    }

    // ── Read ──────────────────────────────────────────────────────

    public async Task<List<PaymentResponse>> GetPaymentsByBookingAsync(int bookingId)
    {
        var payments = await _paymentRepo.GetByBookingAsync(bookingId);

        return payments.Select(p => new PaymentResponse
        {
            PaymentId = p.PaymentId,
            BookingId = p.BookingId,
            Amount = p.Amount,
            Status = p.Status?.Name ?? string.Empty,
            StatusId = p.StatusId,
            PaymentMethod = p.Method?.Name ?? string.Empty,
            MethodId = p.MethodId,
            TransactionCode = p.TransactionCode,
            Note = p.Note,
            PaidAt = p.PaidAt,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<DepositResponse?> GetDepositByBookingAsync(int bookingId)
    {
        var deposit = await _depositRepo.GetByBookingAsync(bookingId);
        if (deposit is null) return null;

        return new DepositResponse
        {
            DepositId = deposit.DepositId,
            BookingId = deposit.BookingId,
            RequiredAmount = deposit.RequiredAmount,
            PaidAmount = deposit.PaidAmount,
            Status = deposit.Status?.Name ?? string.Empty,
            StatusId = deposit.StatusId,
            DeadlineAt = deposit.DeadlineAt,
            MinutesLeft = Math.Max(0,
                (int)(deposit.DeadlineAt - DateTime.UtcNow).TotalMinutes),
            PaidAt = deposit.PaidAt
        };
    }

    /// <summary>
    /// Lấy booking và tính toán số tiền cần thanh toán tiếp theo.
    /// Trả về trong AmountDue của BookingResponse (xem PaymentsController).
    /// Cho phép các trạng thái: PendingDeposit(5), PendingPayment(1), Confirmed(2).
    /// </summary>
    public async Task<BookingResponse> GetBookingForPaymentAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.StatusId != (int)BookingStatusEnum.Confirmed
            && booking.StatusId != (int)BookingStatusEnum.PendingDeposit
            && booking.StatusId != (int)BookingStatusEnum.PendingPayment)
        {
            throw new BusinessException(
                "Booking không ở trạng thái có thể thanh toán.", 400);
        }

        return BookingMapper.MapDetail(booking);
    }

    /// <summary>
    /// Tính số tiền cần thanh toán cho lần tiếp theo.
    /// - PendingDeposit(5): trả DepositAmount
    /// - PendingPayment(1): trả full TotalAmount (chưa có giao dịch nào)
    /// - Confirmed(2): trả phần còn lại = TotalAmount - TổngĐãTrả
    /// [FIX Bug 6 & 7] Trước đây luôn dùng TotalAmount, gây overcharge lần 2
    /// </summary>
    public async Task<decimal> GetAmountDueAsync(int bookingId, BookingResponse booking)
    {
        if (booking.StatusId == (int)BookingStatusEnum.PendingDeposit)
            return booking.DepositAmount;

        var totalAmount = booking.TotalAmount ?? 0;

        // Lấy tổng đã thanh toán từ bảng Payments
        var alreadyPaid = await _paymentRepo.GetTotalPaidAsync(bookingId);
        var remaining = totalAmount - alreadyPaid;

        if (remaining <= 0)
            throw new BusinessException(
                "Booking đã được thanh toán đủ.", 400);

        return remaining;
    }
}