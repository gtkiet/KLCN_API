using KLCN_API.Data;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KLCN_API.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly SportPlusDbContext _ctx;

    public NotificationRepository(SportPlusDbContext ctx) => _ctx = ctx;

    public async Task<(List<Notification> Items, int TotalCount)> GetByUserAsync(
        int userId, bool? isRead, int page, int pageSize)
    {
        var query = _ctx.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        query = query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> CountUnreadAsync(int userId)
        => await _ctx.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(int notificationId, int userId)
        => await _ctx.Notifications
            .Where(n => n.NotificationId == notificationId && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    public async Task MarkAllAsReadAsync(int userId)
        => await _ctx.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    public async Task<Notification> AddAsync(Notification notification)
    {
        await _ctx.Notifications.AddAsync(notification);
        await _ctx.SaveChangesAsync();
        return notification;
    }
}
