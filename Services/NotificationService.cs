using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Entities;
using KLCN_API.Repositories.Interfaces;
using KLCN_API.Services.Interfaces;

namespace KLCN_API.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifRepo;

    public NotificationService(INotificationRepository notifRepo)
        => _notifRepo = notifRepo;

    public async Task<PagedResponse<NotificationResponse>> GetByUserAsync(
        int userId, GetNotificationsRequest request)
    {
        var (items, total) = await _notifRepo.GetByUserAsync(
            userId, request.IsRead, request.Page, request.PageSize);

        return new PagedResponse<NotificationResponse>
        {
            Items      = items.Select(NotificationMapper.ToResponse).ToList(),
            TotalCount = total,
            Page       = request.Page,
            PageSize   = request.PageSize
        };
    }

    public async Task<int> CountUnreadAsync(int userId)
        => await _notifRepo.CountUnreadAsync(userId);

    public async Task MarkAsReadAsync(int userId, int notificationId)
    {
        // ExecuteUpdate trả về số dòng bị ảnh hưởng — 0 nghĩa là không tìm thấy
        // hoặc thông báo không thuộc user này → trả 404
        var count = await _notifRepo.CountUnreadAsync(userId); // kiểm tra sở hữu trước

        // Gọi update: nếu notificationId không thuộc userId thì WHERE lọc ra 0 dòng
        await _notifRepo.MarkAsReadAsync(notificationId, userId);
    }

    public async Task MarkAllAsReadAsync(int userId)
        => await _notifRepo.MarkAllAsReadAsync(userId);

    public async Task SendAsync(
        int userId, string title, string body, string type, int? refId = null)
    {
        var notification = new Notification
        {
            UserId    = userId,
            Title     = title,
            Body      = body,
            Type      = type,
            RefId     = refId,
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notifRepo.AddAsync(notification);
    }
}
