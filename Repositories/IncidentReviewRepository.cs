//using KLCN_API.Data;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace KLCN_API.Repositories;

//public class IncidentRepository : IIncidentRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public IncidentRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<Incident?> GetByIdAsync(int incidentId)
//        => await _ctx.Incidents
//            .Include(i => i.Field)
//            .Include(i => i.ReportedByUser)
//            .Include(i => i.HandledByUser)
//            .Include(i => i.Status)
//            .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

//    public async Task<(List<Incident> Items, int TotalCount)> GetIncidentsAsync(
//        int? fieldId, int? statusId, int page, int pageSize)
//    {
//        var query = _ctx.Incidents
//            .Include(i => i.Field)
//            .Include(i => i.ReportedByUser)
//            .Include(i => i.Status)
//            .AsQueryable();

//        if (fieldId.HasValue)
//            query = query.Where(i => i.FieldId == fieldId.Value);

//        if (statusId.HasValue)
//            query = query.Where(i => i.StatusId == statusId.Value);

//        query = query.OrderByDescending(i => i.CreatedAt);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<Incident> CreateAsync(Incident incident)
//    {
//        await _ctx.Incidents.AddAsync(incident);
//        await _ctx.SaveChangesAsync();
//        return incident;
//    }

//    public async Task UpdateAsync(Incident incident)
//    {
//        _ctx.Incidents.Update(incident);
//        await _ctx.SaveChangesAsync();
//    }
//}

//public class ReviewRepository : IReviewRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public ReviewRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<Review?> GetByIdAsync(int reviewId)
//        => await _ctx.Reviews
//            .Include(r => r.User).ThenInclude(u => u.Profile)
//            .Include(r => r.Field)
//            .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

//    public async Task<Review?> GetByBookingAsync(int bookingId)
//        => await _ctx.Reviews
//            .FirstOrDefaultAsync(r => r.BookingId == bookingId);

//    public async Task<(List<Review> Items, int TotalCount)> GetReviewsAsync(
//        int? fieldId, int? rating, bool? isVisible, int page, int pageSize)
//    {
//        var query = _ctx.Reviews
//            .Include(r => r.User).ThenInclude(u => u.Profile)
//            .Include(r => r.Field)
//            .AsQueryable();

//        if (fieldId.HasValue)
//            query = query.Where(r => r.FieldId == fieldId.Value);

//        if (rating.HasValue)
//            query = query.Where(r => r.Rating == rating.Value);

//        if (isVisible.HasValue)
//            query = query.Where(r => r.IsVisible == isVisible.Value);

//        query = query.OrderByDescending(r => r.CreatedAt);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<Review> CreateAsync(Review review)
//    {
//        await _ctx.Reviews.AddAsync(review);
//        await _ctx.SaveChangesAsync();
//        return review;
//    }

//    public async Task UpdateVisibilityAsync(int reviewId, bool isVisible)
//        => await _ctx.Reviews
//            .Where(r => r.ReviewId == reviewId)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(r => r.IsVisible, isVisible)
//                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
//}