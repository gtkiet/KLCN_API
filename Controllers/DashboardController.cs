//using KLCN_API.Filters;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/dashboard")]
//[AuthorizeRoles(RoleEnum.Admin, RoleEnum.Staff)]
//public class DashboardController : ControllerBase
//{
//    private readonly IDashboardService _dashboardService;

//    public DashboardController(IDashboardService dashboardService)
//    {
//        _dashboardService = dashboardService;
//    }

//    /// <summary>Tổng quan Dashboard.</summary>
//    [HttpGet("summary")]
//    public async Task<IActionResult> GetSummary()
//    {
//        var result = await _dashboardService.GetSummaryAsync();
//        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(result));
//    }

//    /// <summary>Doanh thu theo tháng.</summary>
//    [HttpGet("revenue")]
//    public async Task<IActionResult> GetRevenue([FromQuery] int? year)
//    {
//        var result = await _dashboardService.GetRevenueByMonthAsync(year ?? DateTime.Now.Year);
//        return Ok(ApiResponse<List<RevenueByMonthResponse>>.Ok(result));
//    }

//    /// <summary>Tỷ lệ lấp đầy theo sân.</summary>
//    [HttpGet("occupancy")]
//    public async Task<IActionResult> GetOccupancy([FromQuery] int? year, [FromQuery] int? month)
//    {
//        var result = await _dashboardService.GetOccupancyAsync(
//            year ?? DateTime.Now.Year,
//            month ?? DateTime.Now.Month);
//        return Ok(ApiResponse<List<FieldOccupancyResponse>>.Ok(result));
//    }

//    /// <summary>Dịch vụ bán chạy nhất.</summary>
//    [HttpGet("top-services")]
//    public async Task<IActionResult> GetTopServices()
//    {
//        var result = await _dashboardService.GetTopServicesAsync();
//        return Ok(ApiResponse<List<ServiceRevenueResponse>>.Ok(result));
//    }
//}