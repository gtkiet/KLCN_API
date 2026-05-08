//using KLCN_API.Filters;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/fields")]
//public class FieldsController : ControllerBase
//{
//    private readonly IFieldService _fieldService;

//    public FieldsController(IFieldService fieldService)
//    {
//        _fieldService = fieldService;
//    }

//    /// <summary>Lấy danh sách sân — Public.</summary>
//    [HttpGet]
//    [AllowAnonymous]
//    public async Task<IActionResult> GetFields([FromQuery] int? typeId, [FromQuery] int? statusId)
//    {
//        var result = await _fieldService.GetFieldsAsync(typeId, statusId);
//        return Ok(ApiResponse<List<FieldResponse>>.Ok(result));
//    }

//    /// <summary>Lấy chi tiết 1 sân — Public.</summary>
//    [HttpGet("{fieldId:int}")]
//    [AllowAnonymous]
//    public async Task<IActionResult> GetById(int fieldId)
//    {
//        var result = await _fieldService.GetFieldByIdAsync(fieldId);
//        return Ok(ApiResponse<FieldResponse>.Ok(result));
//    }

//    /// <summary>Tạo sân mới — Admin.</summary>
//    [HttpPost]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Create([FromBody] CreateFieldRequest request)
//    {
//        var result = await _fieldService.CreateFieldAsync(request);
//        return Ok(ApiResponse<FieldResponse>.Ok(result, "Tạo sân thành công."));
//    }

//    /// <summary>Cập nhật sân — Admin.</summary>
//    [HttpPut("{fieldId:int}")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Update(int fieldId, [FromBody] UpdateFieldRequest request)
//    {
//        var result = await _fieldService.UpdateFieldAsync(fieldId, request);
//        return Ok(ApiResponse<FieldResponse>.Ok(result, "Cập nhật sân thành công."));
//    }

//    /// <summary>Xóa mềm sân — Admin.</summary>
//    [HttpDelete("{fieldId:int}")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> Delete(int fieldId)
//    {
//        await _fieldService.DeleteFieldAsync(fieldId);
//        return Ok(ApiResponse.Ok("Xóa sân thành công."));
//    }

//    /// <summary>Lấy lịch sân theo ngày — Public.</summary>
//    [HttpGet("schedule")]
//    [AllowAnonymous]
//    public async Task<IActionResult> GetSchedule([FromQuery] GetFieldScheduleRequest request)
//    {
//        var result = await _fieldService.GetScheduleAsync(request);
//        return Ok(ApiResponse<List<FieldScheduleResponse>>.Ok(result));
//    }

//    /// <summary>Sinh slot cho khoảng ngày — Admin.</summary>
//    [HttpPost("generate-slots")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> GenerateSlots([FromBody] GenerateSlotsRequest request)
//    {
//        await _fieldService.GenerateSlotsAsync(request.StartDate, request.EndDate);
//        return Ok(ApiResponse.Ok("Sinh slot thành công."));
//    }

//    /// <summary>Tạo log bảo trì sân — Admin.</summary>
//    [HttpPost("{fieldId:int}/maintenance")]
//    [AuthorizeRoles(RoleEnum.Admin)]
//    public async Task<IActionResult> SetMaintenance(int fieldId, [FromBody] CreateMaintenanceRequest request)
//    {
//        await _fieldService.SetMaintenanceAsync(fieldId, request);
//        return Ok(ApiResponse.Ok("Ghi nhận bảo trì thành công."));
//    }

//    /// <summary>Lấy rating + reviews của sân — Public.</summary>
//    [HttpGet("{fieldId:int}/reviews")]
//    [AllowAnonymous]
//    public async Task<IActionResult> GetReviews(int fieldId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
//    {
//        var result = await _fieldService.GetFieldRatingAsync(fieldId, page, pageSize);
//        return Ok(ApiResponse<FieldRatingResponse>.Ok(result));
//    }
//}