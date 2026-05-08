//using KLCN_API.Filters;
//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/incidents")]
//[Authorize]
//public class IncidentsController : ControllerBase
//{
//    private readonly IIncidentService _incidentService;

//    public IncidentsController(IIncidentService incidentService)
//    {
//        _incidentService = incidentService;
//    }

//    /// <summary>Lấy danh sách sự cố — Admin và Staff.</summary>
//    [HttpGet]
//    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//    public async Task<IActionResult> GetIncidents(
//        [FromQuery] int? fieldId,
//        [FromQuery] int? statusId,
//        [FromQuery] int page = 1,
//        [FromQuery] int pageSize = 20)
//    {
//        var result = await _incidentService.GetIncidentsAsync(fieldId, statusId, page, pageSize);
//        return Ok(ApiResponse<PagedResponse<IncidentResponse>>.Ok(result));
//    }

//    /// <summary>Lấy chi tiết sự cố.</summary>
//    [HttpGet("{incidentId:int}")]
//    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//    public async Task<IActionResult> GetById(int incidentId)
//    {
//        var result = await _incidentService.GetIncidentByIdAsync(incidentId);
//        return Ok(ApiResponse<IncidentResponse>.Ok(result));
//    }

//    /// <summary>Báo cáo sự cố — tất cả đã đăng nhập.</summary>
//    [HttpPost]
//    public async Task<IActionResult> Create([FromBody] CreateIncidentRequest request)
//    {
//        var userId = User.GetUserId();
//        var result = await _incidentService.CreateIncidentAsync(request, userId);
//        return Ok(ApiResponse<IncidentResponse>.Ok(result, "Báo cáo sự cố thành công."));
//    }

//    /// <summary>Xử lý sự cố — Admin và Staff.</summary>
//    [HttpPut("{incidentId:int}/handle")]
//    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//    public async Task<IActionResult> Handle(int incidentId, [FromBody] HandleIncidentRequest request)
//    {
//        var userId = User.GetUserId();
//        await _incidentService.HandleIncidentAsync(incidentId, request, userId);
//        return Ok(ApiResponse.Ok("Cập nhật sự cố thành công."));
//    }
//}