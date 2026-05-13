using KLCN_API.Filters;
using KLCN_API.Helpers;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/system-config")]
[Authorize]
public class SystemConfigController : ControllerBase
{
    private readonly ISystemConfigService _configService;

    public SystemConfigController(ISystemConfigService configService)
        => _configService = configService;

    /// <summary>
    /// Lấy toàn bộ cấu hình hệ thống.
    /// Admin và Staff có thể xem — Staff chỉ đọc, Admin có thể sửa.
    /// </summary>
    [HttpGet]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<List<SystemConfigResponse>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _configService.GetAllAsync();
        return Ok(ApiResponse<List<SystemConfigResponse>>.Ok(result));
    }

    /// <summary>Lấy một mục cấu hình theo key.</summary>
    [HttpGet("{key}")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<SystemConfigResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetByKey(string key)
    {
        var result = await _configService.GetByKeyAsync(key);
        return Ok(ApiResponse<SystemConfigResponse>.Ok(result));
    }

    /// <summary>
    /// Cập nhật giá trị một mục cấu hình — chỉ Admin.
    /// Ví dụ: DEPOSIT_REQUIRED_PERCENT, TAX_PERCENT, MIN_CANCEL_BEFORE_HOURS.
    /// </summary>
    [HttpPut("{key}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 422)]
    public async Task<IActionResult> Update(
        string key, [FromBody] UpdateSystemConfigRequest request)
    {
        var adminId = User.GetUserId();
        await _configService.UpdateAsync(key, request, adminId);
        return Ok(ApiResponse.Ok($"Cập nhật cấu hình '{key}' thành công."));
    }
}
