using KLCN_API.Data;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Services;

public class InvoiceService : IInvoiceService
{
    private readonly SportPlusDbContext _ctx;

    public InvoiceService(SportPlusDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<PagedResponse<InvoiceListItemResponse>> GetInvoicesAsync(DateOnly? date, int page = 1, int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var from = targetDate.ToDateTime(TimeOnly.MinValue);
        var to = targetDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var query = _ctx.Payments
            .AsNoTracking()
            .Include(p => p.Booking)
                .ThenInclude(b => b.User)
            .Where(p => p.PaidAt.HasValue
                        && p.PaidAt.Value >= from
                        && p.PaidAt.Value < to
                        && p.StatusId == 2); // giả sử 2 = thành công

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new InvoiceListItemResponse
            {
                PaymentId = p.PaymentId,
                InvoiceCode = $"INV-{p.PaidAt!.Value:yyyyMMdd}-{p.PaymentId:D5}",
                CustomerName = p.Booking.User.FullName,
                Amount = p.Amount
            })
            .ToListAsync();

        return new PagedResponse<InvoiceListItemResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<InvoiceDetailResponse> GetInvoiceByPaymentIdAsync(int paymentId)
    {
        var payment = await _ctx.Payments
            .AsNoTracking()
            .Include(p => p.Booking)
                .ThenInclude(b => b.User)
            .Include(p => p.Method)
            .Include(p => p.Status)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BookingDetails)
                    .ThenInclude(d => d.FieldSlot)
                        .ThenInclude(fs => fs.Field)
                            .ThenInclude(f => f.Type)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BookingDetails)
                    .ThenInclude(d => d.FieldSlot)
                        .ThenInclude(fs => fs.TimeSlot)
            .Include(p => p.Booking)
                .ThenInclude(b => b.BookingServices)
                    .ThenInclude(bs => bs.Service)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
            throw new NotFoundException("Hóa đơn", paymentId);

        return new InvoiceDetailResponse
        {
            PaymentId = payment.PaymentId,
            InvoiceCode = $"INV-{(payment.PaidAt ?? payment.CreatedAt):yyyyMMdd}-{payment.PaymentId:D5}",

            BookingId = payment.BookingId,
            CustomerName = payment.Booking.User.FullName,
            CustomerPhone = payment.Booking.User.Phone,
            CustomerEmail = payment.Booking.User.Email,

            Amount = payment.Amount,
            PaymentMethod = payment.Method.Name,
            PaymentStatus = payment.Status.Name,
            TransactionCode = payment.TransactionCode,
            Note = payment.Note,
            PaidAt = payment.PaidAt,

            Details = payment.Booking.BookingDetails
                .OrderBy(x => x.FieldSlot.SlotDate)
                .ThenBy(x => x.FieldSlot.TimeSlot.StartTime)
                .Select(x => new BookingDetailResponse
                {
                    BookingDetailId = x.BookingDetailId,
                    FieldId = x.FieldSlot.FieldId,
                    FieldName = x.FieldSlot.Field.Name,
                    FieldType = x.FieldSlot.Field.Type.Name,
                    SlotDate = x.FieldSlot.SlotDate,
                    StartTime = x.FieldSlot.TimeSlot.StartTime,
                    EndTime = x.FieldSlot.TimeSlot.EndTime,
                    Price = x.Price
                })
                .ToList(),

            Services = payment.Booking.BookingServices
                .Select(x => new BookingServiceResponse
                {
                    ServiceId = x.ServiceId,
                    ServiceName = x.Service.Name,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice
                })
                .ToList()
        };
    }
}