using KLCN_API.Filters;
using KLCN_API.Models.DTOs.Response;
using KLCN_API.Models.Enums;
using KLCN_API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KLCN_API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashService;

    public DashboardController(IDashboardService dashService)
        => _dashService = dashService;

    /// <summary>
    /// Tổng quan dashboard: số booking chờ xử lý, doanh thu hôm nay,
    /// sân đang hoạt động / bảo trì, sự cố mới, v.v.
    /// </summary>
    [HttpGet("summary")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryResponse>), 200)]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _dashService.GetSummaryAsync();
        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(result));
    }

    /// <summary>
    /// Doanh thu theo từng tháng trong năm chỉ định.
    /// Mặc định năm hiện tại nếu không truyền.
    /// </summary>
    [HttpGet("revenue-by-month")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<List<RevenueByMonthResponse>>), 200)]
    public async Task<IActionResult> GetRevenueByMonth([FromQuery] int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var result = await _dashService.GetRevenueByMonthAsync(targetYear);
        return Ok(ApiResponse<List<RevenueByMonthResponse>>.Ok(result));
    }

    /// <summary>
    /// Tỷ lệ lấp đầy slot của từng sân theo tháng.
    /// Có thể lọc theo năm và/hoặc tháng.
    /// </summary>
    [HttpGet("field-occupancy")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<List<FieldOccupancyResponse>>), 200)]
    public async Task<IActionResult> GetFieldOccupancy(
        [FromQuery] int? year, [FromQuery] int? month)
    {
        var result = await _dashService.GetOccupancyAsync(year, month);
        return Ok(ApiResponse<List<FieldOccupancyResponse>>.Ok(result));
    }

    /// <summary>Doanh thu đóng góp theo từng dịch vụ đi kèm.</summary>
    [HttpGet("revenue-by-service")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<List<RevenueByServiceResponse>>), 200)]
    public async Task<IActionResult> GetRevenueByService()
    {
        var result = await _dashService.GetRevenueByServiceAsync();
        return Ok(ApiResponse<List<RevenueByServiceResponse>>.Ok(result));
    }

    [HttpGet("monthly-report")]
    [AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
    [ProducesResponseType(typeof(ApiResponse<MonthlyReportResponse>), 200)]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int? year, [FromQuery] int? month)
    {
        var now = DateTime.UtcNow;
        var targetYear = year ?? now.Year;
        var targetMonth = month ?? now.Month;

        var result = await _dashService.GetMonthlyReportAsync(targetYear, targetMonth);
        return Ok(ApiResponse<MonthlyReportResponse>.Ok(result));
    }
}
