using KLCN_API.Filters;
using KLCN_API.Models.DTOs.Request;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;

    public ServicesController(IServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    /// <summary>Lấy danh sách dịch vụ — Public.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetServices([FromQuery] bool? isAvailable)
    {
        var result = await _serviceService.GetAllAsync(isAvailable);
        return Ok(ApiResponse<List<ServiceResponse>>.Ok(result));
    }

    /// <summary>Tạo dịch vụ — Admin.</summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request)
    {
        var result = await _serviceService.CreateAsync(request);
        return Ok(ApiResponse<ServiceResponse>.Ok(result, "Tạo dịch vụ thành công."));
    }

    /// <summary>Cập nhật dịch vụ — Admin.</summary>
    [HttpPut("{serviceId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    public async Task<IActionResult> Update(int serviceId, [FromBody] UpdateServiceRequest request)
    {
        var result = await _serviceService.UpdateAsync(serviceId, request);
        return Ok(ApiResponse<ServiceResponse>.Ok(result, "Cập nhật dịch vụ thành công."));
    }

    /// <summary>Xóa mềm dịch vụ — Admin.</summary>
    [HttpDelete("{serviceId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    public async Task<IActionResult> Delete(int serviceId)
    {
        await _serviceService.DeleteAsync(serviceId);
        return Ok(ApiResponse.Ok("Xóa dịch vụ thành công."));
    }
}