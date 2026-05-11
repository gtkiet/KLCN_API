//using KLCN_API.Data;
//using KLCN_API.Models.Entities;
//using KLCN_API.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace KLCN_API.Repositories;

//public class NotificationRepository : INotificationRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public NotificationRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<(List<Notification> Items, int TotalCount)> GetByUserAsync(
//        int userId, bool? isRead, int page, int pageSize)
//    {
//        var query = _ctx.Notifications
//            .Where(n => n.UserId == userId)
//            .AsQueryable();

//        if (isRead.HasValue)
//            query = query.Where(n => n.IsRead == isRead.Value);

//        query = query.OrderByDescending(n => n.CreatedAt);

//        var totalCount = await query.CountAsync();
//        var items = await query
//            .Skip((page - 1) * pageSize)
//            .Take(pageSize)
//            .ToListAsync();

//        return (items, totalCount);
//    }

//    public async Task<int> CountUnreadAsync(int userId)
//        => await _ctx.Notifications
//            .CountAsync(n => n.UserId == userId && !n.IsRead);

//    public async Task MarkAsReadAsync(int notificationId, int userId)
//        => await _ctx.Notifications
//            .Where(n => n.NotificationId == notificationId && n.UserId == userId)
//            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

//    public async Task MarkAllAsReadAsync(int userId)
//        => await _ctx.Notifications
//            .Where(n => n.UserId == userId && !n.IsRead)
//            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

//    public async Task<Notification> AddAsync(Notification notification)
//    {
//        await _ctx.Notifications.AddAsync(notification);
//        await _ctx.SaveChangesAsync();
//        return notification;
//    }
//}

//public class SystemConfigRepository : ISystemConfigRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public SystemConfigRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<List<SystemConfig>> GetAllAsync()
//        => await _ctx.SystemConfigs
//            .OrderBy(c => c.ConfigKey)
//            .ToListAsync();

//    public async Task<SystemConfig?> GetByKeyAsync(string key)
//        => await _ctx.SystemConfigs
//            .FirstOrDefaultAsync(c => c.ConfigKey == key);

//    public async Task UpdateAsync(string key, string value, int updatedBy)
//        => await _ctx.SystemConfigs
//            .Where(c => c.ConfigKey == key)
//            .ExecuteUpdateAsync(s => s
//                .SetProperty(c => c.ConfigValue, value)
//                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow)
//                .SetProperty(c => c.UpdatedBy, updatedBy));
//}

//public class SpecialDayRepository : ISpecialDayRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public SpecialDayRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<List<SpecialDay>> GetAllAsync()
//        => await _ctx.SpecialDays
//            .Include(s => s.CreatedByUser)
//            .OrderBy(s => s.SpecialDate)
//            .ToListAsync();

//    public async Task<SpecialDay?> GetByIdAsync(int specialDayId)
//        => await _ctx.SpecialDays
//            .FirstOrDefaultAsync(s => s.SpecialDayId == specialDayId);

//    public async Task<SpecialDay?> GetByDateAsync(DateOnly date)
//        => await _ctx.SpecialDays
//            .FirstOrDefaultAsync(s => s.SpecialDate == date);

//    public async Task<SpecialDay> CreateAsync(SpecialDay specialDay)
//    {
//        await _ctx.SpecialDays.AddAsync(specialDay);
//        await _ctx.SaveChangesAsync();
//        return specialDay;
//    }

//    public async Task UpdateAsync(SpecialDay specialDay)
//    {
//        _ctx.SpecialDays.Update(specialDay);
//        await _ctx.SaveChangesAsync();
//    }

//    public async Task DeleteAsync(int specialDayId)
//        => await _ctx.SpecialDays
//            .Where(s => s.SpecialDayId == specialDayId)
//            .ExecuteDeleteAsync();
//}

//public class DashboardRepository : IDashboardRepository
//{
//    private readonly SportPlusDbContext _ctx;

//    public DashboardRepository(SportPlusDbContext ctx) => _ctx = ctx;

//    public async Task<DashboardRaw> GetSummaryAsync()
//        => await _ctx.Database
//            .SqlQuery<DashboardRaw>($"""
//                SELECT
//                    PendingBookings, PendingDepositBookings, TodayConfirmed,
//                    ActiveFields, MaintenanceFields, NewIncidents,
//                    TodayRevenue, ActiveCustomers, LowStockCount, UrgentDepositCount
//                FROM vw_DashboardSummary
//                """)
//            .FirstAsync();

//    public async Task<List<RevenueByMonthRaw>> GetRevenueByMonthAsync(int year)
//        => await _ctx.Database
//            .SqlQuery<RevenueByMonthRaw>($"""
//                SELECT [Year], [Month], TotalBookings, TotalRevenue, AvgBookingValue
//                FROM vw_RevenueByMonth
//                WHERE [Year] = {year}
//                ORDER BY [Month]
//                """)
//            .ToListAsync();

//    public async Task<List<FieldOccupancyRaw>> GetOccupancyAsync(int? year, int? month)
//        => await _ctx.Database
//            .SqlQuery<FieldOccupancyRaw>($"""
//                SELECT FieldId, FieldName, FieldType, [Year], [Month],
//                       TotalSlots, BookedSlots, OccupancyRate
//                FROM vw_FieldOccupancyByMonth
//                WHERE ({year} IS NULL OR [Year] = {year})
//                  AND ({month} IS NULL OR [Month] = {month})
//                ORDER BY [Year] DESC, [Month] DESC, FieldName
//                """)
//            .ToListAsync();

//    public async Task<List<RevenueByServiceRaw>> GetRevenueByServiceAsync()
//        => await _ctx.Database
//            .SqlQuery<RevenueByServiceRaw>($"""
//                SELECT ServiceId, ServiceName, TotalQuantitySold, TotalRevenue, TotalBookings
//                FROM vw_RevenueByService
//                ORDER BY TotalRevenue DESC
//                """)
//            .ToListAsync();
//}