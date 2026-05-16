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
[Route("api/incidents")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
        => _incidentService = incidentService;

    /// <summary>
    /// Lấy danh sách sự cố — Admin và Staff.
    /// Lọc theo sân, trạng thái (1=Mới, 2=Đang xử lý, 3=Đã giải quyết).
    /// </summary>
    [HttpGet]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<IncidentResponse>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? fieldId,
        [FromQuery] int? statusId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _incidentService.GetIncidentsAsync(
            fieldId, statusId, page, pageSize);
        return Ok(ApiResponse<PagedResponse<IncidentResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết sự cố — Admin và Staff.</summary>
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
    /// Báo cáo sự cố — Staff hoặc Admin.
    /// Gửi multipart/form-data: fieldId, title, description (tuỳ chọn), image (tuỳ chọn).
    /// </summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Create([FromForm] CreateIncidentRequest request)
    {
        var result = await _incidentService.CreateAsync(request, User.GetUserId());
        return StatusCode(201, ApiResponse<IncidentResponse>.Ok(
            result, "Báo cáo sự cố thành công."));
    }

    /// <summary>
    /// Cập nhật trạng thái xử lý sự cố — Staff hoặc Admin.
    /// StatusId: 2=Đang xử lý, 3=Đã giải quyết.
    /// </summary>
    [HttpPatch("{incidentId:int}/handle")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Handle(
        int incidentId, [FromBody] HandleIncidentRequest request)
    {
        await _incidentService.HandleAsync(incidentId, request, User.GetUserId());
        return Ok(ApiResponse.Ok("Cập nhật sự cố thành công."));
    }
}