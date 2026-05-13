using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifService;

    public NotificationsController(INotificationService notifService)
        => _notifService = notifService;

    /// <summary>
    /// Lấy danh sách thông báo của user hiện tại.
    /// Có thể lọc theo trạng thái đã đọc / chưa đọc, phân trang.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationResponse>>), 200)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] GetNotificationsRequest request)
    {
        var userId = User.GetUserId();
        var result = await _notifService.GetByUserAsync(userId, request);
        return Ok(ApiResponse<PagedResponse<NotificationResponse>>.Ok(result));
    }

    /// <summary>Lấy số thông báo chưa đọc của user hiện tại.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.GetUserId();
        var count = await _notifService.CountUnreadAsync(userId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>Đánh dấu một thông báo là đã đọc.</summary>
    [HttpPatch("{notificationId:int}/read")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var userId = User.GetUserId();
        await _notifService.MarkAsReadAsync(userId, notificationId);
        return Ok(ApiResponse.Ok("Đã đánh dấu thông báo là đã đọc."));
    }

    /// <summary>Đánh dấu tất cả thông báo của user hiện tại là đã đọc.</summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        await _notifService.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse.Ok("Đã đánh dấu tất cả thông báo là đã đọc."));
    }
}
