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

    /// <summary>
    /// Ghi nhận đặt cọc online từ cổng thanh toán.
    /// Gọi từ IPN callback (MoMo/VNPay) — không cần userId vì là server-to-server.
    /// SP sp_RecordDeposit cập nhật Deposit.StatusId=2, Booking.StatusId=2.
    /// </summary>
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
            userId: null); // IPN — không có user context
    }

    // ── Full payment (Staff tại quầy hoặc chuyển khoản) ──────────

    /// <summary>
    /// Ghi nhận thanh toán phần còn lại — Staff hoặc Admin.
    /// MethodId: 1=Trực tiếp, 2=MoMo, 3=VNPay (Staff tự chọn khi ghi nhận).
    /// SP sp_RecordFullPayment cập nhật Payment + kiểm tra tổng đã thanh toán.
    /// </summary>
    public async Task RecordFullPaymentAsync(
        int bookingId, ConfirmPaymentRequest request, int userId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        if (booking.StatusId != (int)BookingStatusEnum.Confirmed)
            throw new BusinessException(
                "Booking chưa được xác nhận hoặc không thể thanh toán ở trạng thái này.", 400);

        await StoredProcedureHelper.RecordFullPaymentAsync(
            _ctx,
            bookingId: bookingId,
            methodId: request.MethodId,
            transactionCode: request.TransactionCode,
            userId: userId);
    }

    // ── Online payment router (gọi từ IPN) ───────────────────────

    /// <summary>
    /// Router dùng cho IPN callback: tự phân loại là cọc hay thanh toán còn lại
    /// dựa trên StatusId hiện tại của booking.
    /// Idempotent: bỏ qua nếu booking đã thanh toán đủ.
    /// </summary>
    public async Task RecordOnlinePaymentAsync(
        int bookingId, decimal amount, int methodId, string transactionCode)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        // Idempotent: VNPay/MoMo có thể gọi IPN nhiều lần
        var alreadyPaid = await _paymentRepo.GetTotalPaidAsync(bookingId);
        if (alreadyPaid >= (booking.TotalAmount ?? 0)) return;

        if (booking.StatusId == (int)BookingStatusEnum.PendingDeposit)
        {
            await StoredProcedureHelper.RecordDepositAsync(
                _ctx, bookingId, amount, methodId, transactionCode, userId: null);
        }
        else if (booking.StatusId == (int)BookingStatusEnum.Confirmed)
        {
            await StoredProcedureHelper.RecordFullPaymentAsync(
                _ctx, bookingId, methodId, transactionCode, userId: null);
        }
        // Các StatusId khác (Cancelled, Completed…) → bỏ qua
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

    public async Task<BookingResponse> GetBookingForPaymentAsync(int bookingId)
    {
        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
            ?? throw new NotFoundException("Booking", bookingId);

        // Chỉ cho tạo payment URL khi booking đang ở trạng thái cần thanh toán
        if (booking.StatusId != (int)BookingStatusEnum.Confirmed
            && booking.StatusId != (int)BookingStatusEnum.PendingDeposit)
            throw new BusinessException(
                "Booking không ở trạng thái có thể thanh toán.", 400);

        return BookingMapper.MapDetail(booking);
    }
}