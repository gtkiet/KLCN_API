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

    private static (bool IsGuest, string GuestName, string GuestPhone) ExtractGuestInfo(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return (false, string.Empty, string.Empty);

        const string prefix = "[KHÁCH VÃNG LAI]";
        var trimmed = note.Trim();

        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return (false, string.Empty, string.Empty);

        var raw = trimmed.Substring(prefix.Length).Trim();

        // Nếu note có thêm phần ghi chú phía sau:
        // [KHÁCH VÃNG LAI] chị Liễu - 098765322 | ghi chú thêm
        var pipeIndex = raw.IndexOf('|');
        if (pipeIndex >= 0)
            raw = raw.Substring(0, pipeIndex).Trim();

        string guestName = raw;
        string guestPhone = string.Empty;

        var lastDashIndex = raw.LastIndexOf(" - ", StringComparison.Ordinal);
        if (lastDashIndex >= 0)
        {
            guestName = raw.Substring(0, lastDashIndex).Trim();
            guestPhone = raw.Substring(lastDashIndex + 3).Trim();
        }

        if (string.IsNullOrWhiteSpace(guestName))
            guestName = "Khách vãng lai";

        return (true, guestName, guestPhone);
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
                        && p.StatusId == 2);

        var totalCount = await query.CountAsync();

        var rawItems = await query
            .OrderByDescending(p => p.PaidAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.PaymentId,
                InvoiceCode = $"INV-{p.PaidAt!.Value:yyyyMMdd}-{p.PaymentId:D5}",
                BookingNote = p.Booking.Note,
                UserFullName = p.Booking.User.FullName,
                Amount = p.Amount
            })
            .ToListAsync();

        var items = rawItems
            .Select(x =>
            {
                var guest = ExtractGuestInfo(x.BookingNote);

                return new InvoiceListItemResponse
                {
                    PaymentId = x.PaymentId,
                    InvoiceCode = x.InvoiceCode,
                    CustomerName = guest.IsGuest ? guest.GuestName : x.UserFullName,
                    Amount = x.Amount
                };
            })
            .ToList();

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

        var guest = ExtractGuestInfo(payment.Booking.Note);

        return new InvoiceDetailResponse
        {
            PaymentId = payment.PaymentId,
            InvoiceCode = $"INV-{(payment.PaidAt ?? payment.CreatedAt):yyyyMMdd}-{payment.PaymentId:D5}",

            BookingId = payment.BookingId,
            CustomerName = guest.IsGuest ? guest.GuestName : payment.Booking.User.FullName,
            CustomerPhone = guest.IsGuest ? guest.GuestPhone : payment.Booking.User.Phone,
            CustomerEmail = guest.IsGuest ? string.Empty : payment.Booking.User.Email,

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