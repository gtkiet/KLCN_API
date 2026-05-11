//using KLCN_API.Data;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace KLCN_API.Repositories;

//public class PromotionRepository : IPromotionRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public PromotionRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<Promotion?> GetByIdAsync(int promotionId)
//        => await _ctx.Promotions
//            .Include(p => p.Type)
//            .FirstOrDefaultAsync(p => p.PromotionId == promotionId);

//    public async Task<Promotion?> GetActiveByCodeAsync(string code)
//    {
//        var today = DateOnly.FromDateTime(DateTime.UtcNow);
//        return await _ctx.Promotions
//            .Include(p => p.Type)
//            .FirstOrDefaultAsync(p =>
//                p.Code == code &&
//                p.IsActive &&
//                p.StartDate <= today &&
//                p.EndDate >= today &&
//                p.UsageCount < p.UsageLimit);
//    }

//    public async Task<(List<Promotion> Items, int TotalCount)> GetPromotionsAsync(
//        bool? isActive, int page, int pageSize)
//    {
//        var query = _ctx.Promotions
//            .Include(p => p.Type)
//            .AsQueryable();

//        if (isActive.HasValue)
//            query = query.Where(p => p.IsActive == isActive.Value);

//        query = query.OrderByDescending(p => p.CreatedAt);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<Promotion> CreateAsync(Promotion promotion)
//    {
//        await _ctx.Promotions.AddAsync(promotion);
//        await _ctx.SaveChangesAsync();
//        return promotion;
//    }

//    public async Task UpdateAsync(Promotion promotion)
//    {
//        _ctx.Promotions.Update(promotion);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task IncrementUsageAsync(int promotionId)
//        => await _ctx.Promotions
//            .Where(p => p.PromotionId == promotionId)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(p => p.UsageCount, p => p.UsageCount + 1));
//}