//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/notifications")]
//[Authorize]
//public class NotificationsController : ControllerBase
//{
//    private readonly INotificationService _notificationService;

//    public NotificationsController(INotificationService notificationService)
//    {
//        _notificationService = notificationService;
//    }

//    /// <summary>Lấy thông báo của bản thân.</summary>
//    [HttpGet]
//    public async Task<IActionResult> GetNotifications(
//        [FromQuery] bool? isRead,
//        [FromQuery] int page = 1,
//        [FromQuery] int pageSize = 20)
//    {
//        var userId = User.GetUserId();
//        var result = await _notificationService.GetNotificationsAsync(userId, isRead, page, pageSize);
//        return Ok(ApiResponse<PagedResponse<NotificationResponse>>.Ok(result));
//    }

//    /// <summary>Đánh dấu đã đọc 1 thông báo.</summary>
//    [HttpPatch("{notificationId:int}/read")]
//    public async Task<IActionResult> MarkAsRead(int notificationId)
//    {
//        var userId = User.GetUserId();
//        await _notificationService.MarkAsReadAsync(userId, notificationId);
//        return Ok(ApiResponse.Ok("Đã đánh dấu đã đọc."));
//    }

//    /// <summary>Đánh dấu tất cả đã đọc.</summary>
//    [HttpPost("read-all")]
//    public async Task<IActionResult> MarkAllAsRead()
//    {
//        var userId = User.GetUserId();
//        await _notificationService.MarkAllAsReadAsync(userId);
//        return Ok(ApiResponse.Ok("Đã đánh dấu tất cả đã đọc."));
//    }
//}