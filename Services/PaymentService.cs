//using KLCN_API.Data;
//using KLCN_API.Helpers;
//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;

//namespace KLCN_API.Services;

//public class PaymentService : IPaymentService
//{
//    private readonly IPaymentRepository _paymentRepo;
//    private readonly IDepositRepository _depositRepo;
//    private readonly IBookingRepository _bookingRepo;
//    private readonly SportPlusDbContext _ctx;

//    public PaymentService(
//        IPaymentRepository paymentRepo,
//        IDepositRepository depositRepo,
//        IBookingRepository bookingRepo,
//        SportPlusDbContext ctx)
//    {
//        _paymentRepo = paymentRepo;
//        _depositRepo = depositRepo;
//        _bookingRepo = bookingRepo;
//        _ctx = ctx;
//    }

//    public async Task RecordDepositAsync(
//        int bookingId, RecordDepositRequest request, int userId)
//    {
//        // Kiểm tra booking tồn tại và thuộc về user
//        // (Admin/Staff cũng được ghi nhận cọc thay customer)
//        var booking = await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        if (booking.StatusId != 5) // Chờ đặt cọc
//            throw new BusinessException(
//                "Booking không ở trạng thái chờ đặt cọc.", 400);

//        await StoredProcedureHelper.RecordDepositAsync(
//            _ctx, bookingId, request.Amount,
//            request.MethodId, request.TransactionCode, userId);
//    }

//    public async Task RecordFullPaymentAsync(
//        int bookingId, ConfirmPaymentRequest request, int userId)
//    {
//        var booking = await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        if (booking.StatusId != 2) // Đã xác nhận
//            throw new BusinessException(
//                "Booking chưa được xác nhận hoặc không thể thanh toán ở trạng thái này.", 400);

//        await StoredProcedureHelper.RecordFullPaymentAsync(
//            _ctx, bookingId, request.MethodId, request.TransactionCode, userId);
//    }

//    public async Task<List<PaymentResponse>> GetPaymentsByBookingAsync(int bookingId)
//    {
//        var payments = await _paymentRepo.GetByBookingAsync(bookingId);

//        return payments.Select(p => new PaymentResponse
//        {
//            PaymentId = p.PaymentId,
//            BookingId = p.BookingId,
//            Amount = p.Amount,
//            Status = p.Status?.Name ?? string.Empty,
//            StatusId = p.StatusId,
//            PaymentMethod = p.Method?.Name ?? string.Empty,
//            MethodId = p.MethodId,
//            TransactionCode = p.TransactionCode,
//            Note = p.Note,
//            PaidAt = p.PaidAt,
//            CreatedAt = p.CreatedAt
//        }).ToList();
//    }

//    public async Task<DepositResponse?> GetDepositByBookingAsync(int bookingId)
//    {
//        var deposit = await _depositRepo.GetByBookingAsync(bookingId);
//        if (deposit is null) return null;

//        return new DepositResponse
//        {
//            DepositId = deposit.DepositId,
//            BookingId = deposit.BookingId,
//            RequiredAmount = deposit.RequiredAmount,
//            PaidAmount = deposit.PaidAmount,
//            Status = deposit.Status?.Name ?? string.Empty,
//            StatusId = deposit.StatusId,
//            DeadlineAt = deposit.DeadlineAt,
//            MinutesLeft = Math.Max(0,
//                (int)(deposit.DeadlineAt - DateTime.UtcNow).TotalMinutes),
//            PaidAt = deposit.PaidAt
//        };
//    }

//    public async Task RecordOnlinePaymentAsync(
//        int bookingId, decimal amount, int methodId, string transactionCode)
//    {
//        var booking = await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        // Idempotent: nếu đã thanh toán rồi thì bỏ qua (VNPay có thể gọi IPN nhiều lần)
//        var alreadyPaid = await _paymentRepo.GetTotalPaidAsync(bookingId);
//        if (alreadyPaid >= (booking.TotalAmount ?? 0)) return;

//        if (booking.StatusId == 5) // Chờ đặt cọc
//            await StoredProcedureHelper.RecordDepositAsync(
//                _ctx, bookingId, amount, methodId, transactionCode, userId: null);
//        else
//            await StoredProcedureHelper.RecordFullPaymentAsync(
//                _ctx, bookingId, methodId, transactionCode, userId: null);
//    }

//    public async Task<BookingResponse> GetBookingForPaymentAsync(int bookingId)
//    {
//        var booking = await _bookingRepo.GetWithDetailsAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        if (booking.StatusId != 2 && booking.StatusId != 5)
//            throw new BusinessException(
//                "Booking không ở trạng thái có thể thanh toán.", 400);

//        return BookingMapper.MapDetail(booking);
//    }
//}