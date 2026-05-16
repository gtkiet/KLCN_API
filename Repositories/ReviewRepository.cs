using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly SportPlusDbContext _ctx;

    public ReviewRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<Review?> GetByIdAsync(int reviewId)
        => await _ctx.Reviews
            .Include(r => r.User).ThenInclude(u => u.Profile)
            .Include(r => r.Field)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

    public async Task<Review?> GetByBookingAsync(int bookingId)
        => await _ctx.Reviews
            .FirstOrDefaultAsync(r => r.BookingId == bookingId);

    public async Task<(List<Review> Items, int TotalCount)> GetReviewsAsync(
        int? fieldId, int? rating, bool? isVisible, int page, int pageSize)
    {
        var query = _ctx.Reviews
            .Include(r => r.User).ThenInclude(u => u.Profile)
            .Include(r => r.Field)
            .AsQueryable();

        if (fieldId.HasValue)
            query = query.Where(r => r.FieldId == fieldId.Value);

        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating.Value);

        if (isVisible.HasValue)
            query = query.Where(r => r.IsVisible == isVisible.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        await _ctx.Reviews.AddAsync(review);
        await _ctx.SaveChangesAsync();

        // Reload navigations để mapper dùng ngay
        await _ctx.Entry(review).Reference(r => r.User).LoadAsync();
        await _ctx.Entry(review).Reference(r => r.Field).LoadAsync();
        if (review.User is not null)
            await _ctx.Entry(review.User).Reference(u => u.Profile).LoadAsync();

        return review;
    }

    public async Task UpdateVisibilityAsync(int reviewId, bool isVisible)
        => await _ctx.Reviews
            .Where(r => r.ReviewId == reviewId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsVisible, isVisible)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

    public async Task<FieldRatingRaw?> GetFieldRatingRawAsync(int fieldId)
        => await _ctx.Database
            .SqlQueryRaw<FieldRatingRaw>(
                "SELECT * FROM vw_FieldRatings WHERE FieldId = {0}", fieldId)
            .FirstOrDefaultAsync();
}