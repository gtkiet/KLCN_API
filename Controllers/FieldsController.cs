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
[Route("api/fields")]
public class FieldsController : ControllerBase
{
    private readonly IFieldService _fieldService;

    public FieldsController(IFieldService fieldService)
        => _fieldService = fieldService;

    // ── CRUD ─────────────────────────────────────────────────────

    /// <summary>Lấy danh sách sân có filter + phân trang — Public.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FieldResponse>>), 200)]
    public async Task<IActionResult> GetFields([FromQuery] GetFieldsRequest request)
    {
        var result = await _fieldService.GetFieldsAsync(request);
        return Ok(ApiResponse<PagedResponse<FieldResponse>>.Ok(result));
    }

    /// <summary>Lấy chi tiết 1 sân — Public.</summary>
    [HttpGet("{fieldId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FieldResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetById(int fieldId)
    {
        var result = await _fieldService.GetByIdAsync(fieldId);
        return Ok(ApiResponse<FieldResponse>.Ok(result));
    }

    /// <summary>Tạo sân mới — Admin.</summary>
    [HttpPost]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<FieldResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Create([FromBody] CreateFieldRequest request)
    {
        var adminId = User.GetUserId();
        var result = await _fieldService.CreateAsync(adminId, request);
        return Ok(ApiResponse<FieldResponse>.Ok(result, "Tạo sân thành công."));
    }

    /// <summary>Cập nhật thông tin sân — Admin.</summary>
    [HttpPut("{fieldId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse<FieldResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Update(int fieldId, [FromBody] UpdateFieldRequest request)
    {
        var adminId = User.GetUserId();
        var result = await _fieldService.UpdateAsync(fieldId, adminId, request);
        return Ok(ApiResponse<FieldResponse>.Ok(result, "Cập nhật sân thành công."));
    }

    /// <summary>Xóa mềm sân — Admin.</summary>
    [HttpDelete("{fieldId:int}")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> Delete(int fieldId)
    {
        await _fieldService.DeleteAsync(fieldId);
        return Ok(ApiResponse.Ok("Xóa sân thành công."));
    }

    // ── Schedule & slots ─────────────────────────────────────────

    /// <summary>Lấy lịch sân theo ngày — Public.</summary>
    [HttpGet("schedule")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<FieldScheduleResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> GetSchedule([FromQuery] GetFieldScheduleRequest request)
    {
        var result = await _fieldService.GetScheduleAsync(request);
        return Ok(ApiResponse<List<FieldScheduleResponse>>.Ok(result));
    }

    /// <summary>Sinh slot cho khoảng ngày — Admin.</summary>
    [HttpPost("generate-slots")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> GenerateSlots([FromBody] GenerateSlotsRequest request)
    {
        await _fieldService.GenerateSlotsAsync(request);
        return Ok(ApiResponse.Ok("Sinh slot thành công."));
    }

    // ── Price history ─────────────────────────────────────────────

    /// <summary>Lấy lịch sử thay đổi giá của sân — Admin và Staff.</summary>
    [HttpGet("{fieldId:int}/price-history")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<List<FieldPriceHistoryResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetPriceHistory(int fieldId)
    {
        var result = await _fieldService.GetPriceHistoryAsync(fieldId);
        return Ok(ApiResponse<List<FieldPriceHistoryResponse>>.Ok(result));
    }

    // ── Maintenance ───────────────────────────────────────────────

    /// <summary>Lấy lịch sử bảo trì của sân — Admin và Staff.</summary>
    [HttpGet("{fieldId:int}/maintenance")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<List<FieldMaintenanceLogResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetMaintenanceLogs(int fieldId)
    {
        var result = await _fieldService.GetMaintenanceLogsAsync(fieldId);
        return Ok(ApiResponse<List<FieldMaintenanceLogResponse>>.Ok(result));
    }

    /// <summary>
    /// Tạo log bảo trì sân — Admin.
    /// Nếu StartDate là hôm nay, sân sẽ tự động chuyển sang trạng thái Bảo trì.
    /// </summary>
    [HttpPost("{fieldId:int}/maintenance")]
    [AuthorizeRoles(RoleEnum.Admin)]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> AddMaintenance(
        int fieldId, [FromBody] CreateMaintenanceRequest request)
    {
        var createdBy = User.GetUserId();
        await _fieldService.AddMaintenanceLogAsync(fieldId, createdBy, request);
        return Ok(ApiResponse.Ok("Ghi nhận bảo trì thành công."));
    }
}