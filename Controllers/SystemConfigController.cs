//using KLCN_API.Filters;
//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/system-config")]
//[AuthorizeRoles(RoleEnum.Admin)]
//public class SystemConfigController : ControllerBase
//{
//    private readonly ISystemConfigService _systemConfigService;

//    public SystemConfigController(ISystemConfigService systemConfigService)
//    {
//        _systemConfigService = systemConfigService;
//    }

//    /// <summary>Lấy toàn bộ cấu hình hệ thống.</summary>
//    [HttpGet]
//    public async Task<IActionResult> GetAll()
//    {
//        var result = await _systemConfigService.GetAllConfigsAsync();
//        return Ok(ApiResponse<List<SystemConfigResponse>>.Ok(result));
//    }

//    /// <summary>Cập nhật 1 cấu hình.</summary>
//    [HttpPut]
//    public async Task<IActionResult> Update([FromBody] UpdateSystemConfigRequest request)
//    {
//        var userId = User.GetUserId();
//        await _systemConfigService.UpdateConfigAsync(request, userId);
//        return Ok(ApiResponse.Ok("Cập nhật cấu hình thành công."));
//    }
//}