//using KLCN_API.Data;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace KLCN_API.Repositories;

//public class BookingRepository : IBookingRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public BookingRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<Booking?> GetByIdAsync(int bookingId)
//        => await _ctx.Bookings
//            .Include(b => b.User).ThenInclude(u => u.Profile)
//            .Include(b => b.Status)
//            .Include(b => b.Promotion)
//            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

//    public async Task<Booking?> GetWithDetailsAsync(int bookingId)
//        => await _ctx.Bookings
//            .Include(b => b.User).ThenInclude(u => u.Profile)
//            .Include(b => b.Status)
//            .Include(b => b.Promotion)
//            .Include(b => b.Details)
//                .ThenInclude(d => d.FieldSlot)
//                    .ThenInclude(fs => fs.Field).ThenInclude(f => f.Type)
//            .Include(b => b.Details)
//                .ThenInclude(d => d.FieldSlot)
//                    .ThenInclude(fs => fs.Slot)
//            .Include(b => b.BookingServices)
//                .ThenInclude(bs => bs.Service)
//            .Include(b => b.Deposit).ThenInclude(d => d!.Status)
//            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

//    public async Task<(List<Booking> Items, int TotalCount)> GetBookingsAsync(
//        int? userId, int? statusId, DateOnly? dateFrom, DateOnly? dateTo,
//        int? fieldId, int page, int pageSize)
//    {
//        var query = _ctx.Bookings
//            .Include(b => b.User)
//            .Include(b => b.Status)
//            .Include(b => b.Details)
//                .ThenInclude(d => d.FieldSlot)
//                    .ThenInclude(fs => fs.Field)
//            .Include(b => b.Details)
//                .ThenInclude(d => d.FieldSlot)
//                    .ThenInclude(fs => fs.Slot)
//            .AsQueryable();

//        if (userId.HasValue)
//            query = query.Where(b => b.UserId == userId.Value);

//        if (statusId.HasValue)
//            query = query.Where(b => b.StatusId == statusId.Value);

//        if (dateFrom.HasValue)
//            query = query.Where(b => b.Details.Any(d => d.FieldSlot.SlotDate >= dateFrom.Value));

//        if (dateTo.HasValue)
//            query = query.Where(b => b.Details.Any(d => d.FieldSlot.SlotDate <= dateTo.Value));

//        if (fieldId.HasValue)
//            query = query.Where(b => b.Details.Any(d => d.FieldSlot.FieldId == fieldId.Value));

//        query = query.OrderByDescending(b => b.CreatedAt);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<List<Booking>> GetActiveByUserAsync(int userId)
//        => await _ctx.Bookings
//            .Where(b => b.UserId == userId && new[] { 1, 2, 5 }.Contains(b.StatusId))
//            .ToListAsync();

//    public async Task<Booking> CreateAsync(Booking booking)
//    {
//        await _ctx.Bookings.AddAsync(booking);
//        await _ctx.SaveChangesAsync();
//        return booking;
//    }

//    public async Task UpdateAsync(Booking booking)
//    {
//        booking.UpdatedAt = DateTime.UtcNow;
//        _ctx.Bookings.Update(booking);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task AddBookingDetailAsync(BookingDetail detail)
//    {
//        await _ctx.BookingDetails.AddAsync(detail);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task AddBookingServiceAsync(BookingService service)
//    {
//        await _ctx.BookingServices.AddAsync(service);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task<List<BookingService>> GetBookingServicesAsync(int bookingId)
//        => await _ctx.BookingServices
//            .Include(bs => bs.Service)
//            .Where(bs => bs.BookingId == bookingId)
//            .ToListAsync();
//}