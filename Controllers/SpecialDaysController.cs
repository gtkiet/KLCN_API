//using KLCN_API.Filters;
//using KLCN_API.Helpers;
//using KLCN_API.Models.DTOs.Request;
//using KLCN_API.Models.DTOs.Response;
//using KLCN_API.Models.Enums;
//using KLCN_API.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;

//namespace KLCN_API.Controllers;

//[ApiController]
//[Route("api/special-days")]
//[AuthorizeRoles(RoleEnum.Admin)]
//public class SpecialDaysController : ControllerBase
//{
//    private readonly ISpecialDayService _specialDayService;

//    public SpecialDaysController(ISpecialDayService specialDayService)
//    {
//        _specialDayService = specialDayService;
//    }

//    /// <summary>Lấy danh sách ngày đặc biệt.</summary>
//    [HttpGet]
//    public async Task<IActionResult> GetSpecialDays()
//    {
//        var result = await _specialDayService.GetSpecialDaysAsync();
//        return Ok(ApiResponse<List<SpecialDayResponse>>.Ok(result));
//    }

//    /// <summary>Tạo ngày đặc biệt.</summary>
//    [HttpPost]
//    public async Task<IActionResult> Create([FromBody] CreateSpecialDayRequest request)
//    {
//        var userId = User.GetUserId();
//        var result = await _specialDayService.CreateSpecialDayAsync(request, userId);
//        return Ok(ApiResponse<SpecialDayResponse>.Ok(result, "Tạo ngày đặc biệt thành công."));
//    }

//    /// <summary>Xóa ngày đặc biệt.</summary>
//    [HttpDelete("{specialDayId:int}")]
//    public async Task<IActionResult> Delete(int specialDayId)
//    {
//        await _specialDayService.DeleteSpecialDayAsync(specialDayId);
//        return Ok(ApiResponse.Ok("Xóa ngày đặc biệt thành công."));
//    }
//}