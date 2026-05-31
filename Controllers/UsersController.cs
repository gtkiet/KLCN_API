using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Middleware;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>Lấy danh sách user có filter + phân trang — Admin và Staff.</summary>
    [HttpGet]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<UserResponse>>), 200)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
    {
        var result = await _userService.GetUsersAsync(request);
        return Ok(ApiResponse<PagedResponse<UserResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết 1 user — Admin và Staff.</summary>
    [HttpGet("{userId:int}")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetById(int userId)
    {
        var result = await _userService.GetByIdAsync(userId);
        return Ok(ApiResponse<UserDetailResponse>.Ok(result));
    }

    /// <summary>Tạo nhân viên — chỉ Admin.</summary>
    [HttpPost("staff")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var result = await _userService.CreateStaffAsync(request);
        return Ok(ApiResponse<UserDetailResponse>.Ok(result, "Tạo nhân viên thành công."));
    }

    /// <summary>Tạo khách hàng từ trang quản trị — Admin và Staff.</summary>
    [HttpPost("customer")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerByAdminRequest request)
    {
        var result = await _userService.CreateCustomerByAdminAsync(request);
        return Ok(ApiResponse<UserDetailResponse>.Ok(result, "Tạo khách hàng thành công."));
    }

    ///// <summary>Cập nhật thông tin user — Admin và Staff.</summary>
    [HttpPut("{userId:int}")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    public async Task<IActionResult> Update(
        int userId, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateUserAsync(userId, request);
        return Ok(ApiResponse<UserDetailResponse>.Ok(result, "Cập nhật thành công."));
    }

    /// <summary>
    /// Đổi role user — chỉ Admin.
    /// Không thể gán role Admin, không thể đổi role của chính mình.
    /// roleId: 2=Staff, 3=Customer.
    /// </summary>
    [HttpPatch("{userId:int}/role")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 403)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> UpdateRole(int userId, [FromBody] UpdateRoleRequest request)
    {
        await _userService.UpdateRoleAsync(userId, request.RoleId, User.GetUserId());
        return Ok(ApiResponse.Ok("Đổi role thành công."));
    }

    /// <summary>Khóa tài khoản user — chỉ Admin.</summary>
    [HttpPatch("{userId:int}/lock")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Lock(int userId)
    {
        if (userId == User.GetUserId())
            throw new BusinessException("Không thể khóa tài khoản của chính mình.", 400);

        await _userService.LockUserAsync(userId);
        return Ok(ApiResponse.Ok("Khóa tài khoản thành công."));
    }

    /// <summary>Mở khóa tài khoản user — chỉ Admin.</summary>
    [HttpPatch("{userId:int}/unlock")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Unlock(int userId)
    {
        await _userService.UnlockUserAsync(userId);
        return Ok(ApiResponse.Ok("Mở khóa tài khoản thành công."));
    }

    /// <summary>Xóa mềm user — chỉ Admin. Không thể xóa Admin hoặc chính mình.</summary>
    [HttpDelete("{userId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Delete(int userId)
    {
        if (userId == User.GetUserId())
            throw new BusinessException("Không thể xóa tài khoản của chính mình.", 400);

        await _userService.DeleteUserAsync(userId);
        return Ok(ApiResponse.Ok("Xóa tài khoản thành công."));
    }
}