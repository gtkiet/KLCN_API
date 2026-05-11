//using KLCN_API.Helpers;
//using KLCN_API.Middleware;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Repositories.Interfaces;
//using KLCN_API.Services.Interfaces;
//using Microsoft.Data.SqlClient;

//namespace KLCN_API.Services;

//public class PaymentService : IPaymentService
//{
//    private readonly IBookingRepository _bookingRepo;
//    private readonly IPaymentRepository _paymentRepo;
//    private readonly IDepositRepository _depositRepo;
//    private readonly StoredProcedureHelper _sp;

//    public PaymentService(
//        IBookingRepository bookingRepo,
//        IPaymentRepository paymentRepo,
//        IDepositRepository depositRepo,
//        StoredProcedureHelper sp)
//    {
//        _bookingRepo = bookingRepo;
//        _paymentRepo = paymentRepo;
//        _depositRepo = depositRepo;
//        _sp = sp;
//    }

//    public async Task RecordDepositAsync(int bookingId, RecordDepositRequest request, int userId)
//    {
//        await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        await _sp.ExecuteAsync("sp_RecordDeposit",
//            new SqlParameter("@BookingId", bookingId),
//            new SqlParameter("@Amount", request.Amount),
//            new SqlParameter("@MethodId", request.MethodId),
//            new SqlParameter("@TransactionCode", (object?)request.TransactionCode ?? DBNull.Value),
//            new SqlParameter("@UserId", userId));
//    }

//    public async Task RecordFullPaymentAsync(int bookingId, ConfirmPaymentRequest request, int userId)
//    {
//        await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

//        await _sp.ExecuteAsync("sp_RecordFullPayment",
//            new SqlParameter("@BookingId", bookingId),
//            new SqlParameter("@MethodId", request.MethodId),
//            new SqlParameter("@TransactionCode", (object?)request.TransactionCode ?? DBNull.Value),
//            new SqlParameter("@UserId", userId));
//    }

//    public async Task<List<PaymentResponse>> GetPaymentsByBookingAsync(int bookingId)
//    {
//        await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

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
//        await _bookingRepo.GetByIdAsync(bookingId)
//            ?? throw new NotFoundException("Booking", bookingId);

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
//            MinutesLeft = (int)(deposit.DeadlineAt - DateTime.UtcNow).TotalMinutes,
//            PaidAt = deposit.PaidAt
//        };
//    }
//}