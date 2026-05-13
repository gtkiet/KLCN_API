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
[Route("api/incidents")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
        => _incidentService = incidentService;

    /// <summary>
    /// Lấy danh sách sự cố — Admin và Staff.
    /// Lọc theo sân hoặc trạng thái, phân trang.
    /// </summary>
    [HttpGet]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IncidentResponse>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? fieldId,
        [FromQuery] int? statusId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _incidentService.GetIncidentsAsync(
            fieldId, statusId, page, pageSize);
        return Ok(ApiResponse<PagedResponse<IncidentResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết một sự cố — Admin và Staff.</summary>
    [HttpGet("{incidentId:int}")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetById(int incidentId)
    {
        var result = await _incidentService.GetByIdAsync(incidentId);
        return Ok(ApiResponse<IncidentResponse>.Ok(result));
    }

    /// <summary>
    /// Báo cáo sự cố mới tại một sân — mọi user đã đăng nhập.
    /// Thường do Staff hoặc Customer báo cáo khi phát hiện vấn đề.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Create([FromBody] CreateIncidentRequest request)
    {
        var reportedBy = User.GetUserId();
        var result = await _incidentService.CreateAsync(request, reportedBy);
        return CreatedAtAction(
            nameof(GetById),
            new { incidentId = result.IncidentId },
            ApiResponse<IncidentResponse>.Ok(result, "Báo cáo sự cố thành công."));
    }

    /// <summary>
    /// Cập nhật trạng thái xử lý sự cố — Admin và Staff.
    /// StatusId: 2 = Đang xử lý, 3 = Đã xử lý.
    /// </summary>
    [HttpPatch("{incidentId:int}/handle")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Handle(
        int incidentId, [FromBody] HandleIncidentRequest request)
    {
        var handledBy = User.GetUserId();
        await _incidentService.HandleAsync(incidentId, request, handledBy);
        return Ok(ApiResponse.Ok("Cập nhật xử lý sự cố thành công."));
    }
}
