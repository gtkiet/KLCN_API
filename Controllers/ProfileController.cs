using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
        => _profileService = profileService;

    /// <summary>Lấy thông tin cá nhân của người dùng đang đăng nhập.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserDetailResponse>), 200)]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        var result = await _profileService.GetProfileAsync(userId);
        return Ok(ApiResponse<UserDetailResponse>.Ok(result));
    }

    /// <summary>Cập nhật thông tin cá nhân (họ tên, SĐT, ngày sinh, địa chỉ).</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var result = await _profileService.UpdateProfileAsync(userId, request);
        return Ok(ApiResponse<UserDetailResponse>.Ok(result, "Cập nhật thông tin thành công."));
    }

    /// <summary>Đổi mật khẩu.</summary>
    [HttpPut("change-password")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.GetUserId();
        await _profileService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse.Ok("Đổi mật khẩu thành công."));
    }

    /// <summary>
    /// Cập nhật avatar — multipart/form-data, field tên "file".
    /// Chỉ nhận JPG, PNG, WebP, tối đa 5MB.
    /// Ảnh cũ sẽ bị xóa khỏi disk.
    /// </summary>
    [HttpPut("avatar")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new BusinessException("Vui lòng chọn file ảnh.", 400);

        var userId = User.GetUserId();
        var avatarUrl = await _profileService.UpdateAvatarAsync(userId, file);

        return Ok(ApiResponse<object>.Ok(new { avatarUrl }, "Cập nhật avatar thành công."));
    }
}
